using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;

public static class KohakuTopologyTransferPoC
{
    private const string RigPath= "Assets/Models/Koi/Meshes/KoiFish_RiggedMesh.asset";
    private const string TaishoPath = "Assets/Models/Koi/Materials/taisho sanke koi texture/Meshy_AI_Koi_Fish_20261021_image-to-3d-texture.fbx";
    private const string KohakuPath = "Assets/Models/Koi/Materials/kohaku koi texture/Meshy_AI_kohaku_koi_textured_1025023033_texture.fbx";
    private const string SourcePath = "Assets/Models/Koi/Materials/kohaku koi texture/kohaku koi_texture.png";
    private const string Folder = "Assets/Models/Koi/ProofOfConcept/KohakuTopologyTransfer";
    private const int Resolution = 4096;
    private const float Cell = 1e-7f;
    private static Vector3Int Key(Vector3 p) => new Vector3Int(Mathf.RoundToInt(p.x / Cell), Mathf.RoundToInt(p.y / Cell), Mathf.RoundToInt(p.z / Cell));
    private static (int, int, int) TriKey(int a, int b, int c)
    {
        if (a > b) { int q = a; a = b; b = q; }
        if (b > c) { int q = b; b = c; c = q; }
        if (a > b) { int q = a; a = b; b = q; }
        return (a, b, c);
    }
    private static string Hash(string p)
    {
        using (var h = SHA256.Create()) return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(p))).Replace("-", "");
    }
    private static Color32 Sample(Color32[] pixels, int width, int height, Vector2 uv)
    {
        // Texel centers at (i + 0.5) / size. Interpolate original sRGB bytes;
        // no GPU color-space conversion, compression, tint, or pattern synthesis.
        float x = Mathf.Clamp(uv.x * width - 0.5f, 0, width - 1);
        float y = Mathf.Clamp(uv.y * height - 0.5f, 0, height - 1);
        int x0 = (int)x, y0 = (int)y, x1 = Math.Min(x0 + 1, width - 1), y1 = Math.Min(y0 + 1, height - 1);
        float fx = x - x0, fy = y - y0;
        Color a = Color.LerpUnclamped(pixels[y0 * width + x0], pixels[y0 * width + x1], fx);
        Color b = Color.LerpUnclamped(pixels[y1 * width + x0], pixels[y1 * width + x1], fx);
        return Color.LerpUnclamped(a, b, fy);
    }
    private static void SaveImage(string path, Color32[] pixels, int size)
    {
        var image = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
        try { image.SetPixels32(pixels); image.Apply(false); File.WriteAllBytes(path, image.EncodeToPNG()); }
        finally { UnityEngine.Object.DestroyImmediate(image); }
    }

    public static string Bake()
    {
        string[] protectedPaths = { RigPath, TaishoPath, KohakuPath, SourcePath, "Assets/Models/Koi/Materials/Kohaku_Koi_MAT.mat" };
        var hashes = protectedPaths.ToDictionary(p => p, Hash);
        var rig = AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(RigPath);
        var taisho = AssetDatabase.LoadAllAssetsAtPath(TaishoPath).OfType<UnityEngine.Mesh>().First();
        var kohaku = AssetDatabase.LoadAllAssetsAtPath(KohakuPath).OfType<UnityEngine.Mesh>().First();
        var rv = rig.vertices; var tv = taisho.vertices; var kv = kohaku.vertices;
        var ri = rig.triangles; var ti = taisho.triangles; var ki = kohaku.triangles;
        var targetUV = rig.uv; var sourceUV = kohaku.uv;
        if (!ri.SequenceEqual(ti) || !targetUV.SequenceEqual(taisho.uv) || ri.Length != ki.Length)
            throw new InvalidOperationException("Verified Taisho/rigged correspondence no longer holds.");
        if (Directory.Exists(Folder)) throw new InvalidOperationException("Output folder already exists; refusing to overwrite this proof of concept.");

        var buckets = new Dictionary<Vector3Int, List<int>>();
        var roots = new int[tv.Length];
        for (int i = 0; i < tv.Length; i++)
        {
            var key = Key(tv[i]);
            if (!buckets.TryGetValue(key, out var list)) buckets[key] = list = new List<int>();
            roots[i] = i;
            foreach (int j in list) if ((tv[j] - tv[i]).sqrMagnitude < 1e-20f) { roots[i] = roots[j]; break; }
            list.Add(i);
        }
        var scale = new Vector3(taisho.bounds.size.x / kohaku.bounds.size.x, taisho.bounds.size.y / kohaku.bounds.size.y, taisho.bounds.size.z / kohaku.bounds.size.z);
        var kohakuRoots = new int[kv.Length];
        double residualSum = 0; float residualMax = 0;
        for (int i = 0; i < kv.Length; i++)
        {
            var p = Vector3.Scale(kv[i] - kohaku.bounds.center, scale) + taisho.bounds.center;
            var key = Key(p); float best = float.MaxValue; int found = -1;
            for (int x = -1; x <= 1; x++) for (int y = -1; y <= 1; y++) for (int z = -1; z <= 1; z++)
                if (buckets.TryGetValue(key + new Vector3Int(x, y, z), out var list))
                    foreach (int j in list) { float d = (tv[j] - p).sqrMagnitude; if (d < best) { best = d; found = roots[j]; } }
            if (found < 0 || best > 1e-14f) throw new InvalidOperationException("Unmatched Kohaku vertex " + i);
            kohakuRoots[i] = found; residualSum += best; residualMax = Mathf.Max(residualMax, Mathf.Sqrt(best));
        }
        var triangles = new Dictionary<(int, int, int), int>();
        for (int i = 0; i < ti.Length; i += 3)
            triangles.Add(TriKey(roots[ti[i]], roots[ti[i + 1]], roots[ti[i + 2]]), i / 3);
        // For every destination triangle corner, retain the ORIGINAL Kohaku UV.
        var cornerUV = new Vector2[ri.Length]; var paired = new bool[ri.Length / 3];
        var sourceCornerIds = new int[ri.Length];
        for (int i = 0; i < ki.Length; i += 3)
        {
            if (!triangles.TryGetValue(TriKey(kohakuRoots[ki[i]], kohakuRoots[ki[i + 1]], kohakuRoots[ki[i + 2]]), out int tri) || paired[tri])
                throw new InvalidOperationException("Missing or ambiguous Kohaku triangle " + i / 3);
            paired[tri] = true;
            for (int c = 0; c < 3; c++)
            {
                int root = roots[ti[tri * 3 + c]], sourceCorner = -1;
                for (int j = 0; j < 3; j++) if (kohakuRoots[ki[i + j]] == root) sourceCorner = ki[i + j];
                if (sourceCorner < 0) throw new InvalidOperationException("Triangle corner mismatch.");
                cornerUV[tri * 3 + c] = sourceUV[sourceCorner]; sourceCornerIds[tri * 3 + c] = sourceCorner;
            }
        }
        if (paired.Any(v => !v)) throw new InvalidOperationException("Unpaired destination triangles.");

        var source = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
        if (!ImageConversion.LoadImage(source, File.ReadAllBytes(SourcePath), false)) throw new InvalidOperationException("Cannot decode original Meshy PNG.");
        int sourceWidth = source.width, sourceHeight = source.height;
        var sourcePixels = source.GetPixels32(); UnityEngine.Object.DestroyImmediate(source);
        int count = Resolution * Resolution;
        var colors = new Color32[count]; var owner = new int[count];
        for (int i = 0; i < count; i++) owner[i] = -1;
        int degenerate = 0, noPixelCenters = 0, overlapEvents = 0, conflictEvents = 0, covered = 0;
        var conflicts = new HashSet<int>();
        double area = 0, colorDiffSum = 0; int colorDiffMax = 0;
        for (int i = 0; i < ri.Length; i += 3)
        {
            Vector2 a = targetUV[ri[i]] * Resolution, b = targetUV[ri[i + 1]] * Resolution, c = targetUV[ri[i + 2]] * Resolution;
            double det = (double)(b.x - a.x) * (c.y - a.y) - (double)(b.y - a.y) * (c.x - a.x);
            area += Math.Abs(det) / (2.0 * Resolution * Resolution);
            if (Math.Abs(det) < 1e-12) { degenerate++; continue; }
            int minX = Math.Max(0, Mathf.CeilToInt(Mathf.Min(a.x, b.x, c.x) - 0.5f));
            int maxX = Math.Min(Resolution - 1, Mathf.FloorToInt(Mathf.Max(a.x, b.x, c.x) - 0.5f));
            int minY = Math.Max(0, Mathf.CeilToInt(Mathf.Min(a.y, b.y, c.y) - 0.5f));
            int maxY = Math.Min(Resolution - 1, Mathf.FloorToInt(Mathf.Max(a.y, b.y, c.y) - 0.5f));
            int hits = 0;
            for (int y = minY; y <= maxY; y++) for (int x = minX; x <= maxX; x++)
            {
                double px = x + 0.5 - a.x, py = y + 0.5 - a.y;
                double wb = (px * (c.y - a.y) - py * (c.x - a.x)) / det;
                double wc = ((b.x - a.x) * py - (b.y - a.y) * px) / det;
                double wa = 1 - wb - wc;
                if (wa < -1e-9 || wb < -1e-9 || wc < -1e-9) continue;
                hits++;
                Vector2 uv = cornerUV[i] * (float)wa + cornerUV[i + 1] * (float)wb + cornerUV[i + 2] * (float)wc;
                Color32 color = Sample(sourcePixels, sourceWidth, sourceHeight, uv); int pixel = y * Resolution + x;
                if (owner[pixel] >= 0)
                {
                    overlapEvents++;
                    int diff = Math.Max(Math.Abs(colors[pixel].r - color.r), Math.Max(Math.Abs(colors[pixel].g - color.g), Math.Abs(colors[pixel].b - color.b)));
                    colorDiffMax = Math.Max(colorDiffMax, diff); colorDiffSum += diff;
                    if (diff > 2) { conflictEvents++; conflicts.Add(pixel); }
                    continue; // Never average incompatible markings from overlapping UVs.
                }
                owner[pixel] = i / 3; colors[pixel] = color; covered++;
            }
            if (hits == 0) noPixelCenters++;
        }
        // Eight texels of nearest-edge bleed OUTSIDE UV interiors only.
        // This is sampling protection, not filling/repainting a surface region.
        int padded = 0;
        var frontier = new List<int>();
        for (int y = 0; y < Resolution; y++) for (int x = 0; x < Resolution; x++)
        {
            int p = y * Resolution + x;
            if (owner[p] < 0) continue;
            if ((x > 0 && owner[p - 1] < 0) || (x < Resolution - 1 && owner[p + 1] < 0) || (y > 0 && owner[p - Resolution] < 0) || (y < Resolution - 1 && owner[p + Resolution] < 0)) frontier.Add(p);
        }
        for (int step = 0; step < 8; step++)
        {
            var next = new List<int>();
            foreach (int p in frontier)
            {
                int x = p % Resolution, y = p / Resolution;
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= Resolution || ny >= Resolution) continue;
                    int q = ny * Resolution + nx;
                    if (owner[q] != -1) continue;
                    owner[q] = -2 - step; colors[q] = colors[p]; next.Add(q); padded++;
                }
            }
            frontier = next;
        }
        Directory.CreateDirectory(Folder);
        string texturePath = Folder + "/Kohaku_OriginalMeshy_ToRigged_BaseColor.png";
        string materialPath = Folder + "/Kohaku_OriginalMeshy_ToRigged_Test.mat";
        SaveImage(texturePath, colors, Resolution);
        var diagnostic = new Color32[count];
        for (int p = 0; p < count; p++) diagnostic[p] = conflicts.Contains(p) ? new Color32(255, 0, 255, 255) : owner[p] >= 0 ? new Color32(255, 255, 255, 255) : owner[p] <= -2 ? new Color32(90, 90, 90, 255) : new Color32(0, 0, 0, 255);
        SaveImage(Folder + "/Kohaku_Transfer_Coverage.png", diagnostic, Resolution);
        AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        importer.textureType = TextureImporterType.Default; importer.sRGBTexture = true;
        importer.maxTextureSize = Resolution; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false; importer.alphaSource = TextureImporterAlphaSource.None;
        importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();
        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.name = "Kohaku Original Meshy topology transfer TEST";
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
        material.SetColor("_BaseColor", Color.white); material.SetFloat("_Metallic", 0); material.SetFloat("_Smoothness", 0);
        AssetDatabase.CreateAsset(material, materialPath);
        bool unchanged = protectedPaths.All(p => Hash(p) == hashes[p]);
        string report = "Kohaku topology-guided UV transfer proof of concept\n" +
            "Source model: " + KohakuPath + "\nSource base-color: " + SourcePath + " (decoded original PNG " + sourceWidth + "x" + sourceHeight + ")\n" +
            "Destination mesh: " + RigPath + "\nTaisho bridge: " + TaishoPath + "\n" +
            "Texture: " + texturePath + "\nMaterial: " + materialPath + "\nResolution: " + Resolution + "x" + Resolution + "\n" +
            "Rigged/Taisho triangle indices AND UV arrays identical: true\n" +
            "One-to-one paired triangles and ordered corners: " + paired.Length + "/" + paired.Length + "\n" +
            "Kohaku vertex registration RMS: " + Math.Sqrt(residualSum / kv.Length).ToString("G9") + "; max: " + residualMax.ToString("G9") + " (Taisho model units)\n" +
            "Degenerate target UV triangles: " + degenerate + "\nTriangles with no pixel centers at this resolution: " + noPixelCenters + " (subpixel footprints, NOT missing correspondence)\n" +
            "UV summed area: " + area.ToString("G9") + "; covered interior texels: " + covered + "; outside-only padding: " + padded + "\n" +
            "Overlapping sample events: " + overlapEvents + "; incompatible events (>2/255): " + conflictEvents + "; distinct conflicting texels: " + conflicts.Count + "\n" +
            "Overlap maximum RGB-channel disagreement: " + colorDiffMax + "; average: " + (overlapEvents == 0 ? 0 : colorDiffSum / overlapEvents) + "\n" +
            "Coverage legend: white=sampled triangle interior; gray=outside-only bleed; black=unused UV space; magenta=incompatible overlapping samples.\n" +
            "Method: bounds-normalize Kohaku geometry to Taisho; spatially match original positions, weld geometry-only duplicates for triangle lookup, verify unique face bijection, recover original UV per corner, rasterize rigged UV triangles and barycentrically sample the original Meshy base-color. No nearest-surface projection, repainting or AI generation.\n" +
            "Finite-resolution bilinear resampling cannot be mathematically lossless. Original triangle-boundary UV discontinuities are retained, not smoothed. Tiny triangles can be subpixel; original markings are not redesigned.\n" +
            "Test material uses only transferred base-color; source normal/roughness/metallic maps were deliberately NOT assigned because their UVs have not been transferred.\n" +
            "Protected source asset SHA256 unchanged: " + unchanged + "\nNo mesh, UV, rig, skin weight, animation, existing material, prefab or scene modified. No material reassigned.\n" +
            "Status: numerical correspondence complete; inspect overlap statistics and side/top/front/back preview before visual acceptance.\n";
        File.WriteAllText(Folder + "/Kohaku_Transfer_Report.txt", report);
        File.WriteAllText(Folder + "/Source_SHA256.txt", string.Join("\n", hashes.Select(p => p.Value + "  " + p.Key)));
        AssetDatabase.Refresh(); AssetDatabase.SaveAssets();
        if (!unchanged) throw new InvalidOperationException("Source hashes changed unexpectedly.");
        return report;
    }
}
