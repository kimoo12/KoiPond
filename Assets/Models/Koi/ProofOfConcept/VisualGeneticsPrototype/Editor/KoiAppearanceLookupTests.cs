using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using KoiPond.GrowthPrototype;

namespace KoiPond.VisualGeneticsPrototype
{
    public static class KoiAppearanceLookupTests
    {
        public const string Root = "Assets/Models/Koi/ProofOfConcept/VisualGeneticsPrototype";
        private static int _assertions;
        [Serializable] private sealed class RecordFixture
        {
            public string id;
            public string firstParentId = "diagnostic-parent-A";
            public string secondParentId = "diagnostic-parent-B";
            public int breedingSeed = 42;
            public KoiGenome genome;
        }
        public static KoiOffspringRecord Birth(KoiGenome genome, string id) =>
            JsonUtility.FromJson<KoiOffspringRecord>(JsonUtility.ToJson(new RecordFixture { id = id, genome = genome }));
        private static KoiAllelePair Pair(KoiAllele a, KoiAllele b) => new KoiAllelePair(a, b);
        public static KoiGenome Carrier(KoiGenome g) => new KoiGenome(
            Pair(g.BasePigmentation.First, KoiAllele.WhiteBase),
            Pair(g.Red.First, KoiAllele.NoRed), Pair(g.Black.First, KoiAllele.NoBlack),
            Pair(g.Pattern.First, KoiAllele.SolidPattern), Pair(g.Metallic.First, KoiAllele.NonMetallic), g.Fin);
        public static KoiGenome Hybrid() => new KoiGenome(Pair(KoiAllele.YellowBase, KoiAllele.WhiteBase),
            Pair(KoiAllele.Red, KoiAllele.NoRed), Pair(KoiAllele.Black, KoiAllele.NoBlack),
            Pair(KoiAllele.DistributedPattern, KoiAllele.SolidPattern),
            Pair(KoiAllele.NonMetallic, KoiAllele.NonMetallic), Pair(KoiAllele.StandardFin, KoiAllele.StandardFin));
        private static void Check(bool value, string label)
        {
            _assertions++;
            if (!value) throw new InvalidOperationException("Appearance assertion failed: " + label);
        }
        [MenuItem("Koi/Visual Genetics Prototype/Run EditMode Lookup Tests")]
        public static void MenuTests()
        {
            string report = RunLookup();
            File.WriteAllText(Root + "/Reports/EditModeResults.txt", report);
            Debug.Log(report);
        }
        public static string RunLookup()
        {
            _assertions = 0;
            var catalog = AssetDatabase.LoadAssetAtPath<KoiAppearanceLookup>(Root + "/Profiles/ExistingAppearanceCatalog.asset");
            catalog.Validate();
            var rng = UnityEngine.Random.state;
            for (int i = 0; i < 8; i++)
            {
                var genome = KoiGeneticsRules.CreateFounder((KoiFounderVariety)i);
                var before = JsonUtility.ToJson(genome);
                var traits = KoiGeneticsRules.Resolve(genome);
                var adult = catalog.Select(traits, KoiGrowthStage.Adult, catalog.CompatibleAdultMesh);
                Check(adult.Material == catalog.GetEntry(i).Material, "exact founder " + i);
                Check(adult.Profile == ((KoiFounderVariety)i).ToString(), "profile label " + i);
                Check(adult.Limitations == (traits.IsMetallic ? KoiAppearanceLimitation.AuthoredMetallicOnly : KoiAppearanceLimitation.None), "metallic disclosure");
                Check(catalog.Select(KoiGeneticsRules.Resolve(Carrier(genome)), KoiGrowthStage.Adult, catalog.CompatibleAdultMesh).Material == adult.Material, "carrier expression " + i);
                foreach (var stage in new[] { KoiGrowthStage.Fry, KoiGrowthStage.Juvenile })
                    Check(!catalog.Select(traits, stage, catalog.CompatibleAdultMesh).HasSupportedPreset, "early stage " + i);
                var h = KoiGeneticsRules.Resolve(KoiGeneticsRules.CreateFounder((KoiFounderVariety)i, KoiFinType.Hirenaga));
                foreach (KoiGrowthStage stage in Enum.GetValues(typeof(KoiGrowthStage)))
                {
                    var result = catalog.Select(h, stage, catalog.CompatibleAdultMesh);
                    Check(!result.HasSupportedPreset && (result.Limitations & KoiAppearanceLimitation.HirenagaGeometry) != 0, "Hirenaga " + i);
                }
                Check(!catalog.Select(traits, KoiGrowthStage.Adult, null).HasSupportedPreset, "incompatible mesh");
                Check(before == JsonUtility.ToJson(genome), "unchanged genome");
            }
            int supported = 0;
            foreach (KoiBasePigmentation b in Enum.GetValues(typeof(KoiBasePigmentation)))
            foreach (KoiPatternType p in Enum.GetValues(typeof(KoiPatternType)))
            foreach (KoiFinType f in Enum.GetValues(typeof(KoiFinType)))
            for (int flags = 0; flags < 8; flags++)
            {
                var t = new KoiTraits(b, (flags & 1) != 0, (flags & 2) != 0, p, (flags & 4) != 0, f);
                var r = catalog.Select(t, KoiGrowthStage.Adult, catalog.CompatibleAdultMesh);
                int exact = Enumerable.Range(0, catalog.Count).Count(i => KoiAppearanceLookup.SameTraits(t, catalog.GetEntry(i).Traits));
                Check(r.HasSupportedPreset == (exact == 1), "no closest-phenotype substitution");
                if (r.HasSupportedPreset) supported++;
            }
            Check(supported == 8, "only eight of 192 expressed signatures supported");
            var hybrid = catalog.Select(KoiGeneticsRules.Resolve(Hybrid()), KoiGrowthStage.Adult, catalog.CompatibleAdultMesh);
            Check(!hybrid.HasSupportedPreset && hybrid.Limitations == KoiAppearanceLimitation.UnsupportedPhenotype, "hybrid unsupported");
            bool rejected = false;
            try { KoiGeneticsRules.Resolve(new KoiGenome(default, default, default, default, default, default)); }
            catch (ArgumentException) { rejected = true; }
            Check(rejected, "invalid genome rejected");
            Check(JsonUtility.ToJson(rng) == JsonUtility.ToJson(UnityEngine.Random.state), "Unity RNG unchanged");
            return "PASS: " + _assertions + " lookup assertions; 8/192 adult signatures, all founders, equivalent carriers, hybrids, Hirenaga, early stages and invalid genomes.";
        }

        // Invoked via reflection by the isolated multi-frame runtime bootstrap.
        public static string RunRuntime()
        {
            RunLookup();
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            var binders = roots.Select(r => r.GetComponent<KoiGeneticAppearanceBinder>()).Where(b => b != null).ToArray();
            Check(binders.Length == 13, "13 independent diagnostic koi");
            var baseline = new Material[3];
            baseline[0] = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Koi/ProofOfConcept/FryRiggingPrototype/Materials/Fry_Prototype_MAT.mat");
            baseline[1] = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Koi/ProofOfConcept/JuvenileRiggingPrototype/Materials/Juvenile_Prototype_MAT.mat");
            baseline[2] = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Koi/Materials/Taisho_Sanke_Koi_MAT.mat");
            var materialJson = binders[0].Catalog.Count == 8 ? Enumerable.Range(0, 8).Select(i => EditorJsonUtility.ToJson(binders[0].Catalog.GetEntry(i).Material)).ToArray() : null;
            foreach (var binder in binders)
            {
                binder.Validate();
                var visuals = binder.Visuals;
                var growth = visuals.Growth;
                var birth = growth.Individual.Birth;
                var genome = birth.Genome;
                var before = JsonUtility.ToJson(birth);
                var initial = growth.Snapshot();
                growth.Paused = true;
                int bindings = 0;
                Action<KoiAppearanceSelection> observed = selection => bindings++;
                binder.Applied += observed;
                growth.ResetAge();
                Check(binder.HasBinding && binder.Selection.Limitations.HasFlag(KoiAppearanceLimitation.EarlyStage), "Fry placeholder");
                Check(visuals.Skins[0].sharedMaterial == baseline[0], "Fry preserved");
                growth.Advance(growth.Individual.Growth.FryDuration);
                Check(visuals.ActiveIndex == 1 && visuals.Skins[1].sharedMaterial == baseline[1], "Juvenile boundary preserved");
                growth.Advance(growth.Individual.Growth.JuvenileDuration);
                var expected = binder.Catalog.Select(KoiGeneticsRules.Resolve(genome), KoiGrowthStage.Adult, visuals.Skins[2].sharedMesh);
                Check(visuals.ActiveIndex == 2 && visuals.Skins[2].sharedMaterial == (expected.Material == null ? baseline[2] : expected.Material), "Adult exact/fallback " + birth.Id);
                Check(binder.Selection.Limitations == expected.Limitations, "explicit limitations");
                int count = bindings;
                growth.Advance(1);
                Check(bindings == count, "no per-tick rebind");
                binder.enabled = false;
                Check(visuals.Skins[2].sharedMaterial == baseline[2], "disable restores authored baseline");
                binder.enabled = true;
                Check(bindings == count + 1 && binder.HasBinding, "late enable uses current context exactly once");
                count = bindings;
                growth.ResetAge();
                growth.Advance(growth.Individual.Growth.AdultAge + 10);
                Check(bindings == count + 2, "large skip binds only final applicable stages");
                Check(visuals.Skins[2].sharedMaterial == (expected.Material == null ? baseline[2] : expected.Material), "skip material");
                growth.RestoreSnapshot(initial);
                Check(ReferenceEquals(birth, growth.Individual.Birth) && ReferenceEquals(genome, growth.Individual.Birth.Genome), "identity/genome references retained");
                Check(before == JsonUtility.ToJson(birth), "birth values/lineage retained");
                count = bindings;
                binder.gameObject.SetActive(false);
                binder.gameObject.SetActive(true);
                Check(bindings == count + 1 && binder.HasBinding, "whole-koi re-enable one binding");
                binder.Applied -= observed;
            }
            var adults = binders.Where(b => b.Visuals.ActiveIndex == 2 && b.Selection.HasSupportedPreset).ToArray();
            Check(adults.Select(b => b.Visuals.Skins[2].sharedMaterial).Distinct().Count() == 8, "multiple koi simultaneously show eight different materials");
            for (int i = 0; i < 8; i++)
                Check(EditorJsonUtility.ToJson(binders[0].Catalog.GetEntry(i).Material) == materialJson[i], "shared material not mutated " + i);
            return "PASS: " + _assertions + " total assertions; 13 independent koi, all eight Adult presets, stage boundaries/skips, carriers, hybrids, Hirenaga, reset/restore, late/whole-koi re-enable, identity and shared-material isolation.";
        }
    }
}
