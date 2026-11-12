using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using KoiPond.EarlyStageTransferPilot;

public static class KohakuCandidateBake
{
    const string Folder = EarlyStageAlbedoTransferBuilder.Folder;
    const int Size = 2048;
    public static GameObject CopyForReview(GameObject source)
    {
        return (GameObject)typeof(EarlyStageAlbedoTransferBuilder).GetMethod("CopyVisual", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { source });
    }
    sealed class Surface
    {
        public Vector3[] V, Bones;
        public Vector2[] UV;
        public int[] T, Regions;
        public Bounds Bounds;
        public Surface(string path)
        {
            // Reuse the established prototype anatomy classification without changing it.
            var type = typeof(EarlyStageAlbedoTransferBuilder).GetNestedType("Surface", BindingFlags.NonPublic);
            var data = Activator.CreateInstance(type, new object[] { path });
            V = (Vector3[])type.GetField("V").GetValue(data);
            Bones = (Vector3[])type.GetField("Bones").GetValue(data);
            UV = (Vector2[])type.GetField("UV").GetValue(data);
            T = (int[])type.GetField("T").GetValue(data);
            Regions = (int[])type.GetField("Regions").GetValue(data);
            Bounds = (Bounds)type.GetField("Bounds").GetValue(data);
        }
    }
    static Texture2D Decode(string path)
    {
        var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!t.LoadImage(File.ReadAllBytes(path))) throw new InvalidOperationException(path);
        return t;
    }
    static float MapZ(Surface d, Surface a, float z)
    {
        float[] dz = { -.17f, d.Bones[6].z, d.Bones[1].z, d.Bones[2].z, d.Bounds.max.z };
        float[] az = { -.17f, a.Bones[6].z, a.Bones[1].z, a.Bones[2].z, a.Bounds.max.z };
        int i = 0;
        while (i < dz.Length - 2 && z > dz[i + 1]) i++;
        return Mathf.LerpUnclamped(az[i], az[i + 1], (z - dz[i]) / (dz[i + 1] - dz[i]));
    }
    static bool Protected(Surface s, Vector3[] eyes, Vector3 p)
    {
        float scale = s.Bounds.size.x > .28f ? 1 : .8f;
        foreach (var eye in eyes)
        {
            var q = p - eye;
            q = new Vector3(q.x / (.064f * scale), q.y / (.060f * scale), q.z / (.067f * scale));
            if (q.sqrMagnitude <= 1 && p.y < .048f) return true;
        }
        var m = p - s.Bones[3];
        return m.x * m.x / (.070f * .070f) + m.y * m.y / (.043f * .043f) + m.z * m.z / (.041f * .041f) <= 1 && p.y < -.013f;
    }
    static Vector3[] Eyes(Surface s, Texture2D original)
    {
        var sum = new Vector3[2]; var count = new int[2];
        for (int i = 0; i < s.V.Length; i++)
        {
            var p = s.V[i];
            if (p.z < s.Bones[2].z - .065f || p.z > s.Bones[3].z - .025f || Mathf.Abs(p.x) < .04f || p.y < -.035f) continue;
            var c = original.GetPixelBilinear(s.UV[i].x, s.UV[i].y);
            if (c.r > .2f || c.g > .2f || c.b > .2f) continue;
            int side = p.x < 0 ? 0 : 1; sum[side] += p; count[side]++;
        }
        for (int i = 0; i < 2; i++) { if (count[i] == 0) throw new InvalidOperationException("Eye landmark absent"); sum[i] /= count[i]; }
        return sum;
    }
    public static void Bake(string stage)
    {
        if (stage != "Fry" && stage != "Juvenile") throw new ArgumentException(stage);
        string output = Folder + "/Textures/Kohaku_" + stage + "_Albedo_Candidate_v01.png";
        if (File.Exists(output)) throw new InvalidOperationException("Refusing to overwrite candidate: " + output);
        var a = new Surface(EarlyStageAlbedoTransferBuilder.Adult);
        string prefab = stage == "Fry" ? EarlyStageAlbedoTransferBuilder.Fry : EarlyStageAlbedoTransferBuilder.Juvenile;
        var d = new Surface(prefab);
        var skin = AssetDatabase.LoadAssetAtPath<GameObject>(prefab).GetComponentInChildren<SkinnedMeshRenderer>();
        var original = Decode(AssetDatabase.GetAssetPath(skin.sharedMaterial.GetTexture("_BaseMap")));
        var eyes = Eyes(d, original);
        var source = Decode("Assets/Models/Koi/ProofOfConcept/KohakuTopologyTransfer/Kohaku_OriginalMeshy_ToRigged_BaseColor.png");
        var baseline = Decode(Folder + "/Textures/Kohaku_" + stage + "_Albedo_DERIVED.png");
        var pixels = baseline.GetPixels32();
        var triangles = new List<int>();
        for (int f = 0; f < a.Regions.Length; f++) if (a.Regions[f] == 0) { triangles.Add(a.T[f * 3]); triangles.Add(a.T[f * 3 + 1]); triangles.Add(a.T[f * 3 + 2]); }
        var mesh = new Mesh(); mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = a.V; mesh.uv = a.UV; mesh.triangles = triangles.ToArray(); mesh.RecalculateBounds();
        var go = new GameObject("Temporary Kohaku anatomical projection"); go.hideFlags = HideFlags.HideAndDontSave;
        var collider = go.AddComponent<MeshCollider>(); collider.sharedMesh = mesh;
        var seen = new bool[pixels.Length]; var occupied = new bool[pixels.Length];
        int transferred = 0, protectedPixels = 0, misses = 0, eyeExcluded = 0;
        try
        {
            for (int f = 0; f < d.Regions.Length; f++)
            {
                int ia = d.T[f * 3], ib = d.T[f * 3 + 1], ic = d.T[f * 3 + 2];
                var u = d.UV[ia] * Size; var v = d.UV[ib] * Size; var w = d.UV[ic] * Size;
                float det = (v.x - u.x) * (w.y - u.y) - (v.y - u.y) * (w.x - u.x);
                if (Mathf.Abs(det) < 1e-10f) continue;
                int x0 = Mathf.Max(0, Mathf.CeilToInt(Mathf.Min(u.x, v.x, w.x) - .5f));
                int x1 = Mathf.Min(Size - 1, Mathf.FloorToInt(Mathf.Max(u.x, v.x, w.x) - .5f));
                int y0 = Mathf.Max(0, Mathf.CeilToInt(Mathf.Min(u.y, v.y, w.y) - .5f));
                int y1 = Mathf.Min(Size - 1, Mathf.FloorToInt(Mathf.Max(u.y, v.y, w.y) - .5f));
                for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)
                {
                    float px = x + .5f - u.x, py = y + .5f - u.y;
                    float b = (px * (w.y - u.y) - py * (w.x - u.x)) / det;
                    float c = ((v.x - u.x) * py - (v.y - u.y) * px) / det; float aa = 1 - b - c;
                    if (aa < -1e-6f || b < -1e-6f || c < -1e-6f) continue;
                    int index = y * Size + x; occupied[index] = true;
                    if (seen[index]) continue; seen[index] = true;
                    var p = d.V[ia] * aa + d.V[ib] * b + d.V[ic] * c;
                    if (d.Regions[f] != 0 || Protected(d, eyes, p)) { protectedPixels++; continue; }
                    float z = MapZ(d, a, p.z);
                    var radial = new Vector3(p.x, p.y, 0).normalized;
                    if (radial.sqrMagnitude < .5f) { misses++; continue; }
                    var center = new Vector3(0, 0, z);
                    RaycastHit hit;
                    if (!collider.Raycast(new Ray(center + radial, -radial), out hit, 1.1f)) { misses++; pixels[index] = new Color32(255, 0, 255, 255); continue; }
                    var q = hit.point;
                    if (Mathf.Abs(q.x) > .040f && q.z > .183f && q.z < .281f && q.y > -.049f && q.y < .048f) { eyeExcluded++; continue; }
                    pixels[index] = source.GetPixelBilinear(hit.textureCoord.x, hit.textureCoord.y); transferred++;
                }
            }
            // Only empty atlas space receives padding; protected and failed surface pixels stay untouched.
            for (int pass = 0; pass < 8; pass++)
            {
                var add = new List<int>();
                for (int y = 1; y < Size - 1; y++) for (int x = 1; x < Size - 1; x++)
                {
                    int i = y * Size + x; if (occupied[i]) continue;
                    foreach (int j in new[] { i - 1, i + 1, i - Size, i + Size }) if (occupied[j]) { pixels[i] = pixels[j]; add.Add(i); break; }
                }
                foreach (int i in add) occupied[i] = true;
            }
            var result = new Texture2D(Size, Size, TextureFormat.RGBA32, false); result.SetPixels32(pixels); result.Apply();
            File.WriteAllBytes(output, result.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(result);
            File.WriteAllText(Folder + "/Reports/Kohaku_" + stage + "_Candidate_v01.txt", "Step3B candidate, NOT approved\nContinuous radial surface projection; bone-anchored longitudinal mapping; existing face/fin pixels retained.\ntransferred=" + transferred + "\nprotected=" + protectedPixels + "\nprojectionMisses=" + misses + "\nadultEyeExcluded=" + eyeExcluded);
            AssetDatabase.ImportAsset(output, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(output); importer.sRGBTexture = true; importer.maxTextureSize = Size; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            Debug.Log("Kohaku " + stage + " candidate: transferred=" + transferred + " protected=" + protectedPixels + " misses=" + misses + " eyeExcluded=" + eyeExcluded);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(mesh);
            UnityEngine.Object.DestroyImmediate(original); UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(baseline);
        }
    }
}
