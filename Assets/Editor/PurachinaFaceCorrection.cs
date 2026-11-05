using UnityEngine;
using UnityEditor;
using System.IO;

public static class PurachinaFaceCorrection
{
    public static void CreatePreview()
    {
        const string folder = "Assets/Archive/Koi/Purachina_New/";
        var original = new Texture2D(2, 2);
        original.LoadImage(File.ReadAllBytes(folder + "Purachina_Albedo.png"));
        int size = original.width;
        var before = original.GetPixels();
        var pixels = (Color[])before.Clone();
        var edited = new bool[pixels.Length];
        var mesh = AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>("Assets/Models/Koi/Meshes/KoiFish_RiggedMesh.asset");
        var v = mesh.vertices; var uv = mesh.uv; var tri = mesh.triangles;
        int eyes = 0, nostrils = 0;
        for (int t = 0; t < tri.Length; t += 3)
        {
            int ia = tri[t], ib = tri[t + 1], ic = tri[t + 2];
            if (Mathf.Max(v[ia].z, Mathf.Max(v[ib].z, v[ic].z)) < 0.20f) continue;
            Vector2 a = uv[ia] * size, b = uv[ib] * size, c = uv[ic] * size;
            float den = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
            if (Mathf.Abs(den) < 0.000001f) continue;
            int x0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))), 0, size - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))), 0, size - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))), 0, size - 1);
            int y1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))), 0, size - 1);
            for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)
            {
                int index = y * size + x;
                if (edited[index] || before[index].r > 0.3f) continue;
                float wa = ((b.y - c.y) * (x + 0.5f - c.x) + (c.x - b.x) * (y + 0.5f - c.y)) / den;
                float wb = ((c.y - a.y) * (x + 0.5f - c.x) + (a.x - c.x) * (y + 0.5f - c.y)) / den;
                float wc = 1f - wa - wb;
                if (wa < 0f || wb < 0f || wc < 0f) continue;
                Vector3 p = v[ia] * wa + v[ib] * wb + v[ic] * wc;
                float ax = Mathf.Abs(p.x);
                if (p.z > 0.20f && p.z < 0.254f && ax > 0.048f && Mathf.Abs(p.y) < 0.03f)
                {
                    // Mirror both eyes in surface coordinates; retain the existing painted silhouette.
                    float u = (-(ax - 0.066f) * 0.55f + (p.z - 0.228f) * 0.835f) / 0.026f;
                    float w = (p.y + 0.0015f) / 0.024f;
                    float r = Mathf.Sqrt(u * u + w * w);
                    Color pupil = new Color(0.035f, 0.032f, 0.031f, 1f);
                    Color iris = new Color(0.29f, 0.27f, 0.28f, 1f);
                    Color rim = new Color(0.91f, 0.91f, 0.93f, 1f);
                    Color color = Color.Lerp(pupil, iris, Mathf.SmoothStep(0f, 1f, (r - 0.38f) / 0.18f));
                    color = Color.Lerp(color, rim, Mathf.SmoothStep(0f, 1f, (r - 0.70f) / 0.12f));
                    color = Color.Lerp(color, new Color(0.98f, 0.98f, 0.99f, 1f), Mathf.SmoothStep(0f, 1f, (r - 0.89f) / 0.10f));
                    float highlight = Vector2.Distance(new Vector2(u, w), new Vector2(-0.23f, 0.32f));
                    color = Color.Lerp(color, Color.white, 1f - Mathf.SmoothStep(0f, 1f, (highlight - 0.10f) / 0.07f));
                    float secondary = Vector2.Distance(new Vector2(u, w), new Vector2(0.24f, -0.23f));
                    color = Color.Lerp(color, new Color(0.7f, 0.7f, 0.72f, 1f), (1f - Mathf.SmoothStep(0f, 1f, (secondary - 0.035f) / 0.03f)) * 0.7f);
                    pixels[index] = color; edited[index] = true; eyes++;
                }
                else if (p.z > 0.263f && p.z < 0.279f && ax > 0.026f && ax < 0.042f && p.y > -0.013f && p.y < 0.009f)
                {
                    float r = Mathf.Sqrt(Mathf.Pow((p.z - 0.271f) / 0.005f, 2f) + Mathf.Pow((p.y + 0.002f) / 0.0055f, 2f));
                    float shade = Mathf.Lerp(0.58f, 0.86f, Mathf.SmoothStep(0.1f, 1f, r));
                    pixels[index] = new Color(shade, shade, shade + 0.008f, 1f); edited[index] = true; nostrils++;
                }
            }
        }
        original.SetPixels(pixels); original.Apply();
        string path = folder + "Purachina_FaceCorrected.png";
        File.WriteAllBytes(path, original.EncodeToPNG());
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.maxTextureSize = size; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp; importer.SaveAndReimport();
        if (AssetDatabase.LoadAssetAtPath<Material>(folder + "Purachina_FacePreview.mat") == null)
        {
            var material = new Material(AssetDatabase.LoadAssetAtPath<Material>(folder + "Purachina_Platinum.mat"));
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            AssetDatabase.CreateAsset(material, folder + "Purachina_FacePreview.mat");
        }
        AssetDatabase.SaveAssets();
        Object.DestroyImmediate(original);
        Debug.Log("Face preview created: eye pixels=" + eyes + ", nostril pixels=" + nostrils + "; all other texels unchanged. Original albedo retained as backup.");
    }
}
