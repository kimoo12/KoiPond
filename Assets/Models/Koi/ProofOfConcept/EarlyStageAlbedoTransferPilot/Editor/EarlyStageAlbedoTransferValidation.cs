using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace KoiPond.EarlyStageTransferPilot
{
    public static class EarlyStageAlbedoTransferValidation
    {
        const string Root = EarlyStageAlbedoTransferBuilder.Folder;
        public static void Capture()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save user work before isolated capture; nothing was discarded.");
            var scene = EditorSceneManager.OpenScene(Root + "/Scenes/EarlyStage_AlbedoTransfer_Test.unity", OpenSceneMode.Single);
            var camera = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>()).Single();
            var skins = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SkinnedMeshRenderer>()).ToArray();
            var log = new StringBuilder();
            Directory.CreateDirectory(Root + "/Reports/Captures");
            var unlit = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            try
            {
                foreach (var skin in skins)
                {
                    foreach (var other in skins) other.enabled = other == skin;
                    var rig = skin.transform;
                    while (rig.parent != null) rig = rig.parent;
                    // Bounds from exact rest vertices, not inflated skinned culling bounds.
                    var vertices = skin.sharedMesh.vertices;
                    var bounds = new Bounds(skin.transform.TransformPoint(vertices[0]), Vector3.zero);
                    foreach (var v in vertices) bounds.Encapsulate(skin.transform.TransformPoint(v));
                    float length = bounds.size.z;
                    log.AppendLine(rig.name + " restLength=" + length + " unchangedMesh=" + AssetDatabase.GetAssetPath(skin.sharedMesh));
                    var material = skin.sharedMaterial;
                    for (int mode = 0; mode < 2; mode++)
                    {
                        if (mode == 1) { unlit.SetTexture("_BaseMap", material.GetTexture("_BaseMap")); unlit.SetColor("_BaseColor", Color.white); skin.sharedMaterial = unlit; }
                        RenderViews(camera, bounds, rig.name + (mode == 0 ? "_Lit_Rest" : "_Albedo_Rest"));
                    }
                    skin.sharedMaterial = material;
                    if (!rig.name.EndsWith("_Pilot")) continue;
                    var transforms = rig.GetComponentsInChildren<Transform>();
                    var positions = transforms.Select(t => t.localPosition).ToArray(); var rotations = transforms.Select(t => t.localRotation).ToArray(); var scales = transforms.Select(t => t.localScale).ToArray();
                    foreach (string clipName in new[] { "Swim", "Idle", "Eat" })
                    {
                        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Models/Koi/Animations/Koi_" + clipName + ".anim");
                        for (int sample = 0; sample <= 4; sample++)
                        {
                            for (int i = 0; i < transforms.Length; i++) { transforms[i].localPosition = positions[i]; transforms[i].localRotation = rotations[i]; transforms[i].localScale = scales[i]; }
                            for (int i = 0; i < skin.sharedMesh.blendShapeCount; i++) skin.SetBlendShapeWeight(i, 0);
                            clip.SampleAnimation(rig.gameObject, clip.length * sample / 4f);
                            var baked = new Mesh(); skin.BakeMesh(baked, true);
                            bool finite = baked.vertices.All(v => !float.IsNaN(v.x) && !float.IsInfinity(v.x) && !float.IsNaN(v.y) && !float.IsInfinity(v.y) && !float.IsNaN(v.z) && !float.IsInfinity(v.z));
                            log.AppendLine(rig.name + " " + clipName + " sample=" + sample + " finite=" + finite);
                            UnityEngine.Object.DestroyImmediate(baked);
                            if (sample == 2) RenderViews(camera, bounds, rig.name + "_" + clipName + "_HalfTime");
                        }
                    }
                    for (int i = 0; i < transforms.Length; i++) { transforms[i].localPosition = positions[i]; transforms[i].localRotation = rotations[i]; transforms[i].localScale = scales[i]; }
                    for (int i = 0; i < skin.sharedMesh.blendShapeCount; i++) skin.SetBlendShapeWeight(i, 0);
                }
                File.WriteAllText(Root + "/Reports/SampledValidation.txt", log + "\nStatic clip sampling only, 5 times/clip. Continuous runtime NOT RUN. Extreme-pose/complete-loop certification NOT CLAIMED.\n");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(unlit);
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
            AssetDatabase.Refresh();
        }
        static void RenderViews(Camera c, Bounds b, string label)
        {
            var directions = new[] { Vector3.right, Vector3.up, Vector3.forward };
            var names = new[] { "Side", "Top", "Front" };
            for (int view = 0; view < 3; view++)
            {
                c.transform.position = b.center + directions[view] * 3;
                c.transform.rotation = Quaternion.LookRotation(-directions[view], view == 1 ? Vector3.forward : Vector3.up);
                c.orthographicSize = view == 2 ? Mathf.Max(b.size.x, b.size.y) * .65f : b.size.z * .65f;
                c.nearClipPlane = .01f; c.farClipPlane = 10;
                var rt = RenderTexture.GetTemporary(1024, 1024, 24, RenderTextureFormat.ARGB32);
                var previous = RenderTexture.active; var target = c.targetTexture;
                var image = new Texture2D(1024, 1024, TextureFormat.RGBA32, false);
                try
                {
                    c.targetTexture = rt; c.Render(); RenderTexture.active = rt;
                    image.ReadPixels(new Rect(0, 0, 1024, 1024), 0, 0); image.Apply();
                    File.WriteAllBytes(Root + "/Reports/Captures/" + label + "_" + names[view] + ".png", image.EncodeToPNG());
                }
                finally { c.targetTexture = target; RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.DestroyImmediate(image); }
            }
        }
        public static void Preservation()
        {
            int same = 0; var errors = new StringBuilder();
            using (var h = SHA256.Create()) foreach (var line in File.ReadAllLines(Root + "/Reports/SourceProtection.tsv"))
            {
                var fields = line.Split('\t');
                if (!File.Exists(fields[0])) { errors.AppendLine("MISSING " + fields[0]); continue; }
                string hash = BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(fields[0]))).Replace("-", "");
                if (hash == fields[1]) same++; else errors.AppendLine("CHANGED " + fields[0]);
            }
            File.WriteAllText(Root + "/Reports/PreservationResults.txt", "Unchanged=" + same + "\n" + (errors.Length == 0 ? "PASS protected bytes and metadata unchanged\n" : errors.ToString()));
            File.WriteAllLines(Root + "/Reports/CreatedFiles.txt", Directory.GetFiles(Root, "*", SearchOption.AllDirectories).Select(p => p.Replace('\\', '/')).OrderBy(p => p));
        }
    }
}
