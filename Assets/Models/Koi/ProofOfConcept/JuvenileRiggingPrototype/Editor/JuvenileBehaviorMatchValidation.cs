using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class JuvenileBehaviorMatchValidation
{
    public const string ReportFolder = JuvenileRiggingPrototypeBuilder.Folder + "/Reports/BehaviorMatch";
    [Serializable] public class Metrics
    {
        public string target, clip;
        public int frames, unresolvedBindings, nonfinite, outsideBounds;
        public float restError, maxDisplacement, normalizedDisplacement, maxEdgeRatio, mouthPeak, loopSeam;
        public float[] regionalMaxEdgeRatio = new float[5];
    }
    [Serializable] public class Audit
    {
        public bool geometryPreserved, uvsPreserved, weightsValid, seamsConsistent, mouthSeamsConsistent;
        public float maxWeightError, sizeRatio, maxLipDelta;
        public int mouthVertices, protectedEyeMouthVertices;
        public Metrics[] metrics;
    }
    public static JuvenilePrototypePlayback Target()
    {
        return EditorSceneManager.GetActiveScene().GetRootGameObjects()
            .Select(g => g.GetComponent<JuvenilePrototypePlayback>()).Single(c => c != null);
    }
    public static Audit Validate()
    {
        var playback = Target(); playback.Initialize(); playback.RestoreRest();
        var mesh = playback.Skin.sharedMesh;
        var source = AssetDatabase.LoadAllAssetsAtPath(JuvenileRiggingPrototypeBuilder.SourcePath).OfType<UnityEngine.Mesh>().First();
        var a = new Audit { geometryPreserved = mesh.vertices.SequenceEqual(source.vertices) && mesh.triangles.SequenceEqual(source.triangles)
            && mesh.normals.SequenceEqual(source.normals) && mesh.tangents.SequenceEqual(source.tangents) && mesh.subMeshCount == source.subMeshCount,
            uvsPreserved = true, weightsValid = true, seamsConsistent = true, mouthSeamsConsistent = true };
        for (int channel = 0; channel < 8; channel++)
        {
            var x = new List<Vector4>(); var y = new List<Vector4>(); mesh.GetUVs(channel, x); source.GetUVs(channel, y); a.uvsPreserved &= x.SequenceEqual(y);
        }
        var verts = mesh.vertices; var weights = mesh.boneWeights;
        var delta = new Vector3[verts.Length]; mesh.GetBlendShapeFrameVertices(mesh.GetBlendShapeIndex("MouthOpen"), 0, delta, null, null);
        var map = AssetDatabase.LoadAssetAtPath<GameObject>(JuvenileRiggingPrototypeBuilder.SourcePath).transform.localToWorldMatrix;
        var seams = new Dictionary<Vector3, int>();
        for (int i = 0; i < verts.Length; i++)
        {
            var w = weights[i]; float sum = w.weight0 + w.weight1 + w.weight2 + w.weight3;
            a.maxWeightError = Mathf.Max(a.maxWeightError, Mathf.Abs(1 - sum));
            if (float.IsNaN(sum) || float.IsInfinity(sum) || Mathf.Abs(1 - sum) > 1e-5f || w.weight0 < 0 || w.weight1 < 0 || w.weight2 < 0 || w.weight3 < 0
                || w.boneIndex0 < 0 || w.boneIndex0 >= 10 || w.boneIndex1 < 0 || w.boneIndex1 >= 10 || w.boneIndex2 < 0 || w.boneIndex2 >= 10 || w.boneIndex3 < 0 || w.boneIndex3 >= 10) a.weightsValid = false;
            if (seams.TryGetValue(verts[i], out int previous))
            {
                for (int b = 0; b < 10; b++) if (Mathf.Abs(JuvenileRiggingPrototypeBuilder.Influence(w, b) - JuvenileRiggingPrototypeBuilder.Influence(weights[previous], b)) > 1e-5f) a.seamsConsistent = false;
                if (delta[i] != delta[previous]) a.mouthSeamsConsistent = false;
            }
            else seams[verts[i]] = i;
            var p = map.MultiplyPoint3x4(verts[i]);
            if (delta[i].sqrMagnitude > 1e-16f) { a.mouthVertices++; a.maxLipDelta = Mathf.Max(a.maxLipDelta, playback.Skin.transform.TransformVector(delta[i]).magnitude); }
            if (p.z < .8f && (delta[i].sqrMagnitude > 1e-16f || JuvenileRiggingPrototypeBuilder.Influence(w, 3) > 1e-5f)) a.protectedEyeMouthVertices++;
        }
        float jl = Length(playback.Skin), al = Length(playback.AdultSkin); a.sizeRatio = jl / al;
        var list = new List<Metrics>();
        var preview = EditorSceneManager.NewPreviewScene();
        var baselineRoot = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ReportFolder + "/Baseline/Juvenile_Baseline.prefab"));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(baselineRoot, preview);
        var baselineSkin = baselineRoot.GetComponentInChildren<SkinnedMeshRenderer>();
        baselineSkin.sharedMesh = AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(JuvenileRiggingPrototypeRefinement.BaselinePath);
        baselineRoot.transform.localScale = playback.transform.localScale;
        bool originalAdaptation = playback.AdaptJuvenile;
        try
        {
            foreach (string clipName in new[] { "Swim", "Idle", "Eat" })
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Models/Koi/Animations/Koi_" + clipName + ".anim");
                list.Add(Measure(baselineRoot, baselineSkin, clip, "Juvenile baseline", null, false));
                list.Add(Measure(playback.transform.gameObject, playback.Skin, clip, "Juvenile exact source", playback, false));
                list.Add(Measure(playback.transform.gameObject, playback.Skin, clip, "Juvenile adapted", playback, true));
                list.Add(Measure(playback.AdultSkin.transform.parent.gameObject, playback.AdultSkin, clip, "Adult exact source", null, false));
            }
        }
        finally { playback.AdaptJuvenile = originalAdaptation; playback.RestoreRest(); EditorSceneManager.ClosePreviewScene(preview); }
        a.metrics = list.ToArray(); File.WriteAllText(ReportFolder + "/StructuralAndSampledResults.json", JsonUtility.ToJson(a, true)); return a;
    }
    private static float Length(SkinnedMeshRenderer skin)
    {
        var v = skin.sharedMesh.vertices.Select(p => skin.transform.TransformPoint(p)).ToArray(); return v.Max(p => p.z) - v.Min(p => p.z);
    }
    private static Metrics Measure(GameObject root, SkinnedMeshRenderer skin, AnimationClip clip, string label, JuvenilePrototypePlayback playback, bool adapted)
    {
        var m = new Metrics { target = label, clip = clip.name };
        var bones = skin.bones; var pos = bones.Select(b => b.localPosition).ToArray(); var rot = bones.Select(b => b.localRotation).ToArray(); var scales = bones.Select(b => b.localScale).ToArray();
        int mouth = skin.sharedMesh.GetBlendShapeIndex("MouthOpen"); var baked = new UnityEngine.Mesh();
        Action restore = () => { if (playback != null) playback.RestoreRest(); else { for (int b = 0; b < bones.Length; b++) { bones[b].localPosition = pos[b]; bones[b].localRotation = rot[b]; bones[b].localScale = scales[b]; } skin.SetBlendShapeWeight(mouth, 0); } };
        restore(); skin.BakeMesh(baked, true); var rest = baked.vertices; var original = skin.sharedMesh.vertices;
        for (int i = 0; i < rest.Length; i++) m.restError = Mathf.Max(m.restError, skin.transform.TransformVector(rest[i] - original[i]).magnitude);
        float length = Length(skin); var tris = skin.sharedMesh.triangles; var bw = skin.sharedMesh.boneWeights;
        foreach (var binding in AnimationUtility.GetCurveBindings(clip))
        {
            var t = root.transform.Find(binding.path);
            if (t == null || (binding.type == typeof(SkinnedMeshRenderer) && t.GetComponent<SkinnedMeshRenderer>() == null)) m.unresolvedBindings++;
        }
        var mouthBinding = AnimationUtility.GetCurveBindings(clip).First(b => b.propertyName == "blendShape.MouthOpen");
        float peak = AnimationUtility.GetEditorCurve(clip, mouthBinding).keys.OrderByDescending(k => k.value).First().time;
        Vector3[] first = null, last = null;
        try
        {
            for (int frame = 0; frame < 34; frame++)
            {
                float time = frame == 33 ? peak : clip.length * frame / 32;
                restore(); clip.SampleAnimation(root, time);
                if (playback != null) { playback.AdaptJuvenile = adapted; playback.ApplyPoseAdaptation(clip.name.Contains("Swim") ? 0 : clip.name.Contains("Idle") ? 1 : 2, time); }
                skin.BakeMesh(baked, true); var v = baked.vertices; m.frames++;
                if (frame == 0) first = v; if (frame == 32) last = v;
                m.mouthPeak = Mathf.Max(m.mouthPeak, skin.GetBlendShapeWeight(mouth));
                for (int i = 0; i < v.Length; i++)
                {
                    if (float.IsNaN(v[i].x + v[i].y + v[i].z) || float.IsInfinity(v[i].x + v[i].y + v[i].z)) m.nonfinite++;
                    if (!skin.localBounds.Contains(v[i])) m.outsideBounds++;
                    m.maxDisplacement = Mathf.Max(m.maxDisplacement, skin.transform.TransformVector(v[i] - rest[i]).magnitude);
                }
                for (int t = 0; t < tris.Length; t += 3) for (int c = 0; c < 3; c++)
                {
                    int x = tris[t + c], y = tris[t + (c + 1) % 3];
                    float before = skin.transform.TransformVector(rest[x] - rest[y]).magnitude;
                    if (before < length * .0001f) continue;
                    float ratio = skin.transform.TransformVector(v[x] - v[y]).magnitude / before;
                    m.maxEdgeRatio = Mathf.Max(m.maxEdgeRatio, ratio);
                    int region = bw[x].boneIndex0 == 4 ? 1 : bw[x].boneIndex0 == 5 ? 2 : bw[x].boneIndex0 >= 6 ? 3 : bw[x].boneIndex0 == 3 ? 4 : 0;
                    m.regionalMaxEdgeRatio[region] = Mathf.Max(m.regionalMaxEdgeRatio[region], ratio);
                }
            }
            m.normalizedDisplacement = m.maxDisplacement / length;
            if (clip.isLooping) for (int i = 0; i < first.Length; i++) m.loopSeam = Mathf.Max(m.loopSeam, skin.transform.TransformVector(first[i] - last[i]).magnitude);
            return m;
        }
        finally { restore(); UnityEngine.Object.DestroyImmediate(baked); }
    }
    public static void CapturePairs()
    {
        var p = Target(); p.Initialize(); p.RestoreRest();
        var skins = new[] { p.AdultSkin, p.Skin };
        var roots = new[] { p.AdultSkin.transform.parent.gameObject, p.gameObject };
        var camera = EditorSceneManager.GetActiveScene().GetRootGameObjects().Select(g => g.GetComponent<Camera>()).First(c => c != null);
        var cameraPos = camera.transform.position; var cameraRot = camera.transform.rotation; float ortho = camera.orthographicSize;
        var previousTarget = camera.targetTexture; var previousActive = RenderTexture.active;
        var hidden = EditorSceneManager.GetActiveScene().GetRootGameObjects().Where(g => g != roots[0] && g != roots[1] && g.GetComponentInChildren<Renderer>() != null).ToArray();
        var active = hidden.Select(g => g.activeSelf).ToArray(); foreach (var g in hidden) g.SetActive(false);
        var snapshots = new GameObject[2]; var meshes = new UnityEngine.Mesh[2]; var enabled = skins.Select(s => s.enabled).ToArray();
        var rt = new RenderTexture(512, 512, 24); rt.Create(); var tile = new Texture2D(512, 512, TextureFormat.RGB24, false);
        bool oldAdapted = p.AdaptJuvenile;
        var directions = new[] { Vector3.right, Vector3.up, Vector3.forward, new Vector3(1, .6f, 1).normalized };
        try
        {
            for (int target = 0; target < 2; target++)
            {
                snapshots[target] = new GameObject("Temporary paired CPU pose"); snapshots[target].transform.SetPositionAndRotation(skins[target].transform.position, skins[target].transform.rotation);
                snapshots[target].transform.localScale = skins[target].transform.lossyScale;
                meshes[target] = new UnityEngine.Mesh(); snapshots[target].AddComponent<MeshFilter>().sharedMesh = meshes[target]; snapshots[target].AddComponent<MeshRenderer>().sharedMaterials = skins[target].sharedMaterials;
                skins[target].enabled = false;
            }
            foreach (bool adapted in new[] { false, true }) foreach (string name in new[] { "Swim", "Idle", "Eat" })
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Models/Koi/Animations/Koi_" + name + ".anim");
                var sheet = new Texture2D(4096, 2048, TextureFormat.RGB24, false);
                for (int row = 0; row < 4; row++)
                {
                    p.RestoreRest(); float time = clip.length * row / 4;
                    foreach (var root in roots) clip.SampleAnimation(root, time);
                    p.AdaptJuvenile = adapted; p.ApplyPoseAdaptation(name == "Swim" ? 0 : name == "Idle" ? 1 : 2, time);
                    for (int target = 0; target < 2; target++) skins[target].BakeMesh(meshes[target], true);
                    for (int view = 0; view < 4; view++) for (int target = 0; target < 2; target++)
                    {
                        snapshots[1 - target].SetActive(false); snapshots[target].SetActive(true);
                        var center = roots[target].transform.position;
                        camera.transform.position = center + directions[view] * 2;
                        camera.transform.rotation = Quaternion.LookRotation(-directions[view], view == 1 ? Vector3.forward : Vector3.up);
                        camera.orthographicSize = .37f; camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
                        tile.ReadPixels(new Rect(0, 0, 512, 512), 0, 0); tile.Apply();
                        sheet.SetPixels(view * 1024 + target * 512, (3 - row) * 512, 512, 512, tile.GetPixels());
                    }
                }
                sheet.Apply(); File.WriteAllBytes(ReportFolder + "/" + name + "_" + (adapted ? "Adapted" : "Source") + "_AdultLeft_JuvenileRight_SideTopFrontIso.png", sheet.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(sheet);
            }
        }
        finally
        {
            p.AdaptJuvenile = oldAdapted; p.RestoreRest();
            for (int target = 0; target < 2; target++) { skins[target].enabled = enabled[target]; UnityEngine.Object.DestroyImmediate(snapshots[target]); UnityEngine.Object.DestroyImmediate(meshes[target]); }
            for (int i = 0; i < hidden.Length; i++) hidden[i].SetActive(active[i]);
            camera.transform.SetPositionAndRotation(cameraPos, cameraRot); camera.orthographicSize = ortho; camera.targetTexture = previousTarget; RenderTexture.active = previousActive;
            rt.Release(); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(tile);
        }
        File.WriteAllText(ReportFolder + "/CaptureLabels.txt", "Paired 4096x2048 contact sheets: each 1024px group is adult LEFT, juvenile RIGHT; equal orthographic camera scale .37. Groups side/top/front/isometric; rows 0/25/50/75% clip. Juvenile world body length 60% of adult. Source = exact adult clip; Adapted = juvenile angular attenuation body .70, tail .85, fins .70, Eat .85; shape timing unchanged. CPU-baked EditMode snapshots, not continuous runtime evidence.\n");
        AssetDatabase.Refresh();
    }
}
