using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEditor;

public static class KohakuPbrTransferPoC
{
    private const string RigPath = "Assets/Models/Koi/Meshes/KoiFish_RiggedMesh.asset";
    private const string BridgePath = "Assets/Models/Koi/Materials/taisho sanke koi texture/Meshy_AI_Koi_Fish_20261021_image-to-3d-texture.fbx";
    private const string SourceFolder = "Assets/Models/Koi/Materials/kohaku koi texture/";
    private const string SourceMeshPath = SourceFolder + "Meshy_AI_kohaku_koi_textured_1025023033_texture.fbx";
    private const string ApprovedFolder = "Assets/Models/Koi/ProofOfConcept/KohakuTopologyTransfer/";
    public const string Folder = "Assets/Models/Koi/ProofOfConcept/KohakuPbrTopologyTransfer";
    private const int Resolution = 4096;
    private const float Cell = 1e-7f;

    private sealed class Image : IDisposable
    {
        public readonly int Width, Height;
        public readonly Color32[] Pixels;
        public Image(string path)
        {
            var image = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                if (!ImageConversion.LoadImage(image, File.ReadAllBytes(path), false)) throw new InvalidOperationException("PNG decode failed: " + path);
                Width = image.width; Height = image.height; Pixels = image.GetPixels32();
            }
            finally { UnityEngine.Object.DestroyImmediate(image); }
        }
        public Color Sample(Vector2 uv)
        {
            float x = Mathf.Clamp(uv.x * Width - 0.5f, 0, Width - 1), y = Mathf.Clamp(uv.y * Height - 0.5f, 0, Height - 1);
            int x0 = (int)x, y0 = (int)y, x1 = Math.Min(x0 + 1, Width - 1), y1 = Math.Min(y0 + 1, Height - 1);
            return Color.LerpUnclamped(Color.LerpUnclamped(Pixels[y0 * Width + x0], Pixels[y0 * Width + x1], x - x0),
                Color.LerpUnclamped(Pixels[y1 * Width + x0], Pixels[y1 * Width + x1], x - x0), y - y0);
        }
        public void Dispose() { }
    }
    private static string Hash(string path)
    {
        using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-", "");
    }
    private static Vector3Int Key(Vector3 p) => new Vector3Int(Mathf.RoundToInt(p.x / Cell), Mathf.RoundToInt(p.y / Cell), Mathf.RoundToInt(p.z / Cell));
    private static (int, int, int) TriKey(int a, int b, int c)
    {
        if (a > b) { int q = a; a = b; b = q; }
        if (b > c) { int q = b; b = c; c = q; }
        if (a > b) { int q = a; a = b; b = q; }
        return (a, b, c);
    }
    private static int[] Pair(UnityEngine.Mesh rig, UnityEngine.Mesh bridge, UnityEngine.Mesh source, out double rms, out float max)
    {
        var rv = rig.vertices; var tv = bridge.vertices; var kv = source.vertices;
        var ri = rig.triangles; var ti = bridge.triangles; var ki = source.triangles;
        if (!ri.SequenceEqual(ti) || !rig.uv.SequenceEqual(bridge.uv) || ri.Length != ki.Length || rv.Length != tv.Length)
            throw new InvalidOperationException("Verified Taisho/rigged correspondence no longer holds.");
        var buckets = new Dictionary<Vector3Int, List<int>>(); var roots = new int[tv.Length];
        for (int i = 0; i < tv.Length; i++)
        {
            var key = Key(tv[i]);
            if (!buckets.TryGetValue(key, out var list)) buckets[key] = list = new List<int>();
            roots[i] = i;
            foreach (int j in list) if ((tv[j] - tv[i]).sqrMagnitude < 1e-20f) { roots[i] = roots[j]; break; }
            list.Add(i);
        }
        var scale = new Vector3(bridge.bounds.size.x / source.bounds.size.x, bridge.bounds.size.y / source.bounds.size.y, bridge.bounds.size.z / source.bounds.size.z);
        var sourceRoots = new int[kv.Length]; double sum = 0; max = 0;
        for (int i = 0; i < kv.Length; i++)
        {
            var p = Vector3.Scale(kv[i] - source.bounds.center, scale) + bridge.bounds.center;
            var key = Key(p); float best = float.MaxValue; int found = -1;
            for (int x = -1; x <= 1; x++) for (int y = -1; y <= 1; y++) for (int z = -1; z <= 1; z++)
                if (buckets.TryGetValue(key + new Vector3Int(x, y, z), out var list))
                    foreach (int j in list) { float d = (tv[j] - p).sqrMagnitude; if (d < best) { best = d; found = roots[j]; } }
            if (found < 0 || best > 1e-14f) throw new InvalidOperationException("Unmatched source vertex: " + i);
            sourceRoots[i] = found; sum += best; max = Mathf.Max(max, Mathf.Sqrt(best));
        }
        rms = Math.Sqrt(sum / kv.Length);
        var triangles = new Dictionary<(int, int, int), int>();
        for (int i = 0; i < ti.Length; i += 3) triangles.Add(TriKey(roots[ti[i]], roots[ti[i + 1]], roots[ti[i + 2]]), i / 3);
        var corners = new int[ri.Length]; var paired = new bool[ri.Length / 3];
        for (int i = 0; i < ki.Length; i += 3)
        {
            if (!triangles.TryGetValue(TriKey(sourceRoots[ki[i]], sourceRoots[ki[i + 1]], sourceRoots[ki[i + 2]]), out int tri) || paired[tri])
                throw new InvalidOperationException("Missing or ambiguous source triangle: " + i / 3);
            paired[tri] = true;
            for (int c = 0; c < 3; c++)
            {
                int root = roots[ti[tri * 3 + c]], corner = -1;
                for (int j = 0; j < 3; j++) if (sourceRoots[ki[i + j]] == root) corner = ki[i + j];
                if (corner < 0) throw new InvalidOperationException("Triangle corner mismatch.");
                corners[tri * 3 + c] = corner;
            }
        }
        if (paired.Any(p => !p)) throw new InvalidOperationException("Unpaired triangle.");
        return corners;
    }
    private static Vector3 V(Vector4 v) => new Vector3(v.x, v.y, v.z);
    private static Vector3 Interpolate(Vector3[] values, int a, int b, int c, Vector3 w) => values[a] * w.x + values[b] * w.y + values[c] * w.z;
    // Exact inverse of the interpolated URP Lit TBN, not orthonormal dot products.
    private static Vector3 ToTangent(Vector3 direction, Vector3 t, Vector3 b, Vector3 n)
    {
        float det = Vector3.Dot(t, Vector3.Cross(b, n));
        if (Mathf.Abs(det) < 1e-8f) throw new InvalidOperationException("Singular destination tangent frame.");
        return new Vector3(Vector3.Dot(direction, Vector3.Cross(b, n)), Vector3.Dot(direction, Vector3.Cross(n, t)), Vector3.Dot(direction, Vector3.Cross(t, b))) / det;
    }
    // Differential tangent transport across corresponding triangle surfaces. Source normal
    // perturbations remain relative to the destination smooth normal, even where the rig's
    // geometry has been rotated/deformed. No global bounds transform is used for normals.
    private static Vector3 Transport(Vector3 tangent, Vector3 e1, Vector3 e2, Vector3 d1, Vector3 d2)
    {
        double aa = Vector3.Dot(e1, e1), ab = Vector3.Dot(e1, e2), bb = Vector3.Dot(e2, e2);
        double det = aa * bb - ab * ab;
        if (det <= aa * bb * 1e-10) throw new InvalidOperationException("Degenerate source geometric triangle.");
        double at = Vector3.Dot(e1, tangent), bt = Vector3.Dot(e2, tangent);
        return d1 * (float)((at * bb - bt * ab) / det) + d2 * (float)((bt * aa - at * ab) / det);
    }
    private static byte Byte(float value) => (byte)Mathf.Clamp(Mathf.RoundToInt(value * 255), 0, 255);
    private static Color32 Encode(Vector3 n) => new Color32(Byte(n.x * 0.5f + 0.5f), Byte(n.y * 0.5f + 0.5f), Byte(n.z * 0.5f + 0.5f), 255);
    private static Vector3 Decode(Color32 n)
    {
        float x = n.r / 255f * 2 - 1, y = n.g / 255f * 2 - 1;
        return new Vector3(x, y, Mathf.Sqrt(Mathf.Max(0, 1 - x * x - y * y))).normalized;
    }
    private static void Save(string path, Color32[] pixels)
    {
        if (File.Exists(path)) throw new IOException("Refusing to overwrite " + path);
        var image = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false, true);
        try { image.SetPixels32(pixels); image.Apply(false); File.WriteAllBytes(path, image.EncodeToPNG()); }
        finally { UnityEngine.Object.DestroyImmediate(image); }
    }
    private static void Import(string path, bool normal, bool alpha)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = false; importer.convertToNormalmap = false; importer.flipGreenChannel = false;
        importer.maxTextureSize = Resolution; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false; importer.alphaSource = alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
        importer.alphaIsTransparency = false; importer.wrapMode = TextureWrapMode.Clamp; importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();
    }
    public static string Bake()
    {
        if (Directory.Exists(Folder)) throw new InvalidOperationException("Output folder already exists; refusing overwrite.");
        // Protect existing koi assets, all scenes/prefabs/animations, plus their importer metadata.
        var files = Directory.GetFiles("Assets/Models/Koi", "*", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles("Assets", "*.unity", SearchOption.AllDirectories))
            .Concat(Directory.GetFiles("Assets", "*.prefab", SearchOption.AllDirectories))
            .Concat(Directory.GetFiles("Assets", "*.anim", SearchOption.AllDirectories))
            .Distinct().ToArray();
        var hashes = files.Concat(files.Select(p => p + ".meta").Where(File.Exists)).Distinct().ToDictionary(p => p, Hash);
        var rig = AssetDatabase.LoadAssetAtPath<UnityEngine.Mesh>(RigPath);
        var bridge = AssetDatabase.LoadAllAssetsAtPath(BridgePath).OfType<UnityEngine.Mesh>().First();
        var source = AssetDatabase.LoadAllAssetsAtPath(SourceMeshPath).OfType<UnityEngine.Mesh>().First();
        var corners = Pair(rig, bridge, source, out double rms, out float maxResidual);
        var ri = rig.triangles; var uv = rig.uv; var suv = source.uv;
        var rv = rig.vertices; var sv = source.vertices; var rn = rig.normals; var sn = source.normals;
        var rt = rig.tangents; var st = source.tangents;
        if (rn.Length != rv.Length || rt.Length != rv.Length || sn.Length != sv.Length || st.Length != sv.Length) throw new InvalidOperationException("Missing mesh tangent data.");
        // Normalize at vertices exactly as the URP vertex stage does for identity transform.
        rn = rn.Select(n => n.normalized).ToArray(); sn = sn.Select(n => n.normalized).ToArray();
        var rtv = rt.Select(t => V(t).normalized).ToArray(); var stv = st.Select(t => V(t).normalized).ToArray();
        string normalSource = SourceFolder + "kohaku koi_texture_normal.png";
        string roughSource = SourceFolder + "kohaku koi_texture_roughness.png";
        string metalSource = SourceFolder + "kohaku koi_texture_metallic.png";
        bool flipGreen = ((TextureImporter)AssetImporter.GetAtPath(normalSource)).flipGreenChannel;
        using (var normals = new Image(normalSource)) using (var roughness = new Image(roughSource)) using (var metallic = new Image(metalSource))
        {
            if (roughness.Pixels.Any(p => p.r != p.g || p.r != p.b) || metallic.Pixels.Any(p => p.r != p.g || p.r != p.b)) throw new InvalidOperationException("Source scalar map is not grayscale.");
            int count = Resolution * Resolution;
            var normalOut = new Color32[count]; var roughOut = new Color32[count]; var metalOut = new Color32[count]; var packed = new Color32[count]; var owner = new int[count];
            var normalErrors = new Color32[count]; var exceptions = new StringBuilder("triangle,x,y,tangentX,tangentY,tangentZ,urpDecodeErrorDegrees\n");
            int errorOver1 = 0, errorOver5 = 0;
            for (int p = 0; p < count; p++) { owner[p] = -1; normalOut[p] = new Color32(128, 128, 255, 255); roughOut[p] = new Color32(255, 255, 255, 255); metalOut[p] = new Color32(0, 0, 0, 255); packed[p] = new Color32(0, 0, 0, 0); normalErrors[p] = new Color32(0, 0, 0, 255); }
            int covered = 0, degenerate = 0, subpixel = 0, overlaps = 0, conflicts = 0, padded = 0, negativeZ = 0, mixedHandedness = 0;
            double errorSum = 0, errorMax = 0, flatErrorMax = 0; float minZ = 1;
            float roughMin = 1, roughMax = 0, metalMin = 1, metalMax = 0;
            for (int i = 0; i < ri.Length; i += 3)
            {
                int a = ri[i], b = ri[i + 1], c = ri[i + 2], sa = corners[i], sb = corners[i + 1], sc = corners[i + 2];
                if (rt[a].w != rt[b].w || rt[a].w != rt[c].w || st[sa].w != st[sb].w || st[sa].w != st[sc].w) mixedHandedness++;
                Vector2 pa = uv[a] * Resolution, pb = uv[b] * Resolution, pc = uv[c] * Resolution;
                double det = (double)(pb.x - pa.x) * (pc.y - pa.y) - (double)(pb.y - pa.y) * (pc.x - pa.x);
                if (Math.Abs(det) < 1e-12) { degenerate++; continue; }
                int minX = Math.Max(0, Mathf.CeilToInt(Mathf.Min(pa.x, pb.x, pc.x) - 0.5f)), maxX = Math.Min(Resolution - 1, Mathf.FloorToInt(Mathf.Max(pa.x, pb.x, pc.x) - 0.5f));
                int minY = Math.Max(0, Mathf.CeilToInt(Mathf.Min(pa.y, pb.y, pc.y) - 0.5f)), maxY = Math.Min(Resolution - 1, Mathf.FloorToInt(Mathf.Max(pa.y, pb.y, pc.y) - 0.5f));
                Vector3 e1 = sv[sb] - sv[sa], e2 = sv[sc] - sv[sa], d1 = rv[b] - rv[a], d2 = rv[c] - rv[a];
                int hits = 0;
                for (int y = minY; y <= maxY; y++) for (int x = minX; x <= maxX; x++)
                {
                    double px = x + 0.5 - pa.x, py = y + 0.5 - pa.y;
                    double wb = (px * (pc.y - pa.y) - py * (pc.x - pa.x)) / det, wc = ((pb.x - pa.x) * py - (pb.y - pa.y) * px) / det, wa = 1 - wb - wc;
                    if (wa < -1e-9 || wb < -1e-9 || wc < -1e-9) continue;
                    hits++; var w = new Vector3((float)wa, (float)wb, (float)wc);
                    Vector2 sampleUV = suv[sa] * w.x + suv[sb] * w.y + suv[sc] * w.z;
                    var color = normals.Sample(sampleUV); var ns = new Vector3(color.r * 2 - 1, (color.g * 2 - 1) * (flipGreen ? -1 : 1), color.b * 2 - 1).normalized;
                    Vector3 sourceN = Interpolate(sn, sa, sb, sc, w), sourceT = Interpolate(stv, sa, sb, sc, w);
                    float sourceSign = st[sa].w * w.x + st[sb].w * w.y + st[sc].w * w.z;
                    Vector3 sourceB = Vector3.Cross(sourceN, sourceT) * sourceSign;
                    Vector3 sourceDirection = (sourceT * ns.x + sourceB * ns.y + sourceN * ns.z).normalized;
                    // Resolve actual shader perturbation in the source orthonormal smooth frame.
                    Vector3 sourceUnitN = sourceN.normalized;
                    Vector3 sourceUnitT = Vector3.ProjectOnPlane(sourceT, sourceUnitN).normalized;
                    Vector3 sourceUnitB = Vector3.Cross(sourceUnitN, sourceUnitT) * Mathf.Sign(sourceSign);
                    Vector3 perturbation = new Vector3(Vector3.Dot(sourceDirection, sourceUnitT), Vector3.Dot(sourceDirection, sourceUnitB), Vector3.Dot(sourceDirection, sourceUnitN));
                    Vector3 destN = Interpolate(rn, a, b, c, w), destT = Interpolate(rtv, a, b, c, w);
                    float destSign = rt[a].w * w.x + rt[b].w * w.y + rt[c].w * w.z;
                    Vector3 destB = Vector3.Cross(destN, destT) * destSign, destUnitN = destN.normalized;
                    Vector3 transportedT = Vector3.ProjectOnPlane(Transport(sourceUnitT, e1, e2, d1, d2), destUnitN).normalized;
                    Vector3 transportedB = Vector3.ProjectOnPlane(Transport(sourceUnitB, e1, e2, d1, d2), destUnitN);
                    float hand = Mathf.Sign(Vector3.Dot(Vector3.Cross(destUnitN, transportedT), transportedB));
                    if (transportedT.sqrMagnitude < 0.5f || Mathf.Abs(destSign) < 1e-8f || Mathf.Abs(sourceSign) < 1e-8f || hand == 0) throw new InvalidOperationException("Singular local frame at triangle " + i / 3);
                    Vector3 transportedUnitB = Vector3.Cross(destUnitN, transportedT) * hand;
                    Vector3 desired = (transportedT * perturbation.x + transportedUnitB * perturbation.y + destUnitN * perturbation.z).normalized;
                    Vector3 converted = ToTangent(desired, destT, destB, destN).normalized;
                    minZ = Mathf.Min(minZ, converted.z); if (converted.z < 0) negativeZ++;
                    Color32 encoded = Encode(converted); Vector3 decoded = Decode(encoded);
                    Vector3 reconstructed = (destT * decoded.x + destB * decoded.y + destN * decoded.z).normalized;
                    double angle = Math.Atan2(Vector3.Cross(reconstructed, desired).magnitude, Vector3.Dot(reconstructed, desired)) * Mathf.Rad2Deg;
                    errorSum += angle; errorMax = Math.Max(errorMax, angle);
                    Vector3 flat = ToTangent(destUnitN, destT, destB, destN).normalized;
                    Vector3 flatReconstructed = (destT * flat.x + destB * flat.y + destN * flat.z).normalized;
                    flatErrorMax = Math.Max(flatErrorMax, Math.Atan2(Vector3.Cross(flatReconstructed, destUnitN).magnitude, Vector3.Dot(flatReconstructed, destUnitN)) * Mathf.Rad2Deg);
                    float r = roughness.Sample(sampleUV).r, m = metallic.Sample(sampleUV).r;
                    roughMin = Mathf.Min(roughMin, r); roughMax = Mathf.Max(roughMax, r); metalMin = Mathf.Min(metalMin, m); metalMax = Mathf.Max(metalMax, m);
                    byte rb = Byte(r), mb = Byte(m); Color32 pack = new Color32(mb, 0, 0, (byte)(255 - rb)); int p = y * Resolution + x;
                    if (owner[p] >= 0)
                    {
                        overlaps++; var old = normalOut[p];
                        if (Math.Abs(old.r - encoded.r) > 2 || Math.Abs(old.g - encoded.g) > 2 || Math.Abs(old.b - encoded.b) > 2 || Math.Abs(packed[p].r - pack.r) > 2 || Math.Abs(packed[p].a - pack.a) > 2) conflicts++;
                        continue;
                    }
                    owner[p] = i / 3; normalOut[p] = encoded; roughOut[p] = new Color32(rb, rb, rb, 255); metalOut[p] = new Color32(mb, mb, mb, 255); packed[p] = pack; covered++;
                    if (angle > 1) errorOver1++; if (angle > 5) errorOver5++;
                    normalErrors[p] = converted.z < 0 ? new Color32(255, 0, 255, 255) : new Color32(Byte((float)angle / 5), 0, 0, 255);
                    if (converted.z < 0 || angle > 5) exceptions.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1},{2},{3:G9},{4:G9},{5:G9},{6:G9}", i / 3, x, y, converted.x, converted.y, converted.z, angle));
                }
                if (hits == 0) subpixel++;
            }
            var frontier = new List<int>();
            for (int y = 0; y < Resolution; y++) for (int x = 0; x < Resolution; x++)
            {
                int p = y * Resolution + x; if (owner[p] < 0) continue;
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
                        int nx = x + dx, ny = y + dy; if (nx < 0 || ny < 0 || nx >= Resolution || ny >= Resolution) continue;
                        int q = ny * Resolution + nx; if (owner[q] != -1) continue;
                        owner[q] = -2 - step; normalOut[q] = normalOut[p]; roughOut[q] = roughOut[p]; metalOut[q] = metalOut[p]; packed[q] = packed[p]; next.Add(q); padded++;
                    }
                }
                frontier = next;
            }
            int packingErrors = 0;
            for (int p = 0; p < count; p++) if (packed[p].r != metalOut[p].r || packed[p].g != 0 || packed[p].b != 0 || packed[p].a != 255 - roughOut[p].r) packingErrors++;
            if (packingErrors != 0 || conflicts != 0) throw new InvalidOperationException("Data validation failed: packing=" + packingErrors + ", overlaps=" + conflicts);
            Directory.CreateDirectory(Folder);
            string prefix = Folder + "/Kohaku_OriginalMeshy_ToRigged_";
            Save(prefix + "Normal.png", normalOut); Save(prefix + "Roughness.png", roughOut); Save(prefix + "Metallic.png", metalOut); Save(prefix + "MetallicSmoothness.png", packed);
            Save(prefix + "NormalDecodeError.png", normalErrors);
            File.WriteAllText(Folder + "/Normal_Exceptions.csv", exceptions.ToString());
            Import(prefix + "Normal.png", true, false); Import(prefix + "Roughness.png", false, false); Import(prefix + "Metallic.png", false, false); Import(prefix + "MetallicSmoothness.png", false, true);
            Import(prefix + "NormalDecodeError.png", false, false);
            var approved = AssetDatabase.LoadAssetAtPath<Material>(ApprovedFolder + "Kohaku_OriginalMeshy_ToRigged_Test.mat");
            var material = new Material(approved);
            material.name = "Kohaku Original Meshy PBR transfer TEST";
            material.SetFloat("_WorkflowMode", 1); material.SetFloat("_Metallic", 1); material.SetFloat("_Smoothness", 1); material.SetFloat("_SmoothnessTextureChannel", 0); material.SetFloat("_BumpScale", 1);
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(prefix + "Normal.png")); material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(prefix + "MetallicSmoothness.png"));
            material.EnableKeyword("_NORMALMAP"); material.EnableKeyword("_METALLICSPECGLOSSMAP"); material.DisableKeyword("_SPECULAR_SETUP"); material.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            AssetDatabase.CreateAsset(material, prefix + "PBR_Test.mat"); AssetDatabase.SaveAssetIfDirty(material);
            bool unchanged = hashes.All(p => File.Exists(p.Key) && Hash(p.Key) == p.Value);
            var report = new StringBuilder();
            report.AppendLine("Kohaku original Meshy PBR topology transfer proof of concept");
            report.AppendLine("Source mesh: " + SourceMeshPath); report.AppendLine("Destination mesh: " + RigPath); report.AppendLine("Bridge: " + BridgePath);
            report.AppendLine("Original normal: " + normalSource + " (raw " + normals.Width + "x" + normals.Height + ")");
            report.AppendLine("Original roughness: " + roughSource + " (raw " + roughness.Width + "x" + roughness.Height + ")");
            report.AppendLine("Original metallic: " + metalSource + " (raw " + metallic.Width + "x" + metallic.Height + ")");
            report.AppendLine("Unity AI-generated MetallicSmoothness source NOT used. Raw linear PNG bytes; existing importer settings ignored and untouched.");
            report.AppendLine("Normal convention: RGB tangent XYZ, source importer flipGreen=" + flipGreen + "; output +Y, no importer flip. Meshy authoring convention not independently proven; consistent with existing normal importer.");
            report.AppendLine("Same verified bounds/spatial welded triangle bijection and ordered source corner recovery as approved base-color baker.");
            report.AppendLine("Paired triangles: " + ri.Length / 3 + "; registration RMS=" + rms.ToString("G9") + "; max=" + maxResidual.ToString("G9"));
            report.AppendLine("Normals: sample original RGB linearly; source interpolated shader TBN -> smooth-frame perturbation; transport source tangent direction by paired triangle differential to destination geometry; re-anchor to destination smooth normal, preserve handedness, invert actual destination interpolated URP TBN; normalize/encode RGB. No global-space bounds transform, no copied source-UV normals.");
            report.AppendLine("Resolution: " + Resolution + "x" + Resolution + "; covered=" + covered + "; outside-only 8px padding=" + padded + "; degenerateUV=" + degenerate + "; subpixel triangles=" + subpixel);
            report.AppendLine("Overlapping events=" + overlaps + "; conflicting events (>2/255)=" + conflicts + "; mixed tangent handedness triangles=" + mixedHandedness);
            report.AppendLine("Normal encode/reconstruct angular error (degrees): mean=" + (errorSum / (covered + overlaps)).ToString("G9") + "; max=" + errorMax.ToString("G9") + "; flat-normal transport max=" + flatErrorMax.ToString("G9") + "; negative destination z=" + negativeZ + "; minimum z=" + minZ.ToString("G9"));
            report.AppendLine("Sampled linear roughness range=" + roughMin.ToString("G9") + ".." + roughMax.ToString("G9") + "; metallic=" + metalMin.ToString("G9") + ".." + metalMax.ToString("G9"));
            report.AppendLine("Packing: R=metallic; G=B=0 (unused by Lit); A=smoothness=1-roughness. Exhaustive pixel packing mismatches=" + packingErrors);
            report.AppendLine("All new maps linear, 4096, uncompressed, bilinear clamp, no mipmaps; normal imported NormalMap, no height conversion; packed alpha retained, no transparency dilation.");
            report.AppendLine("Test material cloned approved base-color test; same BaseMap/BaseColor/ST; URP Lit metallic workflow, _NORMALMAP/_METALLICSPECGLOSSMAP enabled; _Smoothness=1; _BumpScale=1; smoothness source metallic alpha, not base alpha.");
            report.AppendLine("Outputs: " + prefix + "Normal.png\n" + prefix + "Roughness.png\n" + prefix + "Metallic.png\n" + prefix + "MetallicSmoothness.png\n" + prefix + "PBR_Test.mat");
            report.AppendLine("Protected existing file SHA256 unchanged=" + unchanged + "; protected files=" + hashes.Count + ". No mesh/UV/rig/animation/existing material/prefab/scene modification or reassignment.");
            report.AppendLine("Normal URP XY/positive-Z decode errors: >1 degree=" + errorOver1 + "; >5 degrees=" + errorOver5 + ". NormalDecodeError.png: red=error scaled to 5 degrees; magenta=negative tangent Z; black=unused/near-zero error. CSV lists >5-degree and negative-Z interior samples.");
            report.AppendLine("NORMAL FIDELITY STATUS: " + (negativeZ == 0 && errorOver5 == 0 ? "passes measured texel-center 5-degree bound" : "LIMITED / strict fidelity FAILED: existing mixed-handedness tangent interpolation produces ill-conditioned frames; conventional URP normal maps reconstruct positive Z and cannot represent all converted normals. Outputs are diagnostic test assets, NOT production-approved. No tangents/mesh modified and no normal strength reduction or repainting used."));
            report.AppendLine("Limitations: 8-bit quantization and finite resolution/bilinear resampling; subpixel triangles; padding/mipmaps require production tuning; local tangent transport preserves angular detail and anchors neutral normals to deformed destination surface, not physical inverse-transpose stretch of a global normal field. Source +Y convention inferred from current importer. Numerical validation is not visual approval; lighting/skin-deformation appearance remains unapproved.");
            File.WriteAllText(Folder + "/Kohaku_PBR_Transfer_Report.txt", report.ToString());
            File.WriteAllText(Folder + "/Protected_SHA256.txt", string.Join("\n", hashes.Select(p => p.Value + "  " + p.Key)));
            AssetDatabase.Refresh();
            if (!unchanged) throw new InvalidOperationException("Protected asset changed unexpectedly.");
            return report.ToString();
        }
    }
}
