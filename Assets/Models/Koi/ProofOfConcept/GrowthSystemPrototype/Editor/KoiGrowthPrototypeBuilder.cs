using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace KoiPond.GrowthPrototype.EditorTools
{
    public static class KoiGrowthPrototypeBuilder
    {
        public const string Root = "Assets/Models/Koi/ProofOfConcept/GrowthSystemPrototype";
        public const string ScenePath = Root + "/Scenes/Koi_GrowthSystem_Test.unity";
        public static readonly string[] StageSources = {
            "Assets/Models/Koi/ProofOfConcept/FryRiggingPrototype/Prefabs/Fry_RiggingPrototype.prefab",
            "Assets/Models/Koi/ProofOfConcept/JuvenileRiggingPrototype/Prefabs/Juvenile_RiggingPrototype.prefab",
            "Assets/Prefabs/Koi_Stage3_Adult.prefab" };
        public static AnimationClip[] SourceClips()
        {
            return new[] { "Swim", "Idle", "Eat" }.Select(n => AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Models/Koi/Animations/Koi_" + n + ".anim")).ToArray();
        }
        private static Transform CopyTransform(Transform source, Transform parent, Dictionary<Transform, Transform> map)
        {
            var go = new GameObject(source.name);
            Undo.RegisterCreatedObjectUndo(go, "Create isolated growth visual");
            var t = go.transform;
            t.SetParent(parent, false); t.localPosition = source.localPosition;
            t.localRotation = source.localRotation; t.localScale = source.localScale;
            map.Add(source, t);
            foreach (Transform child in source) CopyTransform(child, t, map);
            return t;
        }
        public static GameObject CopyVisual(int stage, Transform parent, out SkinnedMeshRenderer skin)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(StageSources[stage]);
            if (source == null) throw new InvalidOperationException("Missing completed stage source.");
            var map = new Dictionary<Transform, Transform>();
            var rig = CopyTransform(source.transform, parent, map).gameObject;
            var sourceSkin = source.GetComponentInChildren<SkinnedMeshRenderer>(true);
            skin = map[sourceSkin.transform].gameObject.AddComponent<SkinnedMeshRenderer>();
            skin.sharedMesh = sourceSkin.sharedMesh; skin.sharedMaterials = sourceSkin.sharedMaterials;
            skin.bones = sourceSkin.bones.Select(b => map[b]).ToArray(); skin.rootBone = map[sourceSkin.rootBone];
            skin.localBounds = sourceSkin.localBounds; skin.updateWhenOffscreen = true;
            skin.quality = sourceSkin.quality; skin.shadowCastingMode = sourceSkin.shadowCastingMode;
            skin.receiveShadows = sourceSkin.receiveShadows;
            var animator = rig.AddComponent<Animator>();
            animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            // Deliberately no source AnimatorController, collider or production MonoBehaviour.
            foreach (var pair in map)
                if (pair.Key.localPosition != pair.Value.localPosition || pair.Key.localRotation != pair.Value.localRotation
                    || pair.Key.localScale != pair.Value.localScale) throw new InvalidOperationException("Copied rig transform mismatch.");
            ValidateBindings(rig, SourceClips());
            return rig;
        }
        public static void ValidateBindings(GameObject rig, AnimationClip[] clips)
        {
            var paths = rig.GetComponentsInChildren<Transform>(true)
                .ToDictionary(t => AnimationUtility.CalculateTransformPath(t, rig.transform));
            foreach (var clip in clips)
            {
                if (clip == null) throw new InvalidOperationException("Missing source clip.");
                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    if (!paths.TryGetValue(binding.path, out var target)) throw new InvalidOperationException("Unresolved path: " + binding.path);
                    if (target.GetComponent(binding.type) == null) throw new InvalidOperationException("Unresolved component: " + binding.type);
                    if (binding.propertyName.StartsWith("blendShape."))
                    {
                        var skin = target.GetComponent<SkinnedMeshRenderer>();
                        if (skin.sharedMesh.GetBlendShapeIndex(binding.propertyName.Substring(11)) < 0)
                            throw new InvalidOperationException("Unresolved shape: " + binding.propertyName);
                    }
                }
                if (AnimationUtility.GetObjectReferenceCurveBindings(clip).Length != 0 || AnimationUtility.GetAnimationEvents(clip).Length != 0)
                    throw new InvalidOperationException("Source clip object curves/events need explicit review.");
            }
        }
        public static float RestLength(SkinnedMeshRenderer skin, Transform reference)
        {
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (var v in skin.sharedMesh.vertices)
            {
                float z = reference.InverseTransformPoint(skin.transform.TransformPoint(v)).z;
                lo = Mathf.Min(lo, z); hi = Mathf.Max(hi, z);
            }
            return hi - lo;
        }
        [MenuItem("Tools/Koi Pond/Growth Prototype/Build Isolated Scene (new only)")]
        public static void Build()
        {
            if (File.Exists(ScenePath) || File.Exists(Root + "/Prefabs/Koi_GrowthPrototype.prefab"))
                throw new InvalidOperationException("Existing outputs retained. Build refuses overwrite.");
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("EditMode only.");
            foreach (var s in EditorSceneManager.GetSceneManagerSetup())
                if (SceneManager.GetSceneByPath(s.path).isDirty) throw new InvalidOperationException("Unsaved scene; do not discard user work.");
            foreach (string d in new[] { "Profiles", "Prefabs", "Scenes", "Reports/Captures" }) Directory.CreateDirectory(Root + "/" + d);
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var profile = ScriptableObject.CreateInstance<KoiGrowthProfile>();
            AssetDatabase.CreateAsset(profile, Root + "/Profiles/Growth_Diagnostic.asset");
            var individual = new GameObject("Persistent_Koi_Growth");
            Undo.RegisterCreatedObjectUndo(individual, "Create growth individual");
            var growth = individual.AddComponent<KoiGrowthController>();
            var visuals = individual.AddComponent<KoiStageVisuals>();
            var adapter = individual.AddComponent<KoiGrowthAnimationAdapter>();
            var containers = new Transform[3]; var rigs = new GameObject[3]; var skins = new SkinnedMeshRenderer[3]; var lengths = new float[3];
            for (int s = 0; s < 3; s++)
            {
                containers[s] = new GameObject(((KoiGrowthStage)s) + "_GrowthContainer").transform;
                Undo.RegisterCreatedObjectUndo(containers[s].gameObject, "Create stage container");
                containers[s].SetParent(individual.transform, false);
                rigs[s] = CopyVisual(s, containers[s], out skins[s]);
                lengths[s] = RestLength(skins[s], individual.transform);
            }
            visuals.Configure(growth, containers, rigs, skins, lengths);
            adapter.Configure(visuals, SourceClips());
            Complete(scene, individual, profile);
        }
        public static void ContinueCheckpoint()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath || File.Exists(Root + "/Prefabs/Koi_GrowthPrototype.prefab"))
                throw new InvalidOperationException("Only the saved incomplete growth scene may be resumed.");
            var roots = scene.GetRootGameObjects();
            if (roots.Length != 1 || roots[0].GetComponent<KoiStageVisuals>() == null)
                throw new InvalidOperationException("Unexpected checkpoint; preserve and inspect rather than overwrite.");
            roots[0].GetComponent<KoiStageVisuals>().Validate();
            Complete(scene, roots[0], AssetDatabase.LoadAssetAtPath<KoiGrowthProfile>(Root + "/Profiles/Growth_Diagnostic.asset"));
        }
        private static void Complete(Scene scene, GameObject individual, KoiGrowthProfile profile)
        {
            var growth = individual.GetComponent<KoiGrowthController>();
            var visuals = individual.GetComponent<KoiStageVisuals>();
            var lengths = visuals.RestLengths;
            // Birth once at authoring; never regenerated during Play or stage transitions.
var birth = KoiBreedingService.Breed(KoiGeneticsRules.CreateFounder(KoiFounderVariety.TaishoSanke), "growth-parent-sanke",
                KoiGeneticsRules.CreateFounder(KoiFounderVariety.Kohaku), "growth-parent-kohaku", 4004);
            growth.Initialize(birth, profile);
            visuals.Apply();
            PrefabUtility.SaveAsPrefabAsset(individual, Root + "/Prefabs/Koi_GrowthPrototype.prefab");
            for (int s = 0; s < 3; s++)
            {
                var reference = new GameObject("Static_" + (KoiGrowthStage)s + "_" + new[] { "30pct", "60pct", "100pct" }[s]);
                Undo.RegisterCreatedObjectUndo(reference, "Create static stage reference");
                reference.transform.position = new Vector3((s - 1) * .8f, 0, 1);
                var rig = CopyVisual(s, reference.transform, out var referenceSkin);
                rig.GetComponent<Animator>().enabled = false;
            }
            var cameraGo = new GameObject("GrowthComparisonCamera"); Undo.RegisterCreatedObjectUndo(cameraGo, "Create camera");
            var camera = cameraGo.AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.orthographic = true; camera.orthographicSize = 1.05f; camera.nearClipPlane = .01f; camera.farClipPlane = 20;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.12f, .15f, .19f);
            camera.transform.position = new Vector3(1.3f, 2.5f, -2.5f); camera.transform.LookAt(new Vector3(0, 0, .55f));
            var lightGo = new GameObject("NeutralGrowthLight"); Undo.RegisterCreatedObjectUndo(lightGo, "Create neutral light");
            var light = lightGo.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.4f;
            light.transform.rotation = Quaternion.Euler(45, -30, 0);
            File.WriteAllText(Root + "/Reports/SceneAssembly.txt", "Rest lengths Fry/Juvenile/Adult: " + string.Join(", ", lengths.Select(x => x.ToString("R")))
                + "\nBirth ID: " + birth.Id + "\nStatic row left/right: Fry, Juvenile, Adult. Growing individual in front.\nNo source scripts/colliders/controllers instantiated.\n");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = individual;
        }
    }
}
