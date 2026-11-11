using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace KoiPond.EarlyStageTransferPilot
{
    public static class EarlyStageAlbedoTransferBuilder
    {
        public const string Folder = "Assets/Models/Koi/ProofOfConcept/EarlyStageAlbedoTransferPilot";
        public const string Adult = "Assets/Prefabs/Koi_Stage3_Adult.prefab";
        public const string Fry = "Assets/Models/Koi/ProofOfConcept/FryRiggingPrototype/Prefabs/Fry_RiggingPrototype.prefab";
        public const string Juvenile = "Assets/Models/Koi/ProofOfConcept/JuvenileRiggingPrototype/Prefabs/Juvenile_RiggingPrototype.prefab";
        const int Size = 2048;
        const float MaxDistance = .075f;
        const float NormalDot = .25f;
        class Surface
        {
            public Vector3[] V, N, Bones;
            public Vector2[] UV;
            public int[] T, Regions;
            public Bounds Bounds;
            public Bounds[] RegionBounds;
            public float[,] Radius;
            public SkinnedMeshRenderer Skin;
            const int Rings = 64, Angles = 48;
            public Surface(string path)
            {
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Skin = root.GetComponentInChildren<SkinnedMeshRenderer>();
                var m = Skin.sharedMesh;
                var matrix = root.transform.worldToLocalMatrix * Skin.transform.localToWorldMatrix;
                V = m.vertices.Select(matrix.MultiplyPoint3x4).ToArray();
                N = m.normals.Select(n => matrix.inverse.transpose.MultiplyVector(n).normalized).ToArray();
                UV = m.uv; T = m.triangles;
                Bones = Skin.bones.Select(b => root.transform.InverseTransformPoint(b.position)).ToArray();
                Bounds = new Bounds(V[0], Vector3.zero); foreach (var v in V) Bounds.Encapsulate(v);
                var weights = m.boneWeights;
                Regions = new int[T.Length / 3];
                for (int t = 0; t < Regions.Length; t++)
                {
                    float l = 0, r = 0;
                    for (int j = 0; j < 3; j++)
                    {
                        var w = weights[T[t * 3 + j]];
                        l += Weight(w, 4); r += Weight(w, 5);
                    }
                    var p = (V[T[t * 3]] + V[T[t * 3 + 1]] + V[T[t * 3 + 2]]) / 3;
                    var normal = (N[T[t * 3]] + N[T[t * 3 + 1]] + N[T[t * 3 + 2]]).normalized;
                    Regions[t] = p.z < -.17f ? 3 :
                        Mathf.Abs(p.x) < .024f && Mathf.Abs(normal.x) > .50f && Mathf.Abs(p.y) > .055f && p.z < .17f ? (p.y > 0 ? 4 : 5) :
                        l > 1.05f ? 1 : r > 1.05f ? 2 : 0;
                }
                RegionBounds = new Bounds[6]; var initialized = new bool[6];
                Radius = new float[Rings, Angles];
                for (int f = 0; f < Regions.Length; f++) for (int k = 0; k < 3; k++)
                {
                    var p = V[T[f * 3 + k]]; int region = Regions[f];
                    if (!initialized[region]) { RegionBounds[region] = new Bounds(p, Vector3.zero); initialized[region] = true; }
                    else RegionBounds[region].Encapsulate(p);
                    if (region != 0) continue;
                    int z = Mathf.Clamp(Mathf.RoundToInt((p.z - Bounds.min.z) / Bounds.size.z * (Rings - 1)), 0, Rings - 1);
                    int angle = Mathf.Clamp(Mathf.RoundToInt((Mathf.Atan2(p.y, p.x) + Mathf.PI) / (2 * Mathf.PI) * (Angles - 1)), 0, Angles - 1);
                    Radius[z, angle] = Mathf.Max(Radius[z, angle], new Vector2(p.x, p.y).magnitude);
                }
                // Interpolate empty geometric chart cells, never appearance pixels or rejected surface samples.
                var measured = (float[,])Radius.Clone();
                for (int z = 0; z < Rings; z++) for (int angle = 0; angle < Angles; angle++)
                {
                    if (Radius[z, angle] > 0) continue;
                    float nearest = float.MaxValue, value = 0;
                    for (int zz = 0; zz < Rings; zz++) for (int aa = 0; aa < Angles; aa++)
                    {
                        if (measured[zz, aa] <= 0) continue;
                        int da = Mathf.Abs(angle - aa); da = Mathf.Min(da, Angles - da);
                        float distance = (zz - z) * (zz - z) + da * da;
                        if (distance < nearest) { nearest = distance; value = measured[zz, aa]; }
                    }
                    Radius[z, angle] = value;
                }
                for (int pass = 0; pass < 2; pass++)
                {
                    var old = (float[,])Radius.Clone();
                    for (int z = 0; z < Rings; z++) for (int a = 0; a < Angles; a++)
                        Radius[z, a] = (old[z, a] * 4 + old[Mathf.Max(0, z - 1), a] + old[Mathf.Min(Rings - 1, z + 1), a] + old[z, (a + 1) % Angles] + old[z, (a + Angles - 1) % Angles]) / 8;
                }
            }
            public float Radial(float z, float angle)
            {
                float zz = Mathf.Clamp((z - Bounds.min.z) / Bounds.size.z * (Rings - 1), 0, Rings - 1);
                float aa = Mathf.Clamp((angle + Mathf.PI) / (2 * Mathf.PI) * (Angles - 1), 0, Angles - 1);
                int z0 = Mathf.FloorToInt(zz), a0 = Mathf.FloorToInt(aa);
                return Mathf.Lerp(Mathf.Lerp(Radius[z0, a0], Radius[z0, Mathf.Min(a0 + 1, Angles - 1)], aa - a0), Mathf.Lerp(Radius[Mathf.Min(z0 + 1, Rings - 1), a0], Radius[Mathf.Min(z0 + 1, Rings - 1), Mathf.Min(a0 + 1, Angles - 1)], aa - a0), zz - z0);
            }
            static float Weight(BoneWeight w, int b) => (w.boneIndex0 == b ? w.weight0 : 0) + (w.boneIndex1 == b ? w.weight1 : 0) + (w.boneIndex2 == b ? w.weight2 : 0) + (w.boneIndex3 == b ? w.weight3 : 0);
        }
        class Node
        {
            public Bounds Bounds;
            public Node Left, Right;
            public int[] Faces;
        }
        static Node Build(Surface s, int[] faces)
        {
            if (faces.Length == 0) return null;
            var b = new Bounds(s.V[s.T[faces[0] * 3]], Vector3.zero);
            foreach (int f in faces) for (int k = 0; k < 3; k++) b.Encapsulate(s.V[s.T[f * 3 + k]]);
            var n = new Node { Bounds = b };
            if (faces.Length <= 12) { n.Faces = faces; return n; }
            int axis = b.size.x > b.size.y ? 0 : 1; if (b.size.z > b.size[axis]) axis = 2;
            Array.Sort(faces, (a, c) => Center(s, a)[axis].CompareTo(Center(s, c)[axis]));
            int half = faces.Length / 2;
            n.Left = Build(s, faces.Take(half).ToArray()); n.Right = Build(s, faces.Skip(half).ToArray()); return n;
        }
        static Vector3 Center(Surface s, int f) => (s.V[s.T[f * 3]] + s.V[s.T[f * 3 + 1]] + s.V[s.T[f * 3 + 2]]) / 3;
        // Closest point with exact triangle barycentrics (Ericson region tests).
        static Vector3 Bary(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            var ab = b - a; var ac = c - a; var ap = p - a;
            float d1 = Vector3.Dot(ab, ap), d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0 && d2 <= 0) return new Vector3(1, 0, 0);
            var bp = p - b; float d3 = Vector3.Dot(ab, bp), d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0 && d4 <= d3) return new Vector3(0, 1, 0);
            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0 && d1 >= 0 && d3 <= 0) { float v = d1 / (d1 - d3); return new Vector3(1 - v, v, 0); }
            var cp = p - c; float d5 = Vector3.Dot(ab, cp), d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0 && d5 <= d6) return new Vector3(0, 0, 1);
            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0 && d2 >= 0 && d6 <= 0) { float w = d2 / (d2 - d6); return new Vector3(1 - w, 0, w); }
            float va = d3 * d6 - d5 * d4;
            if (va <= 0 && d4 - d3 >= 0 && d5 - d6 >= 0) { float w = (d4 - d3) / ((d4 - d3) + (d5 - d6)); return new Vector3(0, 1 - w, w); }
            float inv = 1 / (va + vb + vc); return new Vector3(1 - (vb + vc) * inv, vb * inv, vc * inv);
        }
        static bool AdultEyeArtwork(Vector3 p)
        {
            // Source Kohaku/Tancho dark-pupil samples locate both eyes near |X|=.07,Z=.234.
            // Expanded anatomical volume includes eyelids and adjacent filter footprint, not just dark pixels.
            return Mathf.Abs(p.x) > .040f && p.z > .183f && p.z < .281f && p.y > -.049f && p.y < .048f;
        }
        static bool StageFace(Surface s, Vector3[] eyes, Vector3 p)
        {
            float scale = s.Bounds.size.x > .28f ? 1f : .8f;
            foreach (var eye in eyes)
            {
                var q = p - eye;
                q = new Vector3(q.x / (.064f * scale), q.y / (.060f * scale), q.z / (.067f * scale));
                // Preserve lateral eyeball, eyelid and cheek detail; dorsal inherited marking domain stays separate.
                if (q.sqrMagnitude <= 1 && p.y < .048f) return true;
            }
            var mouth = p - s.Bones[3];
            return mouth.x * mouth.x / (.070f * .070f) + mouth.y * mouth.y / (.043f * .043f) + mouth.z * mouth.z / (.041f * .041f) <= 1 && p.y < -.013f;
        }
        static void Search(Node n, Surface s, Vector3 p, Vector3 normal, ref float best, ref int face, ref Vector3 bary)
        {
            if (n == null || n.Bounds.SqrDistance(p) > best) return;
            if (n.Faces != null)
            {
                foreach (int f in n.Faces)
                {
                    int a = s.T[f * 3], b = s.T[f * 3 + 1], c = s.T[f * 3 + 2];
                    if (AdultEyeArtwork(s.V[a]) || AdultEyeArtwork(s.V[b]) || AdultEyeArtwork(s.V[c])) continue;
                    var w = Bary(p, s.V[a], s.V[b], s.V[c]);
                    var q = s.V[a] * w.x + s.V[b] * w.y + s.V[c] * w.z;
                    if (AdultEyeArtwork(q)) continue;
                    if (Mathf.Abs(p.x) > .015f && q.x * p.x < 0) continue;
                    // Head/body/tail bands exclude unrelated longitudinal anatomy.
                    if (Mathf.Abs(q.z - p.z) > .045f) continue;
                    var sn = (s.N[a] * w.x + s.N[b] * w.y + s.N[c] * w.z).normalized;
                    if (Vector3.Dot(normal, sn) < NormalDot) continue;
                    float d = (p - q).sqrMagnitude;
                    if (d < best) { best = d; face = f; bary = w; }
                }
                return;
            }
            if (n.Left.Bounds.SqrDistance(p) <= n.Right.Bounds.SqrDistance(p)) { Search(n.Left, s, p, normal, ref best, ref face, ref bary); Search(n.Right, s, p, normal, ref best, ref face, ref bary); }
            else { Search(n.Right, s, p, normal, ref best, ref face, ref bary); Search(n.Left, s, p, normal, ref best, ref face, ref bary); }
        }
        static Vector3 Map(Surface d, Surface a, Vector3 p, int region)
        {
            if (region != 0)
            {
                var db = d.RegionBounds[region]; var ab = a.RegionBounds[region];
                return ab.center + Vector3.Scale(p - db.center, new Vector3(ab.size.x / Mathf.Max(db.size.x, .00001f), ab.size.y / Mathf.Max(db.size.y, .00001f), ab.size.z / Mathf.Max(db.size.z, .00001f)));
            }
            // The body and caudal charts share the SAME -.17 peduncle boundary.
            // Old tail-bone extrapolation sent destination body samples into source caudal-only triangles.
            float[] dz = { -.17f, d.Bones[6].z, d.Bones[1].z, d.Bones[2].z, d.Bounds.max.z };
            float[] az = { -.17f, a.Bones[6].z, a.Bones[1].z, a.Bones[2].z, a.Bounds.max.z };
            float z = p.z;
            int i = 0; while (i < dz.Length - 2 && z > dz[i + 1]) i++;
            float t = (z - dz[i]) / (dz[i + 1] - dz[i]);
            float targetZ = Mathf.LerpUnclamped(az[i], az[i + 1], t);
            // Dorsal-head chart keeps longitudinal/lateral distances isotropic to preserve the authored Tancho circle.
            bool dorsalHead = p.z > d.Bones[2].z - .012f && p.y > .050f;
            if (dorsalHead) targetZ = p.z - d.Bones[2].z + a.Bones[2].z;
            float angle = Mathf.Atan2(p.y, p.x);
            float ratio = a.Radial(targetZ, angle) / Mathf.Max(d.Radial(p.z, angle), .0001f);
            return new Vector3(dorsalHead ? p.x : p.x * ratio, p.y * ratio, targetZ);
        }
        static Texture2D Decode(string path)
        {
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
            if (!ImageConversion.LoadImage(t, File.ReadAllBytes(path))) throw new Exception("PNG decode failed " + path);
            return t;
        }
        static void Save(string path, Color32[] pixels)
        {
            var t = new Texture2D(Size, Size, TextureFormat.RGBA32, false, false);
            t.SetPixels32(pixels); t.Apply(); File.WriteAllBytes(path, t.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(t);
        }
        static Vector3[] EyeCenters(Surface s, Texture2D source)
        {
            var sum = new Vector3[2]; var count = new int[2];
            for (int i = 0; i < s.V.Length; i++)
            {
                var p = s.V[i]; if (p.z < s.Bones[2].z - .065f || p.z > s.Bones[3].z - .025f || Mathf.Abs(p.x) < .04f || p.y < -.035f) continue;
                var c = source.GetPixelBilinear(s.UV[i].x, s.UV[i].y);
                if (c.r > .2f || c.g > .2f || c.b > .2f) continue;
                int side = p.x < 0 ? 0 : 1; sum[side] += p; count[side]++;
            }
            for (int i = 0; i < 2; i++) { if (count[i] == 0) throw new Exception("No pupil landmark candidates; requires reviewed landmark adjustment."); sum[i] /= count[i]; }
            return sum;
        }
        public static void Bake(string stage)
        {
            foreach (var dir in new[] { "Textures", "Materials", "Reports", "Scenes", "Profiles" }) Directory.CreateDirectory(Folder + "/" + dir);
            var adult = new Surface(Adult); var dst = new Surface(stage == "Fry" ? Fry : Juvenile);
            var trees = Enumerable.Range(0, 6).Select(r => Build(adult, Enumerable.Range(0, adult.Regions.Length).Where(t => adult.Regions[t] == r).ToArray())).ToArray();
            var original = Decode(AssetDatabase.GetAssetPath(dst.Skin.sharedMaterial.GetTexture("_BaseMap")));
            var eyes = EyeCenters(dst, original);
            File.WriteAllText(Folder + "/Profiles/" + stage + "_Step3B_Landmarks.json", "{\"eyeLeft\":\"" + eyes[0].ToString("F6") + "\",\"eyeRight\":\"" + eyes[1].ToString("F6") + "\",\"mouth\":\"" + dst.Bones[3].ToString("F6") + "\",\"maxDistance\":0.075,\"normalDot\":0.25,\"regions\":\"body,leftPectoral,rightPectoral,caudal,dorsal,ventral\",\"method\":\"measured angular body profiles; separate fin charts; original-stage face ellipsoids; conservative adult eye-surface exclusion; isotropic dorsal-head chart\"}");
            File.WriteAllLines(Folder + "/Reports/Step3B/" + stage + "_RegionBounds.txt", Enumerable.Range(0, 6).Select(r => "Region " + r + " destination=" + dst.RegionBounds[r] + " adult=" + adult.RegionBounds[r]));
            var sourceUV = new Vector2[Size * Size]; var originalUV = new Vector2[Size * Size]; var position = new Vector3[Size * Size];
            var auditKohaku = Decode("Assets/Models/Koi/ProofOfConcept/KohakuTopologyTransfer/Kohaku_OriginalMeshy_ToRigged_BaseColor.png");
            var auditTancho = Decode("Assets/Models/Koi/ProofOfConcept/TanchoTopologyTransfer/Tancho_OriginalMeshy_ToRigged_BaseColor.png");
            var state = new byte[Size * Size]; // 0 unused, 1 transfer, 2 explicitly protected, 3 rejected, 4 UV conflict
            long overlap = 0, conflicts = 0, rejected = 0, protectedCount = 0, accepted = 0, subpixel = 0;
            var regionAccepted = new long[6]; var regionRejected = new long[6]; var regionResidual = new double[6];
            double distanceSum = 0; float distanceMax = 0;
            for (int f = 0; f < dst.T.Length / 3; f++)
            {
                int ia = dst.T[f * 3], ib = dst.T[f * 3 + 1], ic = dst.T[f * 3 + 2];
                var a = dst.UV[ia] * Size; var b = dst.UV[ib] * Size; var c = dst.UV[ic] * Size;
                double det = (b.x - a.x) * (double)(c.y - a.y) - (b.y - a.y) * (double)(c.x - a.x);
                if (Math.Abs(det) < 1e-12) continue;
                int minX = Math.Max(0, Mathf.CeilToInt(Mathf.Min(a.x, b.x, c.x) - .5f)), maxX = Math.Min(Size - 1, Mathf.FloorToInt(Mathf.Max(a.x, b.x, c.x) - .5f));
                int minY = Math.Max(0, Mathf.CeilToInt(Mathf.Min(a.y, b.y, c.y) - .5f)), maxY = Math.Min(Size - 1, Mathf.FloorToInt(Mathf.Max(a.y, b.y, c.y) - .5f));
                int hits = 0;
                for (int y = minY; y <= maxY; y++) for (int x = minX; x <= maxX; x++)
                {
                    double px = x + .5 - a.x, py = y + .5 - a.y;
                    float wb = (float)((px * (c.y - a.y) - py * (c.x - a.x)) / det), wc = (float)(((b.x - a.x) * py - (b.y - a.y) * px) / det), wa = 1 - wb - wc;
                    if (wa < -1e-6f || wb < -1e-6f || wc < -1e-6f) continue;
                    hits++; int idx = y * Size + x;
                    var p = dst.V[ia] * wa + dst.V[ib] * wb + dst.V[ic] * wc;
                    var uv = dst.UV[ia] * wa + dst.UV[ib] * wb + dst.UV[ic] * wc;
                    if (state[idx] != 0)
                    {
                        overlap++;
                        bool bad = (position[idx] - p).sqrMagnitude > .00000001f;
                        if (!bad && state[idx] == 1)
                        {
                            var alternate = Map(dst, adult, p, dst.Regions[f]);
                            const float step = .0001f;
                            var j = Matrix4x4.identity;
                            j.SetColumn(0, (Map(dst, adult, p + Vector3.right * step, dst.Regions[f]) - alternate) / step);
                            j.SetColumn(1, (Map(dst, adult, p + Vector3.up * step, dst.Regions[f]) - alternate) / step);
                            j.SetColumn(2, (Map(dst, adult, p + Vector3.forward * step, dst.Regions[f]) - alternate) / step);
                            var nn = j.inverse.transpose.MultiplyVector(dst.N[ia] * wa + dst.N[ib] * wb + dst.N[ic] * wc).normalized;
                            float dd = MaxDistance * MaxDistance; int ff = -1; var ww = Vector3.zero;
                            Search(trees[dst.Regions[f]], adult, alternate, nn, ref dd, ref ff, ref ww);
                            bad = ff < 0;
                            if (!bad)
                            {
                                var otherUV = adult.UV[adult.T[ff * 3]] * ww.x + adult.UV[adult.T[ff * 3 + 1]] * ww.y + adult.UV[adult.T[ff * 3 + 2]] * ww.z;
                                foreach (var appearance in new[] { auditKohaku, auditTancho })
                                {
                                    var first = appearance.GetPixelBilinear(sourceUV[idx].x, sourceUV[idx].y);
                                    var second = appearance.GetPixelBilinear(otherUV.x, otherUV.y);
                                    bad |= Mathf.Max(Mathf.Abs(first.r - second.r), Mathf.Abs(first.g - second.g), Mathf.Abs(first.b - second.b)) > 2f / 255;
                                }
                            }
                        }
                        if (bad && state[idx] != 4) { state[idx] = 4; conflicts++; }
                        continue;
                    }
                    position[idx] = p; originalUV[idx] = uv;
                    if (StageFace(dst, eyes, p))
                    { state[idx] = 2; protectedCount++; continue; }
                    var mapped = Map(dst, adult, p, dst.Regions[f]);
                    // Transform destination normals by the inverse transpose of the local mapping Jacobian.
                    const float eps = .0001f;
                    var jac = Matrix4x4.identity; jac.SetColumn(0, (Map(dst, adult, p + Vector3.right * eps, dst.Regions[f]) - mapped) / eps); jac.SetColumn(1, (Map(dst, adult, p + Vector3.up * eps, dst.Regions[f]) - mapped) / eps); jac.SetColumn(2, (Map(dst, adult, p + Vector3.forward * eps, dst.Regions[f]) - mapped) / eps);
                    var normal = jac.inverse.transpose.MultiplyVector(dst.N[ia] * wa + dst.N[ib] * wb + dst.N[ic] * wc).normalized;
                    float best = MaxDistance * MaxDistance; int face = -1; var bary = Vector3.zero;
                    Search(trees[dst.Regions[f]], adult, mapped, normal, ref best, ref face, ref bary);
                    if (face < 0) { state[idx] = 3; rejected++; regionRejected[dst.Regions[f]]++; continue; }
                    var sourcePoint = adult.V[adult.T[face * 3]] * bary.x + adult.V[adult.T[face * 3 + 1]] * bary.y + adult.V[adult.T[face * 3 + 2]] * bary.z;
                    if (AdultEyeArtwork(sourcePoint)) throw new InvalidOperationException("Forbidden adult-eye source point selected.");
                    sourceUV[idx] = adult.UV[adult.T[face * 3]] * bary.x + adult.UV[adult.T[face * 3 + 1]] * bary.y + adult.UV[adult.T[face * 3 + 2]] * bary.z;
                    state[idx] = 1; accepted++; float dist = Mathf.Sqrt(best); distanceSum += dist; distanceMax = Mathf.Max(dist, distanceMax);
                    regionAccepted[dst.Regions[f]]++; regionResidual[dst.Regions[f]] += dist;
                }
                if (hits == 0) subpixel++;
            }
            UnityEngine.Object.DestroyImmediate(auditKohaku);
            UnityEngine.Object.DestroyImmediate(auditTancho);
            foreach (string variety in new[] { "Kohaku", "Tancho" })
            {
                string src = "Assets/Models/Koi/ProofOfConcept/" + variety + "TopologyTransfer/" + variety + "_OriginalMeshy_ToRigged_BaseColor.png";
                var tex = Decode(src); var pixels = new Color32[state.Length]; var coverage = new Color32[state.Length];
                for (int i = 0; i < pixels.Length; i++)
                {
                    byte s = state[i];
                    pixels[i] = s == 1 ? (Color32)tex.GetPixelBilinear(sourceUV[i].x, sourceUV[i].y) : s == 2 ? (Color32)original.GetPixelBilinear(originalUV[i].x, originalUV[i].y) : s == 3 ? new Color32(255, 0, 255, 255) : s == 4 ? new Color32(0, 255, 255, 255) : new Color32(0, 0, 0, 255);
                    coverage[i] = s == 1 ? new Color32(255, 255, 255, 255) : s == 2 ? new Color32(0, 255, 0, 255) : pixels[i];
                }
                // Padding is exclusively unused atlas space, never rejected/conflicting surface pixels.
                var occupied = state.Select(s => s != 0).ToArray();
                for (int pass = 0; pass < 8; pass++)
                {
                    var add = new List<int>();
                    for (int y = 1; y < Size - 1; y++) for (int x = 1; x < Size - 1; x++)
                    {
                        int i = y * Size + x; if (occupied[i]) continue;
                        foreach (int j in new[] { i - 1, i + 1, i - Size, i + Size }) if (occupied[j] && state[j] != 3 && state[j] != 4) { pixels[i] = pixels[j]; add.Add(i); break; }
                    }
                    foreach (int i in add) occupied[i] = true;
                }
                string path = Folder + "/Textures/" + variety + "_" + stage + "_Albedo_DERIVED.png";
                Save(path, pixels); Save(Folder + "/Reports/Step3B/" + variety + "_" + stage + "_Coverage.png", coverage);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                string materialPath = Folder + "/Materials/" + variety + "_" + stage + "_Pilot.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (mat == null) throw new InvalidOperationException("Existing pilot material missing: " + materialPath);
                if (mat.GetTexture("_BaseMap") != AssetDatabase.LoadAssetAtPath<Texture2D>(path))
                    throw new InvalidOperationException("Pilot albedo reference changed unexpectedly; refusing shared material edits.");
                UnityEngine.Object.DestroyImmediate(tex);
            }
            UnityEngine.Object.DestroyImmediate(original);
            File.WriteAllText(Folder + "/Reports/Step3B/" + stage + "_Metrics.txt", "PARTIAL pending visual review\naccepted=" + accepted + "\nprotected=" + protectedCount + "\nrejected=" + rejected + "\nUVoverlapEvents=" + overlap + "\nUVconflictingTexels=" + conflicts + "\nsubpixelFaces=" + subpixel + "\nmeanDistance=" + distanceSum / Math.Max(1, accepted) + "\nmaxDistance=" + distanceMax + "\nbodyLength=" + adult.Bounds.size.z + "\nThreshold maxDistance=.075; normalDot=.25; longitudinalBand=.045 unchanged\nAdult-eye sampled points=0 (asserted). No failed region filled. Magenta=rejected; cyan=UV conflict. Green coverage=original-stage protected detail.\n" + string.Join("\n", Enumerable.Range(0, 6).Select(r => "region=" + r + " accepted=" + regionAccepted[r] + " rejected=" + regionRejected[r] + " meanResidual=" + regionResidual[r] / Math.Max(1, regionAccepted[r]))));
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        }
        static GameObject CopyVisual(GameObject source)
        {
            var map = new Dictionary<Transform, Transform>();
            Action<Transform, Transform> clone = null;
            clone = (s, parent) => { var g = new GameObject(s.name); if (parent != null) g.transform.SetParent(parent, false); g.transform.localPosition = s.localPosition; g.transform.localRotation = s.localRotation; g.transform.localScale = s.localScale; map[s] = g.transform; foreach (Transform child in s) clone(child, g.transform); };
            clone(source.transform, null);
            foreach (var skin in source.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var c = map[skin.transform].gameObject.AddComponent<SkinnedMeshRenderer>(); c.sharedMesh = skin.sharedMesh; c.sharedMaterials = skin.sharedMaterials; c.bones = skin.bones.Select(b => map[b]).ToArray(); c.rootBone = map[skin.rootBone]; c.localBounds = skin.localBounds; c.updateWhenOffscreen = true;
            }
            return map[source.transform].gameObject;
        }
        public static void BuildScene()
        {
            var previous = EditorSceneManager.GetSceneManagerSetup();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive); UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
            for (int row = 0; row < 2; row++) for (int col = 0; col < 5; col++)
            {
                string variety = row == 0 ? "Kohaku" : "Tancho", stage = col < 3 ? "Fry" : "Juvenile";
                string path = col == 0 ? Adult : stage == "Fry" ? Fry : Juvenile;
                var go = CopyVisual(AssetDatabase.LoadAssetAtPath<GameObject>(path)); go.name = variety + "_" + (col == 0 ? "AdultSource" : stage + (col == 2 || col == 4 ? "_OriginalControl" : "_Pilot"));
                go.transform.position = new Vector3((col - 2) * .8f, 0, row * 1f);
                var s = go.GetComponentInChildren<SkinnedMeshRenderer>();
                if (col == 0) s.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Koi/ProofOfConcept/" + (variety == "Kohaku" ? "KohakuPbrTopologyTransfer" : "TanchoTopologyTransfer") + "/" + variety + "_OriginalMeshy_ToRigged_PBR_Test.mat");
                else if (col == 1 || col == 3) s.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Materials/" + variety + "_" + stage + "_Pilot.mat");
            }
            var light = new GameObject("Pilot_KeyLight").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.2f; light.transform.rotation = Quaternion.Euler(40, -25, 0);
            var camera = new GameObject("Pilot_Camera").AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 1.6f; camera.transform.position = new Vector3(0, 4, .5f); camera.transform.rotation = Quaternion.Euler(90, 0, 0); camera.backgroundColor = new Color(.12f, .12f, .12f); camera.clearFlags = CameraClearFlags.SolidColor;
            EditorSceneManager.SaveScene(scene, Folder + "/Scenes/EarlyStage_AlbedoTransfer_Test.unity");
            EditorSceneManager.CloseScene(scene, true); EditorSceneManager.RestoreSceneManagerSetup(previous);
        }
    }
}
