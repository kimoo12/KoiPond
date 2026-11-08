using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class JuvenileRiggingPrototypeRefinement
{
    public const string BaselinePath = JuvenileRiggingPrototypeBuilder.Folder + "/Reports/BehaviorMatch/Baseline/Juvenile_BaselineMesh.asset";

    private static float Smooth(float a, float b, float value) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(a, b, value));

    public static float LipMask(Vector3 p)
    {
        return Smooth(.85f, .92f, p.z) * (1 - Smooth(.09f, .17f, Mathf.Abs(p.x)))
            * (1 - Smooth(.07f, .12f, Mathf.Abs(p.y + .14f)));
    }

    public static void RefineMouth()
    {
        var mesh = AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(JuvenileRiggingPrototypeBuilder.MeshPath);
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(JuvenileRiggingPrototypeBuilder.SourcePath);
        var full = source.transform.localToWorldMatrix;
        var v = mesh.vertices;
        var delta = new Vector3[v.Length];
        for (int i = 0; i < v.Length; i++)
        {
            var p = full.MultiplyPoint3x4(v[i]);
            float mask = LipMask(p);
            float lower = 1 - Smooth(-.16f, -.12f, p.y);
            // Lip separation, not lateral muzzle inflation. No interior or new topology supplied.
            var opening = new Vector3(0, Mathf.Lerp(.012f, -.030f, lower), .004f * lower) * mask;
            delta[i] = full.inverse.MultiplyVector(opening);
        }
        Undo.RecordObject(mesh, "Localize juvenile MouthOpen");
        mesh.ClearBlendShapes();
        mesh.AddBlendShapeFrame("MouthOpen", 100, delta, new Vector3[v.Length], new Vector3[v.Length]);
        EditorUtility.SetDirty(mesh); AssetDatabase.SaveAssets();
        File.WriteAllText(JuvenileRiggingPrototypeBuilder.Folder + "/Reports/BehaviorMatch/MouthRefinement.txt",
            "Localized z>.85, |x|<.17, |y+.14|<.12 lip mask, shape split across y=-.16..-.12 around measured crease. Lower lip down <=.030 full imported units; upper lip up <=.012; forward lower lip <=.004. No lateral inflation; identical positions receive identical deltas. Coordinates converted through original import frame. Original adult shape timing/ranges retained. Mouth-bone influence <=.30 at lower lip. No geometry/cavity added: convincing dark interior remains limited by original mesh and appearance pairing.\n");
    }

    public static void Refine()
    {
        var mesh = AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(JuvenileRiggingPrototypeBuilder.MeshPath);
        var baseline = AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(BaselinePath);
        if (baseline == null || !mesh.vertices.SequenceEqual(baseline.vertices))
            throw new InvalidOperationException("Verified immutable transferred baseline required.");
        var v = mesh.vertices;
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(JuvenileRiggingPrototypeBuilder.SourcePath);
        var full = source.transform.localToWorldMatrix;
        var original = baseline.boneWeights;
        var keys = new Dictionary<Vector3, int>();
        var reps = new int[v.Length];
        var positions = new List<Vector3>();
        for (int i = 0; i < v.Length; i++)
        {
            if (!keys.TryGetValue(v[i], out int id)) { id = keys.Count; keys.Add(v[i], id); positions.Add(full.MultiplyPoint3x4(v[i])); }
            reps[i] = id;
        }
        var weights = new float[keys.Count, 10];
        for (int i = 0; i < v.Length; i++)
        {
            var p = positions[reps[i]];
            int id = reps[i];
            for (int b = 0; b < 10; b++) weights[id, b] = JuvenileRiggingPrototypeBuilder.Influence(original[i], b);
            // Remove diffused jaw influence from cheeks and eyes; retain only localized lower lip.
            float jaw = LipMask(p) * (1 - Smooth(-.15f, -.13f, p.y)) * .30f;
            weights[id, 2] += weights[id, 3];
            weights[id, 3] = 0;
            float finMask = Smooth(.15f, .30f, Mathf.Abs(p.x)) * Smooth(-.12f, -.31f, p.y)
                * Smooth(.06f, .16f, p.z) * (1 - Smooth(.58f, .68f, p.z));
            for (int b = 4; b <= 5; b++)
            {
                float keep = ((b == 4 && p.x < 0) || (b == 5 && p.x > 0)) ? finMask : 0;
                float removed = weights[id, b] * (1 - keep);
                weights[id, b] *= keep;
                weights[id, 1] += removed;
            }
            // Stable juvenile head, with smooth neck transition; transferred body and tail remain intact.
            float head = Smooth(.49f, .73f, p.z) * .65f;
            for (int b = 0; b < 10; b++) weights[id, b] *= 1 - head;
            weights[id, 2] += head;
            for (int b = 0; b < 10; b++) weights[id, b] *= 1 - jaw;
            weights[id, 3] += jaw;
        }
        var neighbors = Enumerable.Range(0, keys.Count).Select(_ => new HashSet<int>()).ToArray();
        var tris = mesh.triangles;
        for (int t = 0; t < tris.Length; t += 3)
            for (int c = 0; c < 3; c++)
            {
                int a = reps[tris[t + c]], b = reps[tris[t + (c + 1) % 3]];
                if (a != b) { neighbors[a].Add(b); neighbors[b].Add(a); }
            }
        // Bounded correction only at pectoral attachments, never global diffusion into mouth/eyes/tail.
        for (int pass = 0; pass < 12; pass++)
        {
            var next = (float[,])weights.Clone();
            for (int i = 0; i < keys.Count; i++)
            {
                var p = positions[i];
                float mask = Smooth(.13f, .22f, Mathf.Abs(p.x)) * (1 - Smooth(.30f, .36f, Mathf.Abs(p.x)))
                    * Smooth(-.13f, -.27f, p.y) * Smooth(.08f, .18f, p.z) * (1 - Smooth(.54f, .64f, p.z));
                if (mask <= 0 || neighbors[i].Count == 0) continue;
                for (int b = 0; b < 10; b++)
                {
                    float sum = 0, total = 0;
                    foreach (int j in neighbors[i])
                    {
                        if (Mathf.Sign(positions[j].x) != Mathf.Sign(p.x)) continue;
                        float factor = 1 / Mathf.Max(Vector3.Distance(p, positions[j]), .0003f);
                        sum += weights[j, b] * factor; total += factor;
                    }
                    if (total > 0) next[i, b] = Mathf.Lerp(weights[i, b], sum / total, mask * .35f);
                }
            }
            weights = next;
        }
        var output = new BoneWeight[v.Length];
        for (int i = 0; i < v.Length; i++)
        {
            int id = reps[i];
            var order = Enumerable.Range(0, 10).OrderByDescending(b => weights[id, b]).Take(4).ToArray();
            float sum = order.Sum(b => weights[id, b]);
            if (sum < 1e-8f) throw new InvalidOperationException("Empty influences after correction.");
            output[i] = new BoneWeight { boneIndex0 = order[0], boneIndex1 = order[1], boneIndex2 = order[2], boneIndex3 = order[3],
                weight0 = weights[id, order[0]] / sum, weight1 = weights[id, order[1]] / sum,
                weight2 = weights[id, order[2]] / sum, weight3 = weights[id, order[3]] / sum };
        }
        Undo.RecordObject(mesh, "Refine juvenile regional weights");
        mesh.boneWeights = output;
        var root = PrefabUtility.LoadPrefabContents(JuvenileRiggingPrototypeBuilder.PrefabPath);
        try
        {
            var skin = root.GetComponentInChildren<SkinnedMeshRenderer>();
            var bones = skin.bones;
            // Update in hierarchy order; Root retains its zero-based keyed position and armature offset.
            for (int b = 1; b < bones.Length; b++)
                bones[b].position = root.transform.TransformPoint(JuvenileRiggingPrototypeBuilder.JointPositions[b] * JuvenileRiggingPrototypeBuilder.DisplayScale);
            mesh.bindposes = bones.Select(b => b.worldToLocalMatrix * skin.transform.localToWorldMatrix).ToArray();
            skin.localBounds = mesh.bounds;
            PrefabUtility.SaveAsPrefabAsset(root, JuvenileRiggingPrototypeBuilder.PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        EditorUtility.SetDirty(mesh); AssetDatabase.SaveAssets();
        File.WriteAllText(JuvenileRiggingPrototypeBuilder.Folder + "/Reports/BehaviorMatch/RegionalRefinement.txt",
            "Deterministic current-transferred baseline; localized lower lip jaw <=0.30; removed jaw leakage from eyes/cheeks; stable head blend <=0.65; same-side fin restriction and 12 masked attachment smoothing passes at 0.35; unchanged transferred body/tail foundation. Recomputed bindposes after head/mouth/fin landmark correction. No rest geometry/UV edits.\n");
    }
}
