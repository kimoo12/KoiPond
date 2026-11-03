using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace KoiPond.Editor
{
    public static class ShiroUvBaker
    {
        public const string Folder = "Assets/Models/Koi/ShiroUtsuri_Recreated";
        private const int Size = 2048;
        private const float ProjectionSpan = 0.68f;
        private static Texture2D _side;
        private static Texture2D _top;
        private static readonly Color White = new Color32(242, 240, 234, 255);
        private static readonly Color Black = new Color32(25, 26, 28, 255);

        public static void Bake()
        {
            _side = ReadSource("SideColorSource");
            _top = ReadSource("TopColorSource");
            var mesh = AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>("Assets/Models/Koi/Meshes/KoiFish_RiggedMesh.asset");
            if (mesh == null || mesh.uv.Length != mesh.vertexCount)
                throw new InvalidOperationException("The original mesh must have valid UVs.");
            var vertices = mesh.vertices;
            var uv = mesh.uv;
            var triangles = mesh.triangles;
            var weights = mesh.boneWeights;
            var pixels = new Color32[Size * Size];
            var surface = new Color32[pixels.Length];
            var covered = new bool[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = White;
                surface[i] = new Color32(0, 0, 0, 110);
            }
            for (int t = 0; t < triangles.Length; t += 3)
            {
                int ia = triangles[t], ib = triangles[t + 1], ic = triangles[t + 2];
                Vector2 a = uv[ia] * Size, b = uv[ib] * Size, c = uv[ic] * Size;
                float d = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
                if (Mathf.Abs(d) < 0.000001f) continue;
                int x0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x))), 0, Size - 1);
                int x1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x))), 0, Size - 1);
                int y0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y))), 0, Size - 1);
                int y1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y))), 0, Size - 1);
                float fa = FinWeight(weights[ia]), fb = FinWeight(weights[ib]), fc = FinWeight(weights[ic]);
                for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float px = x + 0.5f, py = y + 0.5f;
                    float wa = ((b.y - c.y) * (px - c.x) + (c.x - b.x) * (py - c.y)) / d;
                    float wb = ((c.y - a.y) * (px - c.x) + (a.x - c.x) * (py - c.y)) / d;
                    float wc = 1f - wa - wb;
                    if (wa < -0.00001f || wb < -0.00001f || wc < -0.00001f) continue;
                    Vector3 p = vertices[ia] * wa + vertices[ib] * wb + vertices[ic] * wc;
                    Evaluate(p, fa * wa + fb * wb + fc * wc, out Color color, out float smoothness);
                    int index = y * Size + x;
                    pixels[index] = color;
                    surface[index] = new Color32(0, 0, 0, (byte)Mathf.RoundToInt(smoothness * 255));
                    covered[index] = true;
                }
            }
            // Continuous object-space pigment is evaluated identically on every UV island.
            // Extend island borders to keep filtering and mipmaps from introducing white gaps.
            for (int pass = 0; pass < 16; pass++)
            {
                var next = (bool[])covered.Clone();
                for (int y = 1; y < Size - 1; y++)
                for (int x = 1; x < Size - 1; x++)
                {
                    int i = y * Size + x;
                    if (covered[i]) continue;
                    int source = covered[i - 1] ? i - 1 : covered[i + 1] ? i + 1 : covered[i - Size] ? i - Size : covered[i + Size] ? i + Size : -1;
                    if (source < 0) continue;
                    pixels[i] = pixels[source];
                    surface[i] = surface[source];
                    next[i] = true;
                }
                covered = next;
            }
            Write("Shiro_Albedo", pixels, true, false);
            Write("Shiro_MetallicSmoothness", surface, false, false);
            var roughness = new Color32[pixels.Length];
            var normal = new Color32[pixels.Length];
            var height = new Color32[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                byte rough = (byte)(255 - surface[i].a);
                roughness[i] = new Color32(rough, rough, rough, 255);
                normal[i] = new Color32(128, 128, 255, 255);
                height[i] = new Color32(128, 128, 128, 255);
            }
            Write("Shiro_Roughness", roughness, false, false);
            Write("Shiro_Normal", normal, false, true);
            Write("Shiro_Height", height, false, false);
            var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Shiro_Recreated.mat");
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, Folder + "/Shiro_Recreated.mat");
            }
            Undo.RecordObject(material, "Configure recreated Shiro surface");
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Shiro_Albedo.png"));
            material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Shiro_MetallicSmoothness.png"));
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Shiro_Normal.png"));
            material.SetTexture("_ParallaxMap", null);
            material.SetTexture("_DetailNormalMap", null);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 1f);
            material.SetFloat("_SmoothnessTextureChannel", 0f);
            material.SetFloat("_BumpScale", 0f);
            material.SetFloat("_Parallax", 0f);
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.DisableKeyword("_NORMALMAP");
            material.DisableKeyword("_PARALLAXMAP");
            material.DisableKeyword("_DETAIL_MULX2");
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            UnityEngine.Object.DestroyImmediate(_side);
            UnityEngine.Object.DestroyImmediate(_top);
            _side = null;
            _top = null;
        }

        private static float FinWeight(BoneWeight weight)
        {
            return Contribution(weight.boneIndex0, weight.weight0) + Contribution(weight.boneIndex1, weight.weight1)
                + Contribution(weight.boneIndex2, weight.weight2) + Contribution(weight.boneIndex3, weight.weight3);
        }

        private static float Contribution(int bone, float weight)
        {
            return bone == 4 || bone == 5 || bone == 9 ? weight : 0f;
        }

        private static void Evaluate(Vector3 point, float finWeight, out Color color, out float smoothness)
        {
            Vector3 p = point;
            p.x = Mathf.Abs(p.x);
            bool dorsal = p.x < 0.016f && p.y > 0.063f && p.z > -0.115f && p.z < 0.133f;
            bool ventral = p.y < -0.083f && p.z < 0.035f;
            bool fin = finWeight > 0.35f || p.z < -0.178f || dorsal || ventral || p.y > 0.099f || p.y < -0.11f;
            Color side = Sample(_side, 0.5f + p.z / ProjectionSpan, 0.5f + p.y / ProjectionSpan, fin ? 0 : 2);
// The dorsal fin occludes the centreline in the top guide. Sample either flank,
            // not the fin's silhouette, when determining the underlying body's pigment.
            float tx = Mathf.Max(p.x, p.z < 0.12f ? 0.026f : p.x);
            Color top = Sample(_top, 0.5f + tx / ProjectionSpan, 0.5f - p.z / ProjectionSpan, 4);
            float topWeight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.025f, 0.075f, p.y));
            float value = Mathf.Lerp(side.grayscale, top.grayscale, topWeight);
            float ink = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.31f, 0.56f, value));
            if (fin)
            {
                float ray = Mathf.Clamp01(side.grayscale * 1.8f);
                color = Color.Lerp(new Color32(25, 26, 28, 255), new Color32(52, 51, 49, 255), ray);
                smoothness = 0.36f;
                return;
            }
            // Keep the belly white without projecting the pectoral-fin silhouette onto it.
            if (p.y < -0.06f) ink *= Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.105f, -0.06f, p.y));
            // Both freshly painted references contain the connected head bridge.
            // Preserve their curved white forehead boundary rather than imposing a planar cutoff.
            color = Color.Lerp(White, Black, ink);
smoothness = 0.43f;
            // Circular reference eyes are reconstructed in object space on both sides.
            var axis = new Vector3(0.92f, 0.08f, 0.38f).normalized;
            var horizontal = Vector3.Cross(Vector3.up, axis).normalized;
            var vertical = Vector3.Cross(axis, horizontal).normalized;
            Vector3 delta = p - new Vector3(0.0746f, -0.0012f, 0.2279f);
            float ex = Vector3.Dot(delta, horizontal) / 0.0242f;
            float ey = Vector3.Dot(delta, vertical) / 0.0245f;
            float radius = Mathf.Sqrt(ex * ex + ey * ey);
            float depth = Vector3.Dot(delta, axis);
            if (radius < 1.1f && depth > -0.012f && depth < 0.018f)
            {
                float rim = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.99f, 1.07f, radius));
                color = Color.Lerp(color, new Color32(227, 219, 207, 255), rim);
                float pupilRadius = Mathf.Sqrt(Mathf.Pow((ex - 0.03f) * 0.0242f, 2f) + Mathf.Pow(ey * 0.0245f, 2f)) / 0.015f;
                float pupil = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.95f, 1.03f, pupilRadius));
                color = Color.Lerp(color, new Color32(17, 17, 20, 255), pupil);
                float glintRadius = Mathf.Sqrt((ex + 0.16f) * (ex + 0.16f) + (ey - 0.3f) * (ey - 0.3f));
                float glint = (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.06f, 0.12f, glintRadius))) * pupil;
                color = Color.Lerp(color, White, glint);
                smoothness = Mathf.Lerp(smoothness, 0.76f, rim);
            }
            float nostrilDistance = Vector3.Distance(p, new Vector3(0.0309f, -0.0028f, 0.2706f));
            float nostril = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.0018f, 0.0036f, nostrilDistance));
            color = Color.Lerp(color, new Color32(69, 67, 63, 255), nostril);
        }

        private static Color Sample(Texture2D source, float u, float v, int radius)
        {
            Color color = Color.clear;
            int count = 0;
            for (int y = -1; y <= 1; y++)
            for (int x = -1; x <= 1; x++)
            {
                color += source.GetPixelBilinear(u + x * radius / (float)source.width, v + y * radius / (float)source.height);
                count++;
            }
            return color / count;
        }

        private static Texture2D ReadSource(string name)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(Folder + "/" + name + ".png")))
                throw new InvalidOperationException("Unable to read generated source: " + name);
            return texture;
        }

        private static void Write(string name, Color32[] pixels, bool srgb, bool normal)
        {
            string path = Folder + "/" + name + ".png";
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false, !srgb);
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = srgb;
            importer.maxTextureSize = Size;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear;
            importer.mipmapEnabled = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.SaveAndReimport();
        }
    }
}
