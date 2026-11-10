using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace KoiPond.GrowthPrototype.EditorTools
{
    public static class KoiGrowthEditModeTests
    {
        private static int _assertions;
        private static void Check(bool value, string message)
        { _assertions++; if (!value) throw new InvalidOperationException("Growth test failed: " + message); }
        private static void Near(double value, double expected, double tolerance, string message)
        { Check(Math.Abs(value - expected) <= tolerance, message + ": " + value + " vs " + expected); }
        private static void Reject(Action action)
        { bool rejected = false; try { action(); } catch (ArgumentException) { rejected = true; } Check(rejected, "invalid data rejected"); }
        [MenuItem("Tools/Koi Pond/Growth Prototype/Run EditMode Tests")]
        public static string RunAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("EditMode only.");
            _assertions = 0;
            double[] ages = { 0, 60, 120, 210, 300 };
            float[] ratios = { .2f, .275f, .6f, .725f, 1 };
            int[] stages = { 0, 0, 1, 1, 2 };
            for (int i = 0; i < ages.Length; i++)
            {
                var e = KoiGrowthRules.Evaluate(new KoiGrowthState(120, 180, ages[i]));
                Check((int)e.Stage == stages[i], "stage boundary"); Near(e.SizeRatio, ratios[i], 1e-7, "size evaluation");
            }
            foreach (var edge in new[] { 120d, 300d })
            {
                var before = KoiGrowthRules.Evaluate(new KoiGrowthState(120, 180, edge - 1e-7));
                var after = KoiGrowthRules.Evaluate(new KoiGrowthState(120, 180, edge + 1e-7));
                Check((int)after.Stage == (int)before.Stage + 1, "epsilon threshold");
            }
            var small = new KoiGrowthState(120, 180); var large = new KoiGrowthState(120, 180);
            for (int i = 0; i < 21000; i++) KoiGrowthRules.Advance(small, .01);
            KoiGrowthRules.Advance(large, 210); Near(small.Age, large.Age, 1e-7, "split/large delta equivalence");
            var events = new List<string>();
            var skipped = new KoiGrowthState(120, 180);
            Action<KoiGrowthStage, KoiGrowthStage> handler = (a, b) => events.Add(a + ">" + b);
            KoiGrowthRules.Advance(skipped, double.MaxValue, handler); KoiGrowthRules.Advance(skipped, 2, handler);
            Check(string.Join(",", events) == "Fry>Juvenile,Juvenile>Adult", "ordered exactly-once crossings");
            Near(skipped.Age, 300, 0, "adult age saturation");
            double savedAge = small.Age; KoiGrowthRules.Advance(small, 0); Near(small.Age, savedAge, 0, "zero delta");
            Near(large.Age, 210, 0, "independent fish");
            foreach (double invalid in new[] { -1d, double.NaN, double.PositiveInfinity })
            {
                Reject(() => new KoiGrowthState(120, 180, invalid));
                Reject(() => KoiGrowthRules.Advance(large, invalid));
                Reject(() => new KoiGrowthState(invalid, 180));
            }
            Reject(() => new KoiGrowthState(0, 180)); Reject(() => new KoiGrowthState(120, 0));
            Reject(() => new KoiGrowthState(double.MaxValue, double.MaxValue));
            foreach (bool mutate in new[] { false, true })
            {
                var a = KoiGeneticsRules.CreateFounder(KoiFounderVariety.Tancho, KoiFinType.Hirenaga);
                var b = KoiGeneticsRules.CreateFounder(KoiFounderVariety.Chagoi);
                string parentA = JsonUtility.ToJson(a), parentB = JsonUtility.ToJson(b);
                var birth = KoiBreedingService.Breed(a, "growth-test-parent-a", b, "growth-test-parent-b", 42, new KoiMutationSettings(mutate, 1));
                string original = JsonUtility.ToJson(birth); var genome = birth.Genome;
                var individual = new KoiIndividualState(birth, new KoiGrowthState(120, 180));
                var traits = JsonUtility.ToJson(KoiGeneticsRules.Resolve(genome));
                foreach (double age in new[] { 60d, 120d, 210d, 300d })
                {
                    KoiGrowthRules.Advance(individual.Growth, age - individual.Growth.Age);
                    Check(ReferenceEquals(birth, individual.Birth) && ReferenceEquals(genome, individual.Birth.Genome), "runtime birth/genome references");
                    Check(JsonUtility.ToJson(birth) == original && JsonUtility.ToJson(KoiGeneticsRules.Resolve(genome)) == traits, "unchanged birth and traits");
                    var restored = KoiIndividualState.Restore(individual.Snapshot());
                    Check(JsonUtility.ToJson(restored.Birth) == original, "snapshot lineage/genome/ID");
                    Near(restored.Growth.Age, age, 0, "snapshot age");
                    Reject(() => KoiIndividualState.Restore(individual.Snapshot().Replace("\"_schema\":1", "\"_schema\":999")));
                }
                Check(JsonUtility.ToJson(a) == parentA && JsonUtility.ToJson(b) == parentB, "ancestor genomes preserved");
            }
            var prefab = PrefabUtility.LoadPrefabContents(KoiGrowthPrototypeBuilder.Root + "/Prefabs/Koi_GrowthPrototype.prefab");
            try
            {
                var growth = prefab.GetComponent<KoiGrowthController>(); var visuals = prefab.GetComponent<KoiStageVisuals>();
                var animation = prefab.GetComponent<KoiGrowthAnimationAdapter>();
                var birth = growth.Individual.Birth; var genome = birth.Genome; string birthJson = JsonUtility.ToJson(birth);
                var rootPosition = prefab.transform.localPosition; var rootRotation = prefab.transform.localRotation; var rootScale = prefab.transform.localScale;
                Check(prefab.GetComponentsInChildren<Collider>(true).Length == 0, "no production colliders");
                Check(prefab.GetComponentsInChildren<KoiFishController>(true).Length == 0, "no production controller");
                for (int s = 0; s < 3; s++)
                {
                    var source = AssetDatabase.LoadAssetAtPath<GameObject>(KoiGrowthPrototypeBuilder.StageSources[s]);
                    var sourceSkin = source.GetComponentInChildren<SkinnedMeshRenderer>();
                    Check(visuals.Skins[s].sharedMesh == sourceSkin.sharedMesh && visuals.Skins[s].sharedMaterials.SequenceEqual(sourceSkin.sharedMaterials), "exact source mesh/material references");
                    var sourcePaths = source.GetComponentsInChildren<Transform>(true).ToDictionary(t => AnimationUtility.CalculateTransformPath(t, source.transform));
                    foreach (var t in visuals.Rigs[s].GetComponentsInChildren<Transform>(true))
                    {
                        var original = sourcePaths[AnimationUtility.CalculateTransformPath(t, visuals.Rigs[s].transform)];
                        Check(t.localPosition == original.localPosition && t.localRotation == original.localRotation && t.localScale == original.localScale, "internal rig transforms untouched");
                    }
                    KoiGrowthPrototypeBuilder.ValidateBindings(visuals.Rigs[s], animation.Clips);
                }
                animation.Initialize(); animation.Subscribe(); visuals.Apply(); animation.Bind(visuals.ActiveIndex);
                float previous = 0;
                for (int i = 0; i <= 300; i++)
                {
                    growth.ResetAge(); growth.Advance(i); visuals.Apply();
                    animation.RestoreRest(visuals.ActiveIndex);
                    Vector3 size = KoiStageVisuals.MeasureBounds(visuals.Skins[visuals.ActiveIndex], prefab.transform).size;
                    int stage = visuals.ActiveIndex;
                    float expectedScale = visuals.RestLengths[2] * growth.Evaluation.SizeRatio / visuals.RestLengths[stage];
                    for (int axis = 0; axis < 3; axis++)
                    {
                        Near(visuals.Containers[stage].localScale[axis], expectedScale, 1e-6, "uniform container scale");
                        Near(size[axis] / visuals.RestSizes[stage][axis], expectedScale, .001, "preserved stage proportions");
                    }
                    float ratio = size.z / visuals.RestLengths[2];
                    Near(ratio, growth.Evaluation.SizeRatio, .001, "calibrated rendered length");
                    if (stage == 0) Check(ratio <= .350001f, "Fry length cap");
                    if (stage == 1) Check(ratio <= .850001f, "Juvenile length cap");
                    animation.EvaluatePose();
                    Check(ratio >= previous - 1e-6, "monotonic size"); previous = ratio;
                    Check(visuals.Containers.Count(t => t.gameObject.activeSelf) == 1, "one active cached stage");
                    Check(prefab.transform.localPosition == rootPosition && prefab.transform.localRotation == rootRotation && prefab.transform.localScale == rootScale, "persistent root transform");
                }
                for (int clip = 0; clip < 3; clip++)
                {
                    animation.SelectClip(clip);
                    foreach (double edge in new[] { 120d, 300d })
                    {
                        growth.ResetAge(); growth.Advance(edge - .01); visuals.Apply();
                        animation.AdvanceClock(.37); double clock = animation.Clock; double phase = animation.Phase;
                        growth.Advance(.02); visuals.Apply();
                        Near(animation.Clock, clock, 0, "transition clock"); Near(animation.Phase, phase, 1e-12, "transition phase");
                        Check(animation.BoundStage == visuals.ActiveIndex, "new animation stage bound");
                    }
                }
                foreach (int clip in new[] { 0, 1 })
                {
                    animation.SelectClip(clip); double startClock = animation.Clock;
                    animation.AdvanceClock(animation.Clips[clip].length * 3);
                    Near(animation.Clock - startClock, animation.Clips[clip].length * 3, 1e-6, "three logical loops");
                }
                animation.SelectClip(2); animation.AdvanceClock(animation.Clips[2].length + .2);
                Check(animation.ClipIndex != 2, "finite Eat returns to loop");
                growth.ResetAge(); growth.Advance(double.MaxValue); visuals.Apply();
                Check(visuals.ActiveIndex == 2, "large skip presents Adult only");
                Check(ReferenceEquals(birth, growth.Individual.Birth) && ReferenceEquals(genome, growth.Individual.Birth.Genome), "controller references preserved");
                string snapshot = growth.Snapshot(); growth.ResetAge(); growth.RestoreSnapshot(snapshot); visuals.Apply();
                Check(JsonUtility.ToJson(growth.Individual.Birth) == birthJson, "controller restore birth unchanged");
                Reject(() => growth.RestoreSnapshot(snapshot.Replace(birth.Id, "different-individual-id")));
                Near(growth.Evaluation.SizeRatio, 1, 0, "controller restore adult");
var stateBefore = JsonUtility.ToJson(UnityEngine.Random.state);
                growth.ResetAge(); growth.Advance(150); visuals.Apply();
                Check(JsonUtility.ToJson(UnityEngine.Random.state) == stateBefore, "growth does not consume Unity RNG");
                var profile = ScriptableObject.CreateInstance<KoiGrowthProfile>();
                var temporaryOwner = new GameObject("Owned_GrowthProfile_Test");
                temporaryOwner.transform.SetParent(prefab.transform, false);
                var profileOwner = temporaryOwner.AddComponent<KoiGrowthController>();
                try
                {
                    profileOwner.Initialize(birth, profile);
                    profileOwner.Advance(60);
                    var serialized = new SerializedObject(profile); serialized.FindProperty("_fryDurationSeconds").doubleValue = 1; serialized.ApplyModifiedPropertiesWithoutUndo();
                    Near(profileOwner.Individual.Growth.FryDuration, 120, 0, "initialized owner freezes its actual shared profile duration");
                    Near(profileOwner.Evaluation.SizeRatio, .275, 1e-7, "profile edit does not remap existing age or size");
                    profileOwner.ResetAge(true);
                    Near(profileOwner.Individual.Growth.FryDuration, 1, 0, "explicit reinitialization adopts profile timing");
                    Check(ReferenceEquals(profileOwner.Individual.Birth, birth), "timing reinitialization retains original birth reference");
                    serialized.FindProperty("_juvenileDurationSeconds").doubleValue = 0; serialized.ApplyModifiedPropertiesWithoutUndo();
                    Reject(() => profile.CreateState());
                }
                finally { UnityEngine.Object.DestroyImmediate(temporaryOwner); UnityEngine.Object.DestroyImmediate(profile); }
                foreach (double edge in new[] { 120d, 300d })
                {
                    growth.ResetAge(); growth.Advance(edge - 1e-5); visuals.Apply();
                    int outgoing = visuals.ActiveIndex;
                    animation.RestoreRest(outgoing);
                    float outgoingLength = KoiGrowthPrototypeBuilder.RestLength(visuals.Skins[outgoing], prefab.transform);
                    float outgoingRatio = outgoing == 0 ? .35f : .85f;
                    Near(outgoingLength / visuals.RestLengths[2], outgoingRatio, .001, "outgoing boundary length cap");
                    growth.Advance(1e-5); visuals.Apply();
                    int current = (int)growth.Evaluation.Stage;
                    animation.RestoreRest(current);
                    float incomingLength = KoiGrowthPrototypeBuilder.RestLength(visuals.Skins[current], prefab.transform);
                    Near(incomingLength / visuals.RestLengths[2], current == 1 ? .6 : 1, .001, "incoming boundary length");
                    Check(outgoingLength < incomingLength * .86f, "noticeably smaller outgoing stage");
                    animation.EvaluatePose();
                }
var inspector = UnityEditor.Editor.CreateEditor(growth);
                try { Check(inspector.CreateInspectorGUI() != null, "diagnostic inspector builds"); }
                finally { UnityEngine.Object.DestroyImmediate(inspector); }
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            string result = "PASS: " + _assertions + " growth EditMode assertions. Data/identity/snapshot/scaling/source-bindings/phase checks passed. Logical animation sampling is not continuous runtime certification.";
            File.WriteAllText(KoiGrowthPrototypeBuilder.Root + "/Reports/GrowthValidationResults.json", JsonUtility.ToJson(new Result { passed = true, assertions = _assertions, summary = result }, true));
            Debug.Log(result); return result;
        }
        [Serializable] private sealed class Result { public bool passed; public int assertions; public string summary; }
    }

    /// <summary>Called by the temporary editor bootstrap through reflection, only in the isolated scene.</summary>
    public static class KoiGrowthRuntimeVerification
    {
        private static KoiGrowthController _growth;
        private static KoiStageVisuals _visuals;
        private static KoiGrowthAnimationAdapter _animation;
        private static KoiOffspringRecord _birth;
        private static KoiGenome _genome;
        private static string _birthJson;
        private static int _phase, _lastFrame, _frames, _crossings;
        private static double _age, _clock;
        private static float _startTime;
        private static bool _swimFirst, _swimSecond, _eatFirst, _eatSecond;
        private static float _motion;
        private static Quaternion _finRest;
        private static readonly List<string> Evidence = new List<string>();
        private static void Check(bool condition, string message)
        {
            Evidence.Add(message + ": " + (condition ? "PASS" : "FAIL"));
            Debug.Log("[Test] " + Evidence[Evidence.Count - 1]);
            if (!condition) throw new InvalidOperationException(message);
        }
        private static void CheckGrowthSizes()
        {
            Evidence.Clear();
            string snapshot = _growth.Snapshot();
            bool paused = _growth.Paused;
            _growth.Paused = true;
            try
            {
                Check(_growth.Individual.Growth.FryDuration == 120 && _growth.Individual.Growth.JuvenileDuration == 180,
                    "unchanged diagnostic durations: 120/180 seconds");
                Check(Math.Abs(_animation.BlendDuration - .3f) < 1e-6, "unchanged animation blend: 0.3 seconds");
                double[] ages = { 0, 60, 119.999, 120, 210, 299.999, 300 };
                float[] expected = { .2f, .275f, .35f, .6f, .725f, .85f, 1 };
                int[] stages = { 0, 0, 0, 1, 1, 1, 2 };
                for (int i = 0; i < ages.Length; i++)
                {
                    _growth.ResetAge(); _growth.Advance(ages[i]);
                    int stage = _visuals.ActiveIndex;
                    _animation.RestoreRest(stage);
                    Vector3 size = KoiStageVisuals.MeasureBounds(_visuals.Skins[stage], _growth.transform).size;
                    Vector3 scale = _visuals.Containers[stage].localScale;
                    float ratio = size.z / _visuals.RestLengths[2];
                    bool proportions = Math.Abs(scale.x - scale.y) < 1e-6 && Math.Abs(scale.y - scale.z) < 1e-6;
                    for (int axis = 0; axis < 3; axis++)
                        proportions &= Math.Abs(size[axis] / _visuals.RestSizes[stage][axis] - scale.z) < .001;
                    Check(stage == stages[i] && proportions && Math.Abs(ratio - expected[i]) < .001,
                        "live age=" + ages[i] + " stage=" + stage + " length ratio=" + ratio + " scale=" + scale);
                    Check(_animation.GraphValid && _animation.BoundStage == stage,
                        "live size sample retains animation binding, stage=" + stage);
                    _animation.EvaluatePose();
                }
                Check(expected[2] < expected[3] * .86f && expected[5] < expected[6] * .86f,
                    "separate boundary lengths: Fry 35% < Juvenile 60%; Juvenile 85% < Adult 100%");
            }
            finally
            {
                _growth.RestoreSnapshot(snapshot);
                _growth.Paused = paused;
            }
        }
        public static void Setup()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != KoiGrowthPrototypeBuilder.ScenePath)
                throw new InvalidOperationException("Not the isolated growth scene.");
            var root = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()
                .Single(g => g.GetComponent<KoiGrowthController>() != null);
            _growth = root.GetComponent<KoiGrowthController>(); _visuals = root.GetComponent<KoiStageVisuals>();
            _animation = root.GetComponent<KoiGrowthAnimationAdapter>();
            CheckGrowthSizes();
            _birth = _growth.Individual.Birth; _genome = _birth.Genome; _birthJson = JsonUtility.ToJson(_birth);
            _growth.StageTransition += (a, b) => { _crossings++; Evidence.Add(a + ">" + b); };
            _phase = 0; _lastFrame = -1; _frames = 0; _crossings = 0; _motion = 0;
            _swimFirst = _swimSecond = _eatFirst = _eatSecond = false;
            _age = _growth.Individual.Growth.Age; _startTime = Time.time; _growth.Paused = false;
            _finRest = _visuals.Skins[0].bones[4].localRotation;
            Check(_animation.GraphValid && _animation.BoundStage == _visuals.ActiveIndex, "live source graph and stage binding");
        }
        public static bool Tick(float elapsed)
        {
            if (_lastFrame == Time.frameCount) return false;
            _lastFrame = Time.frameCount; _frames++;
            if (_phase == 0 && Time.time - _startTime >= .15f)
            {
                Check(_growth.Individual.Growth.Age > _age, "actual Update advances growth age");
                _growth.Paused = true; _age = _growth.Individual.Growth.Age; _clock = _animation.Clock;
                _phase = 1; _startTime = Time.time;
            }
            else if (_phase == 1 && Time.time - _startTime >= .15f)
            {
                Check(_growth.Individual.Growth.Age == _age && _animation.Clock > _clock, "growth pause preserves age while animation runs");
                Time.timeScale = 0; _clock = _animation.Clock; _phase = 2; _frames = 0;
            }
            else if (_phase == 2 && _frames >= 4)
            {
                Check(_growth.Individual.Growth.Age == _age && _animation.Clock == _clock, "global timeScale zero pauses age and animation");
                Time.timeScale = 1; _growth.enabled = false; _animation.enabled = false;
                Check(!_animation.GraphValid, "disable destroys animation graph"); _phase = 3; _frames = 0;
            }
            else if (_phase == 3 && _frames >= 3)
            {
                Check(_growth.Individual.Growth.Age == _age && _animation.Clock == _clock, "disabled components preserve age and logical clock");
                _animation.enabled = true; _growth.enabled = true;
                Check(_animation.GraphValid, "re-enable restores source graph");
                _animation.SelectClip(0); _growth.ResetAge(); _growth.Advance(119.95); _growth.Paused = false; _phase = 4;
            }
            else if (_phase == 4)
            {
                _motion = Mathf.Max(_motion, Quaternion.Angle(_finRest, _visuals.Skins[_visuals.ActiveIndex].bones[4].localRotation));
                if (!_swimFirst && _visuals.ActiveIndex == 1)
                {
                    _swimFirst = true; Check(_animation.ClipIndex == 0, "Fry→Juvenile during Swim");
                    _growth.ResetAge(); _growth.Advance(299.95);
                }
                if (_swimFirst && !_swimSecond && _visuals.ActiveIndex == 2)
                { _swimSecond = true; Check(_animation.ClipIndex == 0, "Juvenile→Adult during Swim"); }
                if (_animation.Clock >= _animation.Clips[0].length * 3)
                {
                    Check(_swimFirst && _swimSecond && _motion > .01f, "three real Swim loops and changing bone pose; motion degrees=" + _motion);
                    _growth.Paused = true; _animation.SelectClip(1); _phase = 5;
                }
            }
            else if (_phase == 5 && _animation.Clock >= _animation.Clips[1].length * 3)
            {
                Check(_animation.ClipIndex == 1, "three real Idle loops");
                _animation.SelectClip(2); _growth.ResetAge(); _growth.Advance(119.95); _growth.Paused = false; _phase = 6;
            }
            else if (_phase == 6)
            {
                if (!_eatFirst && _visuals.ActiveIndex == 1)
                {
                    _eatFirst = true; Check(_animation.ClipIndex == 2, "Fry→Juvenile during Eat");
                    _growth.ResetAge(); _growth.Advance(299.95);
                }
                if (_eatFirst && !_eatSecond && _visuals.ActiveIndex == 2)
                { _eatSecond = true; Check(_animation.ClipIndex == 2, "Juvenile→Adult during Eat"); }
                if (_animation.ClipIndex == 1)
                {
                    Check(_eatFirst && _eatSecond, "complete Eat and return to Idle");
                    _growth.Paused = true; _animation.SelectClip(0); _animation.SelectClip(2); _phase = 7;
                }
            }
            else if (_phase == 7 && _animation.ClipIndex == 0)
            {
                Check(true, "repeat complete Eat and return to Swim");
                var skin = _visuals.Skins[_visuals.ActiveIndex];
                Check(skin.GetBlendShapeWeight(skin.sharedMesh.GetBlendShapeIndex("MouthOpen")) < 35, "no stale Eat MouthOpen after return");
                var snapshot = _growth.Snapshot(); _growth.ResetAge(); _growth.Advance(600); _growth.RestoreSnapshot(snapshot);
                Check(ReferenceEquals(_birth, _growth.Individual.Birth) && ReferenceEquals(_genome, _growth.Individual.Birth.Genome)
                    && JsonUtility.ToJson(_birth) == _birthJson, "same runtime birth/genome references and all identity/lineage values");
                Check(_visuals.Containers.Count(t => t.gameObject.activeSelf) == 1, "one visible cached model");
                Debug.Log("[Test] actual frames=" + Time.frameCount + "; crossing notifications=" + _crossings + "; maximum fin motion=" + _motion);
                _phase = 8; return true;
            }
            return _phase == 8;
        }
        public static string GetResult()
        {
            Time.timeScale = 1;
            return JsonUtility.ToJson(new RuntimeResult { success = _phase == 8, evidence = Evidence.ToArray(), phase = _phase,
                crossingNotifications = _crossings, finMotionDegrees = _motion }, true);
        }
        [Serializable] private sealed class RuntimeResult
        { public bool success; public string[] evidence; public int phase; public int crossingNotifications; public float finMotionDegrees; }
    }
}
