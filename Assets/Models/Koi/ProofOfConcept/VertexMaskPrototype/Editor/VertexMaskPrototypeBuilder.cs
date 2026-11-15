using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace KoiPond.VertexMaskPrototype
{
    public static class VertexMaskPrototypeBuilder
    {
        public const string Folder = "Assets/Models/Koi/ProofOfConcept/VertexMaskPrototype";
        private const string Fry = "Assets/Models/Koi/ProofOfConcept/FryRiggingPrototype/Prefabs/Fry_RiggingPrototype.prefab";
        private const string Juvenile = "Assets/Models/Koi/ProofOfConcept/JuvenileRiggingPrototype/Prefabs/Juvenile_RiggingPrototype.prefab";
        private static readonly string[] Sources = { Fry, Juvenile };
        private static readonly string[] Names = { "Fry", "Juvenile" };
        private enum Region : byte { Uncertain, Body, LeftEye, RightEye, Lips, Fins }
        private static readonly Color32[] Palette = {
            new Color32(220, 30, 200, 0), new Color32(40, 180, 70, 1),
            new Color32(250, 200, 20, 2), new Color32(250, 110, 20, 3),
            new Color32(230, 40, 35, 4), new Color32(20, 175, 220, 5)
        };

        [MenuItem("Koi/Vertex Mask Prototype/Build Diagnostic Once")]
        public static void Build()
        {
            if (File.Exists(Folder + "/Scenes/Koi_VertexMask_Test.unity"))
                throw new InvalidOperationException("Diagnostic exists. Do not overwrite or iterate without review.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Unsaved scene work: stop before switching scenes.");
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(Folder + "/Shaders/VertexMaskDiagnostic.shader");
            if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Diagnostic shader unavailable or failed compilation.");
            foreach (var dir in new[] { "Meshes", "Materials", "Scenes", "Reports", "Reports/Captures" })
                Directory.CreateDirectory(Folder + "/" + dir);
            AssetDatabase.Refresh();
            string baseline = SessionState.GetString("VertexMask.SourceProtection", "");
            if (string.IsNullOrEmpty(baseline)) throw new InvalidOperationException("Fresh protected-source baseline required.");
            File.WriteAllText(Folder + "/Reports/SourceProtection.tsv", baseline);
            SessionState.SetString("VertexMask.PreviousScene", SceneManager.GetActiveScene().path);
            var copies = new Mesh[2];
            var report = new StringBuilder("# Diagnostic anatomy mask candidate — pending visual approval\n\n");
            report.AppendLine("Legend: green=conservative body interior; yellow/orange=left/right eye envelope; red=lip envelope; cyan=fin candidate; magenta=uncertain/protected. RGB is diagnostic only; alpha stores a vertex label, NOT opacity or an appearance channel. No genetic compositing.\n");
            for (int stage = 0; stage < 2; stage++)
            {
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(Sources[stage]);
                var skin = source.GetComponentInChildren<SkinnedMeshRenderer>();
                var original = skin.sharedMesh;
                string path = Folder + "/Meshes/" + Names[stage] + "_VertexMaskCopy.asset";
                if (File.Exists(path)) throw new InvalidOperationException("Mesh output collision.");
                var transform = source.transform.worldToLocalMatrix * skin.transform.localToWorldMatrix;
                var vertices = original.vertices.Select(transform.MultiplyPoint3x4).ToArray();
                var normals = original.normals.Select(n => transform.inverse.transpose.MultiplyVector(n).normalized).ToArray();
                var labels = new Region[vertices.Length];
                for (int i = 0; i < vertices.Length; i++) labels[i] = Classify(vertices[i], normals[i], stage);
                int seamVerified, seamUncertain;
                ReviewSeams(original, vertices, normals, labels, out seamVerified, out seamUncertain);
                // Single non-cumulative conservative boundary pass: never turn unknown/protected vertices into body.
                var initial = (Region[])labels.Clone();
                int boundary = 0;
                var indices = original.triangles;
                for (int i = 0; i < indices.Length; i += 3)
                {
                    int a = indices[i], b = indices[i + 1], c = indices[i + 2];
                    bool body = initial[a] == Region.Body || initial[b] == Region.Body || initial[c] == Region.Body;
                    bool protectedRegion = initial[a] != Region.Body || initial[b] != Region.Body || initial[c] != Region.Body;
                    if (!body || !protectedRegion) continue;
                    boundary++;
                    foreach (int j in new[] { a, b, c }) if (labels[j] == Region.Body) labels[j] = Region.Uncertain;
                }
                // Reconcile matching seam vertices after the boundary pass, still protection-only.
                ReviewSeams(original, vertices, normals, labels, out seamVerified, out seamUncertain);
                var copy = UnityEngine.Object.Instantiate(original);
                copy.name = Names[stage] + "_VertexMaskCopy";
                copy.colors32 = labels.Select(l => Palette[(int)l]).ToArray();
                ValidateCopy(original, copy);
                AssetDatabase.CreateAsset(copy, path);
                copies[stage] = copy;
                report.AppendLine("## " + Names[stage]);
                foreach (Region region in Enum.GetValues(typeof(Region)))
                    report.AppendLine(region + " vertices: " + labels.Count(l => l == region));
                report.AppendLine("Boundary triangles conservatively guarded: " + boundary);
                report.AppendLine("Verified candidate seam groups: " + seamVerified + "; incompatible/unconfirmed groups: " + seamUncertain);
                report.AppendLine("Eye envelopes come from original dark-eye surface samples plus an expanded eyelid margin. Lip envelope is localized to the existing Mouth/rest muzzle. Fin domains conservatively combine explicit distal/attachment envelopes and thin-surface orientation. These are candidate selections, not independently certified anatomy. Head/cheek/fin-root uncertainty deliberately remains magenta.\n");
            }
            var diagnostic = new Material(shader) { name = "AnatomyMask_Diagnostic" };
            AssetDatabase.CreateAsset(diagnostic, Folder + "/Materials/AnatomyMask_Diagnostic.mat");
            AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            for (int stage = 0; stage < 2; stage++)
            {
                for (int mode = 0; mode < 2; mode++)
                {
                    var source = AssetDatabase.LoadAssetAtPath<GameObject>(Sources[stage]);
                    var instance = UnityEngine.Object.Instantiate(source);
                    instance.name = Names[stage] + (mode == 0 ? "_Original" : "_MaskCandidate");
                    // Equal magnification of existing complete stage hierarchy, outside the keyed root.
                    instance.transform.localScale *= stage == 0 ? 2f : 1f;
                    instance.transform.localPosition = new Vector3(mode == 0 ? -.30f : .30f, 0, stage == 0 ? .24f : -.24f);
                    var skin = instance.GetComponentInChildren<SkinnedMeshRenderer>();
                    if (mode == 1) { skin.sharedMesh = copies[stage]; skin.sharedMaterial = diagnostic; }
                    skin.updateWhenOffscreen = true;
                }
            }
            var cameraObject = new GameObject("DiagnosticCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = .70f; camera.nearClipPlane = .01f;
            camera.farClipPlane = 20; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.075f, .085f, .10f);
            camera.transform.position = new Vector3(1.6f, 1.3f, .8f); camera.transform.LookAt(Vector3.zero);
            var lightObject = new GameObject("DiagnosticLight");
            var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.1f;
            light.transform.rotation = Quaternion.Euler(35, -30, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.40f, .40f, .40f);
            EditorSceneManager.SaveScene(scene, Folder + "/Scenes/Koi_VertexMask_Test.unity");
            report.AppendLine("## Status\nExact-copy geometry/UV/skinning/blendshape checks passed during build. Visual protection and sampled animation checks pending. No appearance compositing, neutral colors, red markings, growth or production integration. Stop for mask review.");
            File.WriteAllText(Folder + "/Reports/MaskResults.md", report.ToString());
            AssetDatabase.Refresh();
        }

        private static bool Ellipsoid(Vector3 p, Vector3 center, Vector3 radii)
        {
            var q = p - center;
            q = new Vector3(q.x / radii.x, q.y / radii.y, q.z / radii.z);
            return q.sqrMagnitude <= 1f;
        }

        private static Region Classify(Vector3 p, Vector3 n, int stage)
        {
            bool fry = stage == 0;
            var eye = fry ? new Vector3(.116f, -.009f, .241f) : new Vector3(.081f, .002f, .230f);
            var radius = fry ? new Vector3(.035f, .039f, .044f) : new Vector3(.027f, .037f, .039f);
            if (Ellipsoid(p, new Vector3(-eye.x, eye.y, eye.z), radius)) return Region.LeftEye;
            if (Ellipsoid(p, eye, radius)) return Region.RightEye;
            var mouth = fry ? new Vector3(0, -.036f, .292f) : new Vector3(0, -.044f, .271f);
            if (p.y < -.013f && Ellipsoid(p, mouth, new Vector3(.044f, .031f, .037f))) return Region.Lips;
            // Explicit conservative distal fins and medial thin-blade domains; no assumption of bone coverage.
            if (p.z < -.205f) return Region.Fins;
            if (p.z < .17f && Mathf.Abs(p.x) < .026f && Mathf.Abs(n.x) > .75f && (p.y > .054f || p.y < -.063f)) return Region.Fins;
            float finX = fry ? .078f : .055f;
            if (p.z > .045f && p.z < .182f && p.y < -.090f && Mathf.Abs(p.x) > finX) return Region.Fins;
            if (p.z > -.15f && p.z < -.035f && p.y < -.097f) return Region.Fins;
            // Head, belly/attachment margins and dorsal base are deliberately uncertain.
            if (p.z > -.175f && p.z < .168f && p.y > -.060f && p.y < .084f
                && !(Mathf.Abs(p.x) < .034f && p.y > .048f)) return Region.Body;
            return Region.Uncertain;
        }

        private static float Weight(BoneWeight w, int bone)
        {
            return (w.boneIndex0 == bone ? w.weight0 : 0) + (w.boneIndex1 == bone ? w.weight1 : 0)
                + (w.boneIndex2 == bone ? w.weight2 : 0) + (w.boneIndex3 == bone ? w.weight3 : 0);
        }

        private static void ReviewSeams(Mesh mesh, Vector3[] vertices, Vector3[] normals, Region[] labels, out int verified, out int uncertain)
        {
            verified = uncertain = 0;
            var weights = mesh.boneWeights;
            var groups = new Dictionary<Vector3Int, List<int>>();
            for (int i = 0; i < vertices.Length; i++)
            {
                var p = vertices[i];
                var key = new Vector3Int(Mathf.RoundToInt(p.x * 1000000), Mathf.RoundToInt(p.y * 1000000), Mathf.RoundToInt(p.z * 1000000));
                if (!groups.TryGetValue(key, out var ids)) { ids = new List<int>(); groups[key] = ids; }
                ids.Add(i);
            }
            foreach (var ids in groups.Values)
            {
                if (ids.Count < 2) continue;
                int first = ids[0]; bool safe = true;
                foreach (int i in ids)
                {
                    if ((vertices[i] - vertices[first]).sqrMagnitude > 1e-12f || Vector3.Dot(normals[i], normals[first]) < .95f) safe = false;
                    for (int b = 0; b < mesh.bindposes.Length; b++)
                        if (Mathf.Abs(Weight(weights[i], b) - Weight(weights[first], b)) > 1e-4f) safe = false;
                }
                if (!safe) { uncertain++; foreach (int i in ids) if (labels[i] == Region.Body) labels[i] = Region.Uncertain; continue; }
                verified++;
                var distinct = ids.Select(i => labels[i]).Distinct().ToArray();
                if (distinct.Length == 1) continue;
                var protectedLabels = distinct.Where(l => l != Region.Body).ToArray();
                Region choice = protectedLabels.Length == 1 ? protectedLabels[0] : Region.Uncertain;
                foreach (int i in ids) labels[i] = choice;
            }
        }

        private static bool Same<T>(T[] a, T[] b) { return a.SequenceEqual(b); }
        public static void ValidateCopy(Mesh source, Mesh copy)
        {
            if (!Same(source.vertices, copy.vertices) || !Same(source.normals, copy.normals)
                || !Same(source.tangents, copy.tangents) || !Same(source.boneWeights, copy.boneWeights)
                || !Same(source.bindposes, copy.bindposes) || source.bounds != copy.bounds
                || source.subMeshCount != copy.subMeshCount || source.blendShapeCount != copy.blendShapeCount)
                throw new InvalidOperationException("Copied mesh changed non-color source attributes.");
            for (int s = 0; s < source.subMeshCount; s++)
                if (!Same(source.GetIndices(s), copy.GetIndices(s)) || !source.GetSubMesh(s).Equals(copy.GetSubMesh(s)))
                    throw new InvalidOperationException("Submesh mismatch.");
            for (int channel = 0; channel < 8; channel++)
            {
                var a = new List<Vector4>(); var b = new List<Vector4>();
                source.GetUVs(channel, a); copy.GetUVs(channel, b);
                if (!a.SequenceEqual(b)) throw new InvalidOperationException("UV mismatch.");
            }
            for (int shape = 0; shape < source.blendShapeCount; shape++)
            {
                if (source.GetBlendShapeName(shape) != copy.GetBlendShapeName(shape) || source.GetBlendShapeFrameCount(shape) != copy.GetBlendShapeFrameCount(shape))
                    throw new InvalidOperationException("Shape structure mismatch.");
                for (int frame = 0; frame < source.GetBlendShapeFrameCount(shape); frame++)
                {
                    if (source.GetBlendShapeFrameWeight(shape, frame) != copy.GetBlendShapeFrameWeight(shape, frame)) throw new InvalidOperationException("Shape weight mismatch.");
                    var av = new Vector3[source.vertexCount]; var an = new Vector3[source.vertexCount]; var at = new Vector3[source.vertexCount];
                    var bv = new Vector3[source.vertexCount]; var bn = new Vector3[source.vertexCount]; var bt = new Vector3[source.vertexCount];
                    source.GetBlendShapeFrameVertices(shape, frame, av, an, at); copy.GetBlendShapeFrameVertices(shape, frame, bv, bn, bt);
                    if (!Same(av, bv) || !Same(an, bn) || !Same(at, bt)) throw new InvalidOperationException("Shape deltas mismatch.");
                }
            }
        }

        [MenuItem("Koi/Vertex Mask Prototype/Verify and Capture")]
        public static void VerifyAndCapture()
        {
            if (SceneManager.GetActiveScene().path != Folder + "/Scenes/Koi_VertexMask_Test.unity") throw new InvalidOperationException("Open isolated diagnostic scene first.");
            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            var camera = roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).Single();
            var allSkins = roots.SelectMany(r => r.GetComponentsInChildren<SkinnedMeshRenderer>(true)).ToArray();
            var rt = new RenderTexture(1024, 1024, 24);
            var previousRT = RenderTexture.active;
            var cameraPosition = camera.transform.position; var cameraRotation = camera.transform.rotation;
            var cameraSize = camera.orthographicSize; var background = camera.backgroundColor;
            var lines = new StringBuilder("# Validation and capture results\n\n");
            try
            {
                camera.targetTexture = rt;
                for (int stage = 0; stage < 2; stage++)
                {
                    var source = AssetDatabase.LoadAssetAtPath<GameObject>(Sources[stage]).GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
                    var copy = AssetDatabase.LoadAssetAtPath<Mesh>(Folder + "/Meshes/" + Names[stage] + "_VertexMaskCopy.asset");
                    ValidateCopy(source, copy);
                    if (copy.colors32.Length != copy.vertexCount || copy.colors32.Any(c => c.a > 5)) throw new InvalidOperationException("Invalid label stream.");
                    lines.AppendLine(Names[stage] + ": exact non-color mesh preservation PASS; all vertex labels valid.");
                    for (int mode = 0; mode < 2; mode++)
                    {
                        string name = Names[stage] + (mode == 0 ? "_Original" : "_MaskCandidate");
                        var target = roots.Single(r => r.name == name);
                        var skin = target.GetComponentInChildren<SkinnedMeshRenderer>();
                        foreach (var other in allSkins) other.enabled = other == skin;
                        var points = skin.sharedMesh.vertices.Select(skin.transform.TransformPoint).ToArray();
                        var bounds = new Bounds(points[0], Vector3.zero); foreach (var p in points) bounds.Encapsulate(p);
                        var directions = new[] { Vector3.left, Vector3.right, Vector3.up };
                        var viewNames = new[] { "Left", "Right", "Top" };
                        for (int view = 0; view < 3; view++)
                        {
                            var center = bounds.center;
                            camera.orthographicSize = bounds.size.z * .60f;
                            camera.transform.position = center + directions[view] * 2;
                            camera.transform.LookAt(center, view == 2 ? Vector3.forward : Vector3.up);
                            CapturePNG(camera, rt, Folder + "/Reports/Captures/" + name + "_" + viewNames[view] + ".png");
                        }
                        for (int detail = 0; detail < 3; detail++)
                        {
                            bool fry = stage == 0;
                            Vector3 local = detail == 2 ? new Vector3(0, -.01f, .10f) : new Vector3(detail == 0 ? -.10f : .10f, 0, .235f);
                            var center = target.transform.TransformPoint(local);
                            camera.orthographicSize = detail == 2 ? .072f : .048f;
                            camera.transform.position = center + (detail == 2 ? Vector3.up : detail == 0 ? Vector3.left : Vector3.right) * 2;
                            camera.transform.LookAt(center, detail == 2 ? Vector3.forward : Vector3.up);
                            CapturePNG(camera, rt, Folder + "/Reports/Captures/" + name + (detail == 2 ? "_FinBase" : detail == 0 ? "_LeftFace" : "_RightFace") + ".png");
                        }
                    }
                    // Static EditMode source clip samples, not a runtime playback pass.
                    var sampleTarget = roots.Single(r => r.name == Names[stage] + "_MaskCandidate");
                    var transforms = sampleTarget.GetComponentsInChildren<Transform>(true);
                    var positions = transforms.Select(t => t.localPosition).ToArray();
                    var rotations = transforms.Select(t => t.localRotation).ToArray();
                    var scales = transforms.Select(t => t.localScale).ToArray();
                    var sampleSkin = sampleTarget.GetComponentInChildren<SkinnedMeshRenderer>();
                    foreach (var other in allSkins) other.enabled = other == sampleSkin;
                    var colors = sampleSkin.sharedMesh.colors32;
                    var baked = new Mesh();
                    try
                    {
                        foreach (string clipName in new[] { "Koi_Swim", "Koi_Eat" })
                        {
                            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Models/Koi/Animations/" + clipName + ".anim");
                            if (clip == null) throw new InvalidOperationException("Missing source clip.");
                            foreach (float ratio in new[] { 0f, .5f, 1f })
                            {
                                for (int i = 0; i < transforms.Length; i++) { transforms[i].localPosition = positions[i]; transforms[i].localRotation = rotations[i]; transforms[i].localScale = scales[i]; }
                                sampleSkin.SetBlendShapeWeight(0, 0);
                                clip.SampleAnimation(sampleTarget, clip.length * ratio);
                                sampleSkin.BakeMesh(baked, true);
                                if (baked.vertexCount != copy.vertexCount || baked.vertices.Any(p => float.IsNaN(p.x) || float.IsInfinity(p.x) || float.IsNaN(p.y) || float.IsInfinity(p.y) || float.IsNaN(p.z) || float.IsInfinity(p.z)))
                                    throw new InvalidOperationException("Invalid sampled skinning.");
                                if (!colors.SequenceEqual(sampleSkin.sharedMesh.colors32)) throw new InvalidOperationException("Mask stream changed during animation.");
                                if (ratio == .5f)
                                {
                                    var center = sampleSkin.bounds.center;
                                    camera.orthographicSize = .23f;
                                    camera.transform.position = center + Vector3.right * 2; camera.transform.LookAt(center);
                                    CapturePNG(camera, rt, Folder + "/Reports/Captures/" + Names[stage] + "_Mask_" + clipName + "_Mid.png");
                                }
                            }
                            lines.AppendLine(Names[stage] + " " + clipName + ": 0/50/100% static samples finite; source vertex masks unchanged. Continuous Play Mode NOT RUN.");
                        }
                    }
                    finally
                    {
                        for (int i = 0; i < transforms.Length; i++) { transforms[i].localPosition = positions[i]; transforms[i].localRotation = rotations[i]; transforms[i].localScale = scales[i]; }
                        sampleSkin.SetBlendShapeWeight(0, 0);
                        UnityEngine.Object.DestroyImmediate(baked);
                    }
                }
            }
            finally
            {
                foreach (var skin in allSkins) skin.enabled = true;
                camera.targetTexture = null; camera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
                camera.orthographicSize = cameraSize; camera.backgroundColor = background;
                RenderTexture.active = previousRT; rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
            }
            lines.AppendLine("\nCandidate region coverage is NOT certified. Magenta remains protected uncertainty. No neutral Fry or Juvenile red pattern was created. Stop for diagnostic mask review.");
            File.WriteAllText(Folder + "/Reports/Validation.md", lines.ToString());
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.Refresh();
        }

        private static void CapturePNG(Camera camera, RenderTexture rt, string path)
        {
            camera.Render(); RenderTexture.active = rt;
            var texture = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            try { texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }
    }
}
