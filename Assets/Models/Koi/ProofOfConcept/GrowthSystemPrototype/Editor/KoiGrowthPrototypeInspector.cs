using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace KoiPond.GrowthPrototype.EditorTools
{
    [CustomEditor(typeof(KoiGrowthController))]
    public sealed class KoiGrowthPrototypeInspector : UnityEditor.Editor
    {
private string _snapshot;
        public override VisualElement CreateInspectorGUI()
        {
            var growth = (KoiGrowthController)target;
            var root = new VisualElement();
            root.Add(new Label("Growth Prototype — Persistent Individual"));
            root.Add(new PropertyField(serializedObject.FindProperty("_profile"), "Diagnostic duration profile"));
            var status = new Label(); root.Add(status);
            var invariants = new Label(); root.Add(invariants);
            string birthBaseline = growth.Initialized ? JsonUtility.ToJson(growth.Individual.Birth) : "";
            var pause = new Toggle("Pause growth only") { value = growth.Paused };
            pause.RegisterValueChangedCallback(e => { Undo.RecordObject(growth, "Pause growth"); growth.Paused = e.newValue; }); root.Add(pause);
            var actions = new VisualElement(); root.Add(actions);
            Action<Action> execute = action => { try { action(); } catch (Exception ex) { Debug.LogException(ex); } };
            actions.Add(new Button(() => execute(() => growth.Advance(1))) { text = "Advance +1 second" });
            actions.Add(new Button(() => execute(() => growth.Advance(10))) { text = "Advance +10 seconds" });
            actions.Add(new Button(() => execute(() => growth.ResetAge())) { text = "Reset age — keep identity/timing" });
            actions.Add(new Button(() => execute(() => growth.ResetAge(true))) { text = "Reinitialize timing from profile — same identity" });
            actions.Add(new Button(() => execute(() => _snapshot = growth.Snapshot())) { text = "Snapshot" });
            actions.Add(new Button(() => execute(() => growth.RestoreSnapshot(_snapshot))) { text = "Restore snapshot" });
            for (int i = 0; i < 3; i++)
            {
                int clip = i;
                actions.Add(new Button(() => execute(() => growth.GetComponent<KoiGrowthAnimationAdapter>().SelectClip(clip)))
                    { text = new[] { "Swim", "Idle", "Eat → previous loop" }[i] });
            }
            root.Add(new HelpBox("Read-only source materials. No genetic appearance matching or Hirenaga visual variants. Existing rig/mouth limitations remain. Controls run only in this isolated Play Mode scene.", HelpBoxMessageType.Info));
            Action refresh = () =>
            {
                if (growth == null) return;
                actions.SetEnabled(Application.isPlaying && growth.Initialized);
                if (!growth.Initialized) { status.text = "Not initialized"; return; }
                var birth = growth.Individual.Birth; var e = growth.Evaluation; var age = growth.Individual.Growth;
                status.text = "ID: " + birth.Id + "\nParents: " + birth.FirstParentId + " / " + birth.SecondParentId
                    + "\nBreeding seed: " + birth.BreedingSeed + "\nStage: " + e.Stage + " | Age: " + age.Age.ToString("F2")
                    + " s\nProgress: " + (100 * e.Progress).ToString("F1") + "% | Adult size: " + (100 * e.SizeRatio).ToString("F1")
                    + "%\nFrozen durations: " + age.FryDuration + " / " + age.JuvenileDuration + " s";
                invariants.text = "Genome/identity/lineage unchanged: " + (birthBaseline == JsonUtility.ToJson(birth) ? "PASS" : "FAIL");
            };
            refresh(); root.schedule.Execute(refresh).Every(200);
            return root;
        }
    }
}
