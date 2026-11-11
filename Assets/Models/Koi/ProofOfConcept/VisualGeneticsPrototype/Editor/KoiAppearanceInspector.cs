using UnityEditor;
using UnityEngine;

namespace KoiPond.VisualGeneticsPrototype
{
    [CustomEditor(typeof(KoiGeneticAppearanceBinder))]
    public sealed class KoiAppearanceInspector : UnityEditor.Editor
    {
public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var binder = (KoiGeneticAppearanceBinder)target;
            var visuals = binder.Visuals;
            if (visuals == null || !visuals.Growth.Initialized) return;
            var birth = visuals.Growth.Individual.Birth;
            var traits = KoiGeneticsRules.Resolve(birth.Genome);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Individual", birth.Id);
            EditorGUILayout.LabelField("Stage", visuals.Growth.Evaluation.Stage.ToString());
            EditorGUILayout.LabelField("Base / Pattern", traits.BasePigmentation + " / " + traits.Pattern);
            EditorGUILayout.LabelField("Red / Black / Metallic / Fin", traits.HasRed + " / " + traits.HasBlack + " / " + traits.IsMetallic + " / " + traits.Fin);
            EditorGUILayout.LabelField("Preset", binder.HasBinding ? binder.Selection.Profile : "Waiting for stage binding");
            EditorGUILayout.LabelField("Coverage", binder.HasBinding ? binder.Selection.Limitations.ToString() : "Not bound");
            if (binder.HasBinding && binder.Selection.Limitations != KoiAppearanceLimitation.None)
                EditorGUILayout.HelpBox("Authored placeholders are not genetic expression. Hirenaga and unmatched phenotypes are not substituted. AuthoredMetallicOnly means the existing platinum/gold preset is selected, but metallic reflectance is not certified.", MessageType.Warning);
            if (Application.isPlaying && GUILayout.Button("Reapply current appearance")) binder.RefreshCurrent();
            if (Application.isPlaying) Repaint();
        }
    }
}
