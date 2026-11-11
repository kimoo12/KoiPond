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
            public SkinnedMeshRenderer Skin;
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
                    Regions[t] = l > 1.05f ? 1 : r > 1.05f ? 2 : 0;
                }
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
        static void Search(Node n, Surface s, Vector3 p, Vector3 normal, ref float best, ref int face, ref Vector3 bary)
        {
            if (n == null || n.Bounds.SqrDistance(p) > best) return;
            if (n.Faces != null)
            {
                foreach (int f in n.Faces)
                {
                    int a = s.T[f * 3], b = s.T[f * 3 + 1], c = s.T[f * 3 + 2];
                    var w = Bary(p, s.V[a], s.V[b], s.V[c]);
                    var q = s.V[a] * w.x + s.V[b] * w.y + s.V[c] * w.z;
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
                int bone = region == 1 ? 4 : 5;
                var ratio = new Vector3(a.Bounds.size.x / d.Bounds.size.x, a.Bounds.size.y / d.Bounds.size.y, 1);
                return a.Bones[bone] + Vector3.Scale(p - d.Bones[bone], ratio);
            }
            int[] chain = { 9, 8, 7, 6, 1, 2, 3 };
            float z = p.z;
            int i = 0; while (i < chain.Length - 2 && z > d.Bones[chain[i + 1]].z) i++;
            int lo = chain[i], hi = chain[i + 1];
            float t = (z - d.Bones[lo].z) / (d.Bones[hi].z - d.Bones[lo].z);
            var dc = Vector3.LerpUnclamped(d.Bones[lo], d.Bones[hi], t);
            var ac = Vector3.LerpUnclamped(a.Bones[lo], a.Bones[hi], t);
            return ac + new Vector3((p.x - dc.x) * a.Bounds.size.x / d.Bounds.size.x, (p.y - dc.y) * a.Bounds.size.y / d.Bounds.size.y, 0);
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
            var trees = Enumerable.Range(0, 3).Select(r => Build(adult, Enumerable.Range(0, adult.Regions.Length).Where(t => adult.Regions[t] == r).ToArray())).ToArray();
            var original = Decode(AssetDatabase.GetAssetPath(dst.Skin.sharedMaterial.GetTexture("_BaseMap")));
            var eyes = EyeCenters(dst, original);
            File.WriteAllText(Folder + "/Profiles/" + stage + "_Landmarks.json", "{\"eyeLeft\":\"" + eyes[0] + "\",\"eyeRight\":\"" + eyes[1] + "\",\"mouth\":\"" + dst.Bones[3] + "\",\"maxDistance\":0.075,\"normalDot\":0.25,\"eyeRadius\":0.026,\"lipRadius\":0.022,\"method\":\"piecewise bone-anchor longitudinal mapping; independently anchored left/right fins; candidate pupil centroids require visual review\"}");
            var sourceUV = new Vector2[Size * Size]; var originalUV = new Vector2[Size * Size]; var position = new Vector3[Size * Size];
            var state = new byte[Size * Size]; // 0 unused, 1 transfer, 2 explicitly protected, 3 rejected, 4 UV conflict
            long overlap = 0, conflicts = 0, rejected = 0, protectedCount = 0, accepted = 0, subpixel = 0;
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
                        if ((position[idx] - p).sqrMagnitude > .00000001f && state[idx] != 4) { state[idx] = 4; conflicts++; }
                        continue;
                    }
                    position[idx] = p; originalUV[idx] = uv;
                    if ((p - eyes[0]).sqrMagnitude < .026f * .026f || (p - eyes[1]).sqrMagnitude < .026f * .026f || (p - dst.Bones[3]).sqrMagnitude < .022f * .022f)
                    { state[idx] = 2; protectedCount++; continue; }
                    var mapped = Map(dst, adult, p, dst.Regions[f]);
                    // Transform destination normals by the inverse transpose of the local mapping Jacobian.
                    const float eps = .0001f;
                    var jac = Matrix4x4.identity; jac.SetColumn(0, (Map(dst, adult, p + Vector3.right * eps, dst.Regions[f]) - mapped) / eps); jac.SetColumn(1, (Map(dst, adult, p + Vector3.up * eps, dst.Regions[f]) - mapped) / eps); jac.SetColumn(2, (Map(dst, adult, p + Vector3.forward * eps, dst.Regions[f]) - mapped) / eps);
                    var normal = jac.inverse.transpose.MultiplyVector(dst.N[ia] * wa + dst.N[ib] * wb + dst.N[ic] * wc).normalized;
                    float best = MaxDistance * MaxDistance; int face = -1; var bary = Vector3.zero;
                    Search(trees[dst.Regions[f]], adult, mapped, normal, ref best, ref face, ref bary);
                    if (face < 0) { state[idx] = 3; rejected++; continue; }
                    sourceUV[idx] = adult.UV[adult.T[face * 3]] * bary.x + adult.UV[adult.T[face * 3 + 1]] * bary.y + adult.UV[adult.T[face * 3 + 2]] * bary.z;
                    state[idx] = 1; accepted++; float dist = Mathf.Sqrt(best); distanceSum += dist; distanceMax = Mathf.Max(dist, distanceMax);
                }
                if (hits == 0) subpixel++;
            }
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
                Save(path, pixels); Save(Folder + "/Reports/" + variety + "_" + stage + "_Coverage.png", coverage);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.sRGBTexture = true; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.maxTextureSize = Size; importer.mipmapEnabled = true; importer.wrapMode = TextureWrapMode.Clamp; importer.SaveAndReimport();
                var mat = new Material(dst.Skin.sharedMaterial); mat.name = variety + " " + stage + " Pilot"; mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(path)); if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", mat.GetTexture("_BaseMap"));
                AssetDatabase.CreateAsset(mat, Folder + "/Materials/" + variety + "_" + stage + "_Pilot.mat");
                UnityEngine.Object.DestroyImmediate(tex);
            }
            UnityEngine.Object.DestroyImmediate(original);
            File.WriteAllText(Folder + "/Reports/" + stage + "_Metrics.txt", "PARTIAL pending visual review\naccepted=" + accepted + "\nprotected=" + protectedCount + "\nrejected=" + rejected + "\nUVoverlapEvents=" + overlap + "\nUVconflictingTexels=" + conflicts + "\nsubpixelFaces=" + subpixel + "\nmeanDistance=" + distanceSum / Math.Max(1, accepted) + "\nmaxDistance=" + distanceMax + "\nbodyLength=" + adult.Bounds.size.z + "\nThreshold maxDistance=.075; normalDot=.25; longitudinalBand=.045\nNo failed region filled. Magenta=rejected; cyan=UV conflict. Green coverage=original-stage protected detail.\nCounts before later overlap conflicts; final masks authoritative.\n");
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
