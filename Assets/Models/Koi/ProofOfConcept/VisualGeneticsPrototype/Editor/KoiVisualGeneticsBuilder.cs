using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using KoiPond.GrowthPrototype;

namespace KoiPond.VisualGeneticsPrototype
{
    public static class KoiVisualGeneticsBuilder
    {
        public static void Build()
        {
            const string root = KoiAppearanceLookupTests.Root;
            var scene = SceneManager.GetActiveScene();
            if (scene.path != root + "/Scenes/Koi_VisualGenetics_Test.unity" || scene.isDirty)
                throw new InvalidOperationException("Open the clean isolated scene copy; never build in a production scene.");
            var roots = scene.GetRootGameObjects();
            var template = roots.Single(r => r.name == "Persistent_Koi_Growth");
            if (template.activeSelf) throw new InvalidOperationException("Template must remain inactive during configuration.");
            foreach (var c in template.GetComponentsInChildren<Component>(true))
                if (!(c is Transform) && !(c is SkinnedMeshRenderer) && !(c is Animator)
                    && !(c is KoiGrowthController) && !(c is KoiStageVisuals) && !(c is KoiGrowthAnimationAdapter))
                    throw new InvalidOperationException("Unexpected template component: " + c.GetType().FullName);
            var catalog = AssetDatabase.LoadAssetAtPath<KoiAppearanceLookup>(root + "/Profiles/ExistingAppearanceCatalog.asset");
            catalog.Validate();
            var authored = template.GetComponent<KoiStageVisuals>().Skins.Select(s => s.sharedMaterial).ToArray();
            var lines = new System.Text.StringBuilder();
            for (int i = 0; i < 13; i++)
            {
                var go = UnityEngine.Object.Instantiate(template);
                Undo.RegisterCreatedObjectUndo(go, "Create diagnostic koi");
                KoiGenome genome;
                string label;
                double age = 300;
                if (i < 8) { genome = KoiGeneticsRules.CreateFounder((KoiFounderVariety)i); label = ((KoiFounderVariety)i).ToString(); }
                else if (i == 8) { genome = KoiAppearanceLookupTests.Carrier(KoiGeneticsRules.CreateFounder(KoiFounderVariety.Kohaku)); label = "Carrier_Kohaku"; }
                else if (i == 9) { genome = KoiAppearanceLookupTests.Hybrid(); label = "Unsupported_Hybrid"; }
                else if (i == 10) { genome = KoiGeneticsRules.CreateFounder(KoiFounderVariety.Kohaku, KoiFinType.Hirenaga); label = "Unsupported_Hirenaga"; }
                else if (i == 11) { genome = KoiGeneticsRules.CreateFounder(KoiFounderVariety.Kohaku); label = "Fry_Placeholder"; age = 0; }
                else { genome = KoiGeneticsRules.CreateFounder(KoiFounderVariety.Tancho); label = "Juvenile_Placeholder"; age = 120; }
                go.name = "VisualGenetics_" + i.ToString("00") + "_" + label;
                go.transform.position = new Vector3((i % 4 - 1.5f) * 1.15f, 0, (1.5f - i / 4) * 1.15f);
                go.transform.rotation = Quaternion.identity;
                var growth = go.GetComponent<KoiGrowthController>();
                var old = growth.Individual.Growth;
                var suppliedBirth = KoiAppearanceLookupTests.Birth(genome, "visual-diagnostic-" + i.ToString("00"));
                var so = new SerializedObject(growth);
                so.FindProperty("_individual").managedReferenceValue = new KoiIndividualState(suppliedBirth, new KoiGrowthState(old.FryDuration, old.JuvenileDuration, age));
                so.FindProperty("_paused").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
                var visuals = go.GetComponent<KoiStageVisuals>();
                var binder = go.AddComponent<KoiGeneticAppearanceBinder>();
                binder.Configure(visuals, catalog, authored);
                go.SetActive(true);
                visuals.Apply();
                binder.RefreshCurrent();
                lines.AppendLine(go.name + " | ID=" + suppliedBirth.Id + " | position=" + go.transform.position + " | stage=" + growth.Evaluation.Stage + " | preset=" + binder.Selection.Profile + " | coverage=" + binder.Selection.Limitations);
            }
            // Only disposable objects inside this new scene copy are removed.
            foreach (var r in roots.Where(r => r.name.StartsWith("Audit_") || r.name.StartsWith("Static_")))
                Undo.DestroyObjectImmediate(r);
            Undo.RecordObject(template, "Preserve disabled template");
            template.name = "SourceTemplate_Disabled";
            var cam = roots.Single(r => r != null && r.GetComponent<Camera>() != null).GetComponent<Camera>();
Undo.RecordObject(cam, "Frame diagnostic koi");
            Undo.RecordObject(cam.transform, "Frame diagnostic koi");
            cam.orthographic = true; cam.orthographicSize = 2.7f;
            cam.transform.position = new Vector3(0, 7, 0); cam.transform.rotation = Quaternion.Euler(90, 0, 0);
            cam.name = "VisualGeneticsComparisonCamera";
            File.WriteAllText(root + "/Reports/SceneLayout.txt", "Rows run top-to-bottom in Game View; columns left-to-right.\n" + lines);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
    }
}
