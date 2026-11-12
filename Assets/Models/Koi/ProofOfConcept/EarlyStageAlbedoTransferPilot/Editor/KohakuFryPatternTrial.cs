using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using KoiPond.EarlyStageTransferPilot;

public static class KohakuFryPatternTrial
{
    const string Folder = EarlyStageAlbedoTransferBuilder.Folder;
    const int Rings = 192, Angles = 128, Size = 2048;
    sealed class Surface
    {
        public Vector3[] V;
        public Vector2[] UV;
        public int[] T, Regions;
        public float Nose, Shoulder, Peduncle;
        public Surface(string path)
        {
            var type = typeof(EarlyStageAlbedoTransferBuilder).GetNestedType("Surface", BindingFlags.NonPublic);
            var data = Activator.CreateInstance(type, new object[] { path });
            V = (Vector3[])type.GetField("V").GetValue(data);
            UV = (Vector2[])type.GetField("UV").GetValue(data);
            T = (int[])type.GetField("T").GetValue(data);
            Regions = (int[])type.GetField("Regions").GetValue(data);
            var body = new bool[V.Length];
            for (int f = 0; f < Regions.Length; f++) if (Regions[f] == 0) for (int j = 0; j < 3; j++) body[T[f * 3 + j]] = true;
            Nose = V.Where((p, i) => body[i]).Max(p => p.z);
            Peduncle = V.Where((p, i) => body[i]).Min(p => p.z);
            // Shoulder is the measured widest lateral body section, not a skeleton joint.
            float[] widths = new float[96];
            for (int i = 0; i < V.Length; i++) if (body[i] && Mathf.Abs(V[i].y) < .045f)
            {
                int z = Mathf.Clamp(Mathf.RoundToInt(Mathf.InverseLerp(Peduncle, Nose, V[i].z) * 95), 0, 95);
                widths[z] = Mathf.Max(widths[z], Mathf.Abs(V[i].x));
            }
            float best = -1; int bestRing = 0;
            for (int i = 3; i < 93; i++)
            {
                float value = 0; for (int j = -3; j <= 3; j++) value += widths[i + j];
                if (value > best) { best = value; bestRing = i; }
            }
            Shoulder = Mathf.Lerp(Peduncle, Nose, bestRing / 95f);
            if (!(Peduncle < Shoulder && Shoulder < Nose)) throw new InvalidOperationException("Non-monotonic surface landmarks");
        }
        public float Coordinate(float z)
        {
            return z <= Shoulder ? .62f * Mathf.InverseLerp(Peduncle, Shoulder, z) : .62f + .38f * Mathf.InverseLerp(Shoulder, Nose, z);
        }
    }
    static Texture2D Decode(string path)
    {
        var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!t.LoadImage(File.ReadAllBytes(path))) throw new InvalidOperationException(path);
        return t;
    }
    static float Angle(Vector3 p) => Mathf.Repeat(Mathf.Atan2(p.y, p.x) / (2 * Mathf.PI) + .5f, 1);
    static float Red(Color c) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.12f, .45f, c.r - Mathf.Max(c.g, c.b)));
    static float Sample(float[,] chart, float z, float angle)
    {
        float zz = Mathf.Clamp01(z) * (Rings - 1), aa = Mathf.Repeat(angle, 1) * Angles;
        int z0 = Mathf.FloorToInt(zz), a0 = Mathf.FloorToInt(aa) % Angles;
        return Mathf.Lerp(Mathf.Lerp(chart[z0, a0], chart[z0, (a0 + 1) % Angles], aa - Mathf.Floor(aa)), Mathf.Lerp(chart[Mathf.Min(z0 + 1, Rings - 1), a0], chart[Mathf.Min(z0 + 1, Rings - 1), (a0 + 1) % Angles], aa - Mathf.Floor(aa)), zz - z0);
    }
    public static string Bake()
    {
        string output = Folder + "/Textures/Kohaku_Fry_RedPattern_Candidate_v02.png";
        if (File.Exists(output)) throw new InvalidOperationException("Candidate already exists; no overwrite permitted");
        var adult = new Surface(EarlyStageAlbedoTransferBuilder.Adult);
        var fry = new Surface(EarlyStageAlbedoTransferBuilder.Fry);
        var skin = AssetDatabase.LoadAssetAtPath<GameObject>(EarlyStageAlbedoTransferBuilder.Fry).GetComponentInChildren<SkinnedMeshRenderer>();
        var original = Decode(AssetDatabase.GetAssetPath(skin.sharedMaterial.GetTexture("_BaseMap")));
        var source = Decode("Assets/Models/Koi/ProofOfConcept/KohakuTopologyTransfer/Kohaku_OriginalMeshy_ToRigged_BaseColor.png");
        try
        {
            var sum = new float[Rings, Angles]; var weights = new float[Rings, Angles];
            var used = new bool[adult.V.Length];
            for (int f = 0; f < adult.Regions.Length; f++) if (adult.Regions[f] == 0) for (int j = 0; j < 3; j++) used[adult.T[f * 3 + j]] = true;
            for (int i = 0; i < used.Length; i++) if (used[i])
            {
                var p = adult.V[i];
                if (Mathf.Abs(p.x) > .04f && p.z > .183f && p.z < .281f && p.y > -.049f && p.y < .048f) continue;
                float z = adult.Coordinate(p.z) * (Rings - 1), angle = Angle(p) * Angles;
                int z0 = Mathf.FloorToInt(z), a0 = Mathf.FloorToInt(angle) % Angles;
                float value = Red(source.GetPixelBilinear(adult.UV[i].x, adult.UV[i].y));
                for (int dz = 0; dz < 2; dz++) for (int da = 0; da < 2; da++)
                {
                    int zz = Mathf.Min(z0 + dz, Rings - 1), aa = (a0 + da) % Angles;
                    float w = (dz == 0 ? 1 - z + z0 : z - z0) * (da == 0 ? 1 - angle + Mathf.Floor(angle) : angle - Mathf.Floor(angle));
                    sum[zz, aa] += value * w; weights[zz, aa] += w;
                }
            }
            var chart = new float[Rings, Angles]; var known = new bool[Rings, Angles]; int measured = 0;
            for (int z = 0; z < Rings; z++) for (int a = 0; a < Angles; a++) if (weights[z, a] > .0001f) { chart[z, a] = sum[z, a] / weights[z, a]; known[z, a] = true; measured++; }
            // Complete the low-frequency marking chart, not the detailed source albedo.
            int interpolated = 0;
            for (int pass = 0; pass < Rings + Angles; pass++)
            {
                var next = (float[,])chart.Clone(); var nextKnown = (bool[,])known.Clone(); int added = 0;
                for (int z = 0; z < Rings; z++) for (int a = 0; a < Angles; a++) if (!known[z, a])
                {
                    float v = 0; int n = 0;
                    for (int j = -1; j <= 1; j += 2)
                    {
                        int zz = Mathf.Clamp(z + j, 0, Rings - 1), aa = (a + j + Angles) % Angles;
                        if (known[zz, a]) { v += chart[zz, a]; n++; }
                        if (known[z, aa]) { v += chart[z, aa]; n++; }
                    }
                    if (n > 0) { next[z, a] = v / n; nextKnown[z, a] = true; added++; }
                }
                chart = next; known = nextKnown; interpolated += added; if (added == 0) break;
            }
            for (int pass = 0; pass < 5; pass++)
            {
                var next = (float[,])chart.Clone();
                for (int z = 0; z < Rings; z++) for (int a = 0; a < Angles; a++) next[z, a] = (chart[z, a] * 4 + chart[Mathf.Max(0, z - 1), a] + chart[Mathf.Min(Rings - 1, z + 1), a] + chart[z, (a + 1) % Angles] + chart[z, (a + Angles - 1) % Angles]) / 8;
                chart = next;
            }
            if (measured < Rings * Angles / 2 || measured + interpolated != Rings * Angles) throw new InvalidOperationException("Insufficient chart coverage; refusing candidate");
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++) pixels[y * Size + x] = original.GetPixelBilinear((x + .5f) / Size, (y + .5f) / Size);
            var visited = new bool[pixels.Length]; var masks = new float[pixels.Length]; var protectedTexels = new bool[pixels.Length];
            int changed = 0, conflicts = 0;
            for (int f = 0; f < fry.Regions.Length; f++)
            {
                int ia = fry.T[f * 3], ib = fry.T[f * 3 + 1], ic = fry.T[f * 3 + 2];
                var u = fry.UV[ia] * Size; var v = fry.UV[ib] * Size; var w = fry.UV[ic] * Size;
                float det = (v.x - u.x) * (w.y - u.y) - (v.y - u.y) * (w.x - u.x);
                if (Mathf.Abs(det) < 1e-10f) continue;
                for (int y = Mathf.Max(0, Mathf.CeilToInt(Mathf.Min(u.y, v.y, w.y) - .5f)); y <= Mathf.Min(Size - 1, Mathf.FloorToInt(Mathf.Max(u.y, v.y, w.y) - .5f)); y++)
                for (int x = Mathf.Max(0, Mathf.CeilToInt(Mathf.Min(u.x, v.x, w.x) - .5f)); x <= Mathf.Min(Size - 1, Mathf.FloorToInt(Mathf.Max(u.x, v.x, w.x) - .5f)); x++)
                {
                    float px = x + .5f - u.x, py = y + .5f - u.y;
                    float b = (px * (w.y - u.y) - py * (w.x - u.x)) / det, c = ((v.x - u.x) * py - (v.y - u.y) * px) / det, a = 1 - b - c;
                    if (a < -1e-6f || b < -1e-6f || c < -1e-6f) continue;
                    int index = y * Size + x; var p = fry.V[ia] * a + fry.V[ib] * b + fry.V[ic] * c;
                    // Entire lateral face and all classified fins retain pristine Fry pixels.
                    bool protect = fry.Regions[f] != 0 || (p.z > .16f && p.y < .052f);
                    float mask = protect ? 0 : Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.30f, .70f, Sample(chart, fry.Coordinate(p.z), Angle(p))));
                    if (visited[index] && !protect && !protectedTexels[index] && Mathf.Abs(mask - masks[index]) > .1f) conflicts++;
                    protectedTexels[index] |= protect;
                    if (!visited[index]) masks[index] = mask;
                    visited[index] = true;
                }
            }
            if (conflicts > 200) throw new InvalidOperationException("Conflicting UV marking samples=" + conflicts + "; refusing candidate");
            for (int i = 0; i < pixels.Length; i++) if (visited[i] && !protectedTexels[i] && masks[i] > .001f)
            {
                Color c = pixels[i]; float detail = Mathf.Clamp(c.r, .15f, 1);
                var red = new Color(detail, detail * .075f, detail * .055f, c.a);
                pixels[i] = Color.Lerp(c, red, masks[i]); changed++;
            }
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false); tex.SetPixels32(pixels); tex.Apply();
            File.WriteAllBytes(output, tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(output, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(output); importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = Size; importer.SaveAndReimport();
            string report = "Fry-only red-pattern trial, pending visual approval\nAdult surface landmarks: peduncle=" + adult.Peduncle + " shoulder=" + adult.Shoulder + " nose=" + adult.Nose + "\nFry surface landmarks: peduncle=" + fry.Peduncle + " shoulder=" + fry.Shoulder + " nose=" + fry.Nose + "\nMeasured chart cells=" + measured + " interpolated=" + interpolated + " total=" + Rings * Angles + "\nConflicting UV samples=" + conflicts + " recolored texels=" + changed + "\nOriginal Fry albedo is the baseline; lateral face and classified fins unchanged. Only red-pattern chroma transferred; no ray projection or adult-eye texture sampling. Chart interpolation and mask smoothing are approximations requiring visual review.";
            File.WriteAllText(Folder + "/Reports/Kohaku_Fry_RedPattern_v02_Validation.txt", report);
            return report;
        }
        finally { UnityEngine.Object.DestroyImmediate(original); UnityEngine.Object.DestroyImmediate(source); }
    }
    public static string Capture()
    {
        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var camObject = new GameObject("Pattern review camera"); var cam = camObject.AddComponent<Camera>();
        cam.orthographic = true; cam.cullingMask = 1 << 31; cam.nearClipPlane = .01f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(.18f, .18f, .18f);
        var lightObject = new GameObject("Pattern review light"); var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.2f; light.cullingMask = 1 << 31; lightObject.transform.rotation = Quaternion.Euler(40, -25, 0);
        var rt = new RenderTexture(768,768,24); cam.targetTexture = rt;
        var atlas = new Texture2D(2304,2304,TextureFormat.RGB24,false);
        Vector3[] views = { new Vector3(1,.1f,0), Vector3.up, new Vector3(1,.6f,1) };
        try
        {
            for (int row = 0; row < 3; row++)
            {
                var go = KohakuCandidateBake.CopyForReview(AssetDatabase.LoadAssetAtPath<GameObject>(row == 0 ? EarlyStageAlbedoTransferBuilder.Adult : EarlyStageAlbedoTransferBuilder.Fry));
                foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
                var skin = go.GetComponentInChildren<SkinnedMeshRenderer>();
                var material = new Material(row == 0 ? AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Koi/ProofOfConcept/KohakuPbrTopologyTransfer/Kohaku_OriginalMeshy_ToRigged_PBR_Test.mat") : skin.sharedMaterial);
                if (row == 2) material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Textures/Kohaku_Fry_RedPattern_Candidate_v02.png"));
                skin.sharedMaterial = material; var bounds = skin.bounds;
                cam.orthographicSize = Mathf.Max(bounds.size.x, bounds.size.z) * .65f;
                for (int col = 0; col < 3; col++)
                {
                    camObject.transform.position = bounds.center + views[col].normalized * 2;
                    camObject.transform.LookAt(bounds.center, col == 1 ? Vector3.forward : Vector3.up);
                    cam.Render(); RenderTexture.active = rt; atlas.ReadPixels(new Rect(0,0,768,768), col * 768, (2-row) * 768);
                }
                UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(material);
            }
            atlas.Apply(); string path = Folder + "/Reports/Kohaku_Fry_RedPattern_v02_Comparisons.png";
            File.WriteAllBytes(path,atlas.EncodeToPNG()); AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path).GetEntityId().ToString();
        }
        finally
        {
            RenderTexture.active = null; cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(camObject); UnityEngine.Object.DestroyImmediate(lightObject); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(atlas);
            EditorSceneManager.CloseScene(scene,true); SceneManager.SetActiveScene(previous);
        }
    }
}
