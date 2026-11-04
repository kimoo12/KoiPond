using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEditor;

public static class PurachinaTopologyTransferPoC
{
    private const string RigPath = "Assets/Models/Koi/Meshes/KoiFish_RiggedMesh.asset";
    private const string BridgePath = "Assets/Models/Koi/Materials/taisho sanke koi texture/Meshy_AI_Koi_Fish_20261021_image-to-3d-texture.fbx";
    private const string ModelPath = "Assets/Models/Koi/Materials/purachina koi texture/Meshy_AI_purachina_koi_texture_1025015213_texture.fbx";
    private const string SourcePath = "Assets/Models/Koi/Materials/purachina koi texture/purachina koi_texture.png";
    public const string Folder = "Assets/Models/Koi/ProofOfConcept/PurachinaTopologyTransfer";
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
    public static string Hash(string path)
    {
        using (var h = SHA256.Create()) return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(path))).Replace("-", "");
    }
    private static Color32 Sample(Color32[] pixels, int width, int height, Vector2 uv)
    {
        // Same original-sRGB-byte bilinear sampling as the Kohaku proof of concept.
        float x = Mathf.Clamp(uv.x * width - 0.5f, 0, width - 1), y = Mathf.Clamp(uv.y * height - 0.5f, 0, height - 1);
        int x0 = (int)x, y0 = (int)y, x1 = Math.Min(x0 + 1, width - 1), y1 = Math.Min(y0 + 1, height - 1);
        float fx = x - x0, fy = y - y0;
        Color a = Color.LerpUnclamped(pixels[y0 * width + x0], pixels[y0 * width + x1], fx);
        Color b = Color.LerpUnclamped(pixels[y1 * width + x0], pixels[y1 * width + x1], fx);
        return Color.LerpUnclamped(a, b, fy);
    }
    private static void SaveImage(string path, Color32[] pixels, int width, int height)
    {
        var image = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
        try { image.SetPixels32(pixels); image.Apply(false); File.WriteAllBytes(path, image.EncodeToPNG()); }
        finally { UnityEngine.Object.DestroyImmediate(image); }
    }
    private static void CheckUV(Vector2[] uv, int vertices, string label)
    {
        if (uv.Length != vertices) throw new InvalidOperationException(label + " UV count mismatch.");
        for (int i = 0; i < uv.Length; i++)
            if (float.IsNaN(uv[i].x) || float.IsNaN(uv[i].y) || float.IsInfinity(uv[i].x) || float.IsInfinity(uv[i].y) || uv[i].x < 0 || uv[i].x > 1 || uv[i].y < 0 || uv[i].y > 1)
                throw new InvalidOperationException(label + " invalid/out-of-range UV at " + i);
    }
    public static string Bake()
    {
        if (Directory.Exists(Folder)) throw new InvalidOperationException("Output folder already exists; refusing any overwrite.");
        // Protect every pre-existing koi asset and metadata, prefabs, animations and all scenes.
        var protectedPaths = Directory.GetFiles("Assets/Models/Koi", "*", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles("Assets/Scenes", "*", SearchOption.AllDirectories))
            .Concat(Directory.Exists("Assets/Prefabs") ? Directory.GetFiles("Assets/Prefabs", "*", SearchOption.AllDirectories) : Array.Empty<string>())
            .Concat(Directory.Exists("Assets/Animations") ? Directory.GetFiles("Assets/Animations", "*", SearchOption.AllDirectories) : Array.Empty<string>())
            .Distinct().ToArray();
        var hashes = protectedPaths.ToDictionary(p => p, Hash);
        var rig = AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(RigPath);
        var bridges = AssetDatabase.LoadAllAssetsAtPath(BridgePath).OfType<UnityEngine.Mesh>().ToArray();
        var sources = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<UnityEngine.Mesh>().ToArray();
        if (rig == null || bridges.Length != 1 || sources.Length != 1) throw new InvalidOperationException("Expected one source mesh and one bridge mesh.");
        var bridge = bridges[0]; var original = sources[0];
        var rv = rig.vertices; var tv = bridge.vertices; var sv = original.vertices;
        var ri = rig.triangles; var ti = bridge.triangles; var si = original.triangles;
        var targetUV = rig.uv; var sourceUV = original.uv;
        if (rv.Length != tv.Length || !ri.SequenceEqual(ti) || !targetUV.SequenceEqual(bridge.uv) || ri.Length != si.Length)
            throw new InvalidOperationException("Verified Taisho/rigged index and UV correspondence no longer holds.");
        CheckUV(targetUV, rv.Length, "Destination"); CheckUV(sourceUV, sv.Length, "Source");
        var buckets = new Dictionary<Vector3Int, List<int>>(); var roots = new int[tv.Length];
        for (int i = 0; i < tv.Length; i++)
        {
            var key = Key(tv[i]);
            if (!buckets.TryGetValue(key, out var list)) buckets[key] = list = new List<int>();
            roots[i] = i;
            foreach (int j in list) if ((tv[j] - tv[i]).sqrMagnitude < 1e-20f) { roots[i] = roots[j]; break; }
            list.Add(i);
        }
        var scale = new Vector3(bridge.bounds.size.x / original.bounds.size.x, bridge.bounds.size.y / original.bounds.size.y, bridge.bounds.size.z / original.bounds.size.z);
        var sourceRoots = new int[sv.Length]; double residualSum = 0; float residualMax = 0;
        for (int i = 0; i < sv.Length; i++)
        {
            var p = Vector3.Scale(sv[i] - original.bounds.center, scale) + bridge.bounds.center;
            var key = Key(p); float best = float.MaxValue; int found = -1;
            for (int x = -1; x <= 1; x++) for (int y = -1; y <= 1; y++) for (int z = -1; z <= 1; z++)
                if (buckets.TryGetValue(key + new Vector3Int(x, y, z), out var list))
                    foreach (int j in list) { float d = (tv[j] - p).sqrMagnitude; if (d < best) { best = d; found = roots[j]; } }
            if (found < 0 || best > 1e-14f) throw new InvalidOperationException("Unmatched Purachina vertex " + i);
            sourceRoots[i] = found; residualSum += best; residualMax = Mathf.Max(residualMax, Mathf.Sqrt(best));
        }
        var triangles = new Dictionary<(int, int, int), int>();
        for (int i = 0; i < ti.Length; i += 3) triangles.Add(TriKey(roots[ti[i]], roots[ti[i + 1]], roots[ti[i + 2]]), i / 3);
        var cornerUV = new Vector2[ri.Length]; var paired = new bool[ri.Length / 3];
        int orderedCorners = 0, reversedWinding = 0;
        for (int i = 0; i < si.Length; i += 3)
        {
            if (!triangles.TryGetValue(TriKey(sourceRoots[si[i]], sourceRoots[si[i + 1]], sourceRoots[si[i + 2]]), out int tri) || paired[tri])
                throw new InvalidOperationException("Missing or ambiguous Purachina triangle " + i / 3);
            paired[tri] = true; var permutation = new int[3];
            for (int c = 0; c < 3; c++)
            {
                int root = roots[ti[tri * 3 + c]], sourceCorner = -1, matches = 0;
                for (int j = 0; j < 3; j++) if (sourceRoots[si[i + j]] == root) { sourceCorner = si[i + j]; permutation[c] = j; matches++; }
                if (matches != 1) throw new InvalidOperationException("Nonunique triangle corner correspondence.");
                cornerUV[tri * 3 + c] = sourceUV[sourceCorner]; orderedCorners++;
            }
            if ((permutation[1] - permutation[0] + 3) % 3 != 1) reversedWinding++;
        }
        if (paired.Any(v => !v)) throw new InvalidOperationException("Unpaired destination triangles.");
        if (reversedWinding != 0) throw new InvalidOperationException("Unexpected reversed source winding.");
        var source = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
        Color32[] sourcePixels; int sourceWidth, sourceHeight;
        try
        {
            if (!ImageConversion.LoadImage(source, File.ReadAllBytes(SourcePath), false)) throw new InvalidOperationException("Cannot decode original PNG.");
            sourceWidth = source.width; sourceHeight = source.height; sourcePixels = source.GetPixels32();
        }
        finally { UnityEngine.Object.DestroyImmediate(source); }
        int count = Resolution * Resolution; var colors = new Color32[count]; var owner = new int[count];
        for (int i = 0; i < count; i++) owner[i] = -1;
        int degenerate = 0, noPixelCenters = 0, overlapEvents = 0, conflictEvents = 0, covered = 0, mappingConflicts = 0;
        var conflicts = new HashSet<int>(); double area = 0, colorDiffSum = 0; int colorDiffMax = 0; float mappingDiffMax = 0;
        for (int i = 0; i < ri.Length; i += 3)
        {
            Vector2 a = targetUV[ri[i]] * Resolution, b = targetUV[ri[i + 1]] * Resolution, c = targetUV[ri[i + 2]] * Resolution;
            double det = (double)(b.x - a.x) * (c.y - a.y) - (double)(b.y - a.y) * (c.x - a.x);
            area += Math.Abs(det) / (2.0 * count);
            if (Math.Abs(det) < 1e-12) { degenerate++; continue; }
            int minX = Math.Max(0, Mathf.CeilToInt(Mathf.Min(a.x, b.x, c.x) - 0.5f)), maxX = Math.Min(Resolution - 1, Mathf.FloorToInt(Mathf.Max(a.x, b.x, c.x) - 0.5f));
            int minY = Math.Max(0, Mathf.CeilToInt(Mathf.Min(a.y, b.y, c.y) - 0.5f)), maxY = Math.Min(Resolution - 1, Mathf.FloorToInt(Mathf.Max(a.y, b.y, c.y) - 0.5f));
            int hits = 0;
            for (int y = minY; y <= maxY; y++) for (int x = minX; x <= maxX; x++)
            {
                double px = x + 0.5 - a.x, py = y + 0.5 - a.y;
                double wb = (px * (c.y - a.y) - py * (c.x - a.x)) / det, wc = ((b.x - a.x) * py - (b.y - a.y) * px) / det, wa = 1 - wb - wc;
                if (wa < -1e-9 || wb < -1e-9 || wc < -1e-9) continue;
                hits++; Vector2 uv = cornerUV[i] * (float)wa + cornerUV[i + 1] * (float)wb + cornerUV[i + 2] * (float)wc;
                Color32 color = Sample(sourcePixels, sourceWidth, sourceHeight, uv); int pixel = y * Resolution + x;
                if (owner[pixel] >= 0)
                {
                    overlapEvents++;
                    int diff = Math.Max(Math.Abs(colors[pixel].r - color.r), Math.Max(Math.Abs(colors[pixel].g - color.g), Math.Abs(colors[pixel].b - color.b)));
                    colorDiffMax = Math.Max(colorDiffMax, diff); colorDiffSum += diff;
                    if (diff > 2) { conflictEvents++; conflicts.Add(pixel); }
                    int oi = owner[pixel] * 3;
                    Vector2 oa = targetUV[ri[oi]] * Resolution, ob = targetUV[ri[oi + 1]] * Resolution, oc = targetUV[ri[oi + 2]] * Resolution;
                    double od = (double)(ob.x - oa.x) * (oc.y - oa.y) - (double)(ob.y - oa.y) * (oc.x - oa.x);
                    double opx = x + 0.5 - oa.x, opy = y + 0.5 - oa.y;
                    float owb = (float)((opx * (oc.y - oa.y) - opy * (oc.x - oa.x)) / od), owc = (float)(((ob.x - oa.x) * opy - (ob.y - oa.y) * opx) / od);
                    Vector2 oldUV = cornerUV[oi] * (1 - owb - owc) + cornerUV[oi + 1] * owb + cornerUV[oi + 2] * owc;
                    float mappingDiff = Vector2.Scale(oldUV - uv, new Vector2(sourceWidth, sourceHeight)).magnitude;
                    mappingDiffMax = Mathf.Max(mappingDiffMax, mappingDiff);
                    if (mappingDiff > 0.01f) { mappingConflicts++; conflicts.Add(pixel); }
                    continue; // Do not average incompatible source markings or mappings.
                }
                owner[pixel] = i / 3; colors[pixel] = color; covered++;
            }
            if (hits == 0) noPixelCenters++;
        }
        if (degenerate != 0 || conflictEvents != 0 || mappingConflicts != 0) throw new InvalidOperationException("Bake rejected: degenerate UVs or incompatible overlapping color/mapping samples.");
        // Same eight-texel outside-only edge bleed as Kohaku. Surface interiors remain untouched.
        int padded = 0; var frontier = new List<int>();
        for (int y = 0; y < Resolution; y++) for (int x = 0; x < Resolution; x++)
        {
            int p = y * Resolution + x;
            if (owner[p] >= 0 && ((x > 0 && owner[p - 1] < 0) || (x < Resolution - 1 && owner[p + 1] < 0) || (y > 0 && owner[p - Resolution] < 0) || (y < Resolution - 1 && owner[p + Resolution] < 0))) frontier.Add(p);
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
        if (!protectedPaths.All(p => Hash(p) == hashes[p]) || !rig.uv.SequenceEqual(targetUV) || !rig.triangles.SequenceEqual(ri)) throw new InvalidOperationException("Protected input changed.");
        Directory.CreateDirectory(Folder);
        string texturePath = Folder + "/Purachina_OriginalMeshy_ToRigged_BaseColor.png", materialPath = Folder + "/Purachina_OriginalMeshy_ToRigged_Test.mat";
        SaveImage(texturePath, colors, Resolution, Resolution);
        var diagnostic = new Color32[count];
        for (int p = 0; p < count; p++) diagnostic[p] = conflicts.Contains(p) ? new Color32(255, 0, 255, 255) : owner[p] >= 0 ? new Color32(255, 255, 255, 255) : owner[p] <= -2 ? new Color32(90, 90, 90, 255) : new Color32(0, 0, 0, 255);
        SaveImage(Folder + "/Purachina_Transfer_Coverage.png", diagnostic, Resolution, Resolution);
        AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        importer.textureType = TextureImporterType.Default; importer.sRGBTexture = true; importer.maxTextureSize = Resolution;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.mipmapEnabled = false; importer.alphaSource = TextureImporterAlphaSource.None;
        importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Bilinear; importer.SaveAndReimport();
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) throw new InvalidOperationException("URP Lit shader missing.");
        var material = new Material(shader); material.name = "Purachina Original Meshy topology transfer TEST";
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath)); material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Metallic", 0); material.SetFloat("_Smoothness", 0); AssetDatabase.CreateAsset(material, materialPath);
        var report = new StringBuilder();
        report.AppendLine("Purachina topology-guided UV-to-UV original base-color transfer");
        report.AppendLine("Source model: " + ModelPath + "\nSource base-color: " + SourcePath + " (original PNG " + sourceWidth + "x" + sourceHeight + ")\nDestination: " + RigPath + "\nTaisho bridge: " + BridgePath);
        report.AppendLine("Texture: " + texturePath + "\nMaterial: " + materialPath + "\nResolution: " + Resolution + "x" + Resolution);
        report.AppendLine("Rigged/Taisho vertex counts, triangle indices AND UV arrays identical: True");
        report.AppendLine("Unique triangle bijection: " + paired.Length + "/" + paired.Length + "; verified uniquely ordered corners: " + orderedCorners + "/" + ri.Length + "; reversed winding: " + reversedWinding);
        report.AppendLine("Vertex registration RMS: " + Math.Sqrt(residualSum / sv.Length).ToString("G9") + "; max: " + residualMax.ToString("G9") + " (Taisho units)");
        report.AppendLine("All source/destination UVs finite and within [0,1]: True\nDegenerate target UV triangles: " + degenerate + "\nTriangles with no pixel centers: " + noPixelCenters + " (subpixel footprints, NOT missing correspondence)");
        report.AppendLine("Summed target UV area: " + area.ToString("G9") + "; interior texels: " + covered + " (" + (100.0 * covered / count).ToString("F6") + "% atlas); outside-only padding texels: " + padded);
        report.AppendLine("Overlapping sample events: " + overlapEvents + "; incompatible RGB events (>2/255): " + conflictEvents + "; incompatible UV mapping events (>0.01 source texel): " + mappingConflicts + "; distinct conflict texels: " + conflicts.Count);
        report.AppendLine("Overlap RGB max/average disagreement: " + colorDiffMax + "/" + (overlapEvents == 0 ? 0 : colorDiffSum / overlapEvents) + "; max mapping disagreement: " + mappingDiffMax.ToString("G9") + " source texels");
        report.AppendLine("Coverage legend: white=sampled interior; gray=outside-only bleed; black=unused atlas; magenta=incompatible color or mapping overlap.");
        report.AppendLine("Method: same bounds normalization, 1e-7 spatial buckets, geometry-only duplicate welding, unique Taisho face lookup and original source corner UV recovery as Kohaku; rasterize unchanged rigged UVs and barycentrically bilinear-sample original sRGB PNG bytes. No projection, repainting, generation, simplification, tint or marking edits.");
        report.AppendLine("Finite-resolution resampling is not mathematically lossless. Source seam UV discontinuities are retained. Subpixel triangle footprints are explicitly reported. Eight texels of edge bleed are outside rasterized interiors only, as in Kohaku.");
        report.AppendLine("Test material: transferred base-color only; white tint; metallic=0; smoothness=0. Source-UV normal/metallic/roughness NOT assigned.");
        File.WriteAllText(Folder + "/Source_SHA256.txt", string.Join("\n", hashes.Select(p => p.Value + "  " + p.Key)));
        RenderComparison(original, rig, report);
        AssetDatabase.Refresh();
        bool unchanged = protectedPaths.All(p => Hash(p) == hashes[p]);
        report.AppendLine("Protected pre-existing asset and metadata SHA256 unchanged: " + unchanged + " (" + hashes.Count + " files)");
        report.AppendLine("Destination in-memory UV/indices/vertices unchanged: " + (rig.uv.SequenceEqual(targetUV) && rig.triangles.SequenceEqual(ri) && rig.vertices.SequenceEqual(rv)));
        report.AppendLine("No existing texture/material/mesh/rig/animation/prefab/scene assignment changed. No other variation processed. Comparison is offscreen in an isolated preview scene.");
        report.AppendLine("Status: correspondence, winding, UV validity, raster coverage and conflict gates PASS; six-view rendering completed. Visual acceptance requires inspection of the comparison.");
        File.WriteAllText(Folder + "/Purachina_Transfer_Report.txt", report.ToString());
        AssetDatabase.ImportAsset(Folder + "/Purachina_Transfer_Report.txt", ImportAssetOptions.ForceSynchronousImport);
        if (!unchanged) throw new InvalidOperationException("Protected assets changed.");
        return report.ToString();
    }
    public static void RenderComparison(UnityEngine.Mesh original, UnityEngine.Mesh rig, StringBuilder report)
    {
        const int width = 640, height = 320;
        var combined = new Color32[width * 2 * height * 6];
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) throw new InvalidOperationException("Unlit comparison shader missing.");
        var sourceTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
        ImageConversion.LoadImage(sourceTexture, File.ReadAllBytes(SourcePath)); sourceTexture.wrapMode = TextureWrapMode.Clamp; sourceTexture.filterMode = FilterMode.Bilinear;
        var sourceMaterial = new Material(shader); sourceMaterial.SetTexture("_BaseMap", sourceTexture); sourceMaterial.SetColor("_BaseColor", Color.white);
        var targetMaterial = new Material(shader); targetMaterial.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Purachina_OriginalMeshy_ToRigged_BaseColor.png")); targetMaterial.SetColor("_BaseColor", Color.white);
        var preview = new PreviewRenderUtility();
        var directions = new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        var names = new[] { "right side", "left side", "top", "bottom", "front", "rear" };
        try
        {
            report.AppendLine("Six-view comparison: " + Folder + "/Purachina_Source_vs_Transfer_6Views.png");
            report.AppendLine("Left=original Purachina Meshy; right=rigged transfer. Rows: right side, left side, top, bottom, front, rear. Identical neutral unlit base-color rendering; meshes independently bounds-normalized only for framing. Geometry differences are expected.");
            for (int row = 0; row < 6; row++) for (int column = 0; column < 2; column++)
            {
                var mesh = column == 0 ? original : rig; var mat = column == 0 ? sourceMaterial : targetMaterial;
                float s = 1 / Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.y, mesh.bounds.size.z);
                // Taisho/rigged vertex covariance verifies source +Y maps to rig -Z, source +Z to rig +Y.
                // Render-only rotation: no source or destination mesh data is modified.
                Quaternion rotation = column == 0 ? Quaternion.Euler(-90, 0, 0) : Quaternion.identity;
                Matrix4x4 matrix = Matrix4x4.TRS(rotation * (-mesh.bounds.center * s), rotation, Vector3.one * s);
                var camera = preview.camera;
                camera.orthographic = true; camera.orthographicSize = 0.575f; camera.nearClipPlane = 0.01f; camera.farClipPlane = 10;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.12f, 0.14f, 0.16f, 1);
                camera.transform.position = directions[row] * 2;
                camera.transform.rotation = Quaternion.LookRotation(-directions[row], row == 2 || row == 3 ? Vector3.forward : Vector3.up);
                preview.BeginPreview(new Rect(0, 0, width, height), GUIStyle.none);
                for (int sub = 0; sub < mesh.subMeshCount; sub++) preview.DrawMesh(mesh, matrix, mat, sub);
                preview.Render(true); var rendered = preview.EndPreview();
                var rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                var old = RenderTexture.active; var read = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
                try
                {
                    Graphics.Blit(rendered, rt); RenderTexture.active = rt; read.ReadPixels(new Rect(0, 0, width, height), 0, 0); read.Apply();
                    var pixels = read.GetPixels32(); int visible = 0; Color32 bg = pixels[0];
                    for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                    {
                        Color32 p = pixels[y * width + x];
                        if (Math.Abs(p.r - bg.r) + Math.Abs(p.g - bg.g) + Math.Abs(p.b - bg.b) > 8) visible++;
                        combined[((5 - row) * height + y) * width * 2 + column * width + x] = p;
                    }
                    if (visible < 100) throw new InvalidOperationException("Empty comparison view " + names[row]);
                    report.AppendLine(names[row] + (column == 0 ? " original" : " transfer") + " nonbackground pixels: " + visible);
                }
                finally { RenderTexture.active = old; RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.DestroyImmediate(read); }
            }
            SaveImage(Folder + "/Purachina_Source_vs_Transfer_6Views.png", combined, width * 2, height * 6);
        }
        finally
        {
            preview.Cleanup(); UnityEngine.Object.DestroyImmediate(sourceMaterial); UnityEngine.Object.DestroyImmediate(targetMaterial); UnityEngine.Object.DestroyImmediate(sourceTexture);
        }
    }
}
