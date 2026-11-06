using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace KoiPond.EditorTests
{
    /// <summary>
    /// Self-contained EditMode tests, runnable from Tools/Koi Pond/Tests/Run Breeding EditMode Tests.
    /// No scene access, package installation, or assembly restructuring is required.
    /// Throws on the first failure; RunAll also supports automated Editor invocation.
    /// </summary>
    public static class KoiBreedingEditModeTests
    {
        private static int _assertions;
        private static int _tests;

        [MenuItem("Tools/Koi Pond/Tests/Run Breeding EditMode Tests")]
        public static void RunFromMenu()
        {
            Debug.Log(RunAll());
        }

        public static string RunAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Run these tests in EditMode.");
            _assertions = 0;
            _tests = 0;
            Run(AllFounderCrossesInheritAndPreserveParents);
            Run(HiddenAllelesCanBeInherited);
            Run(FinTypeIsIndependentOfColorAndPattern);
            Run(MixedVarietiesProduceNonStarterTraits);
            Run(SeedsReproduceGenomesAndPermitVariation);
            Run(GameplayRandomIsIndependent);
            Run(UniqueIdsAndParentLineage);
            Run(OffspringSerializationPreservesIdentityAndGenome);
            Run(MutationConfigurationIsDisabledAndGuarded);
            Run(InvalidParentsAreRejected);
            return "PASS: " + _tests + " breeding EditMode tests; " + _assertions + " assertions.";
        }

        private static void Run(Action test)
        {
            test();
            _tests++;
            Debug.Log("[KoiBreedingTests] PASS " + test.Method.Name);
        }

        private static void Check(bool condition, string message)
        {
            _assertions++;
            if (!condition) throw new InvalidOperationException("Breeding test failed: " + message);
        }

        private static void Reject<T>(Action action) where T : Exception
        {
            bool rejected = false;
            try { action(); }
            catch (T) { rejected = true; }
            Check(rejected, "Expected " + typeof(T).Name);
        }

        private static KoiAllelePair[] Pairs(KoiGenome genome)
        {
            return new[] { genome.BasePigmentation, genome.Red, genome.Black,
                genome.Pattern, genome.Metallic, genome.Fin };
        }

        private static void CheckInheritance(KoiGenome first, KoiGenome second, KoiGenome child)
        {
            var a = Pairs(first);
            var b = Pairs(second);
            var c = Pairs(child);
            Check(!ReferenceEquals(first, child) && !ReferenceEquals(second, child), "Offspring aliases a parent");
            for (int i = 0; i < c.Length; i++)
            {
                Check(c[i].First == a[i].First || c[i].First == a[i].Second,
                    "First-parent allele provenance at locus " + i);
                Check(c[i].Second == b[i].First || c[i].Second == b[i].Second,
                    "Second-parent allele provenance at locus " + i);
            }
            KoiGeneticsRules.Validate(child);
        }

        private static KoiGenome HeterozygousParent()
        {
            return new KoiGenome(
                new KoiAllelePair(KoiAllele.WhiteBase, KoiAllele.BrownBase),
                new KoiAllelePair(KoiAllele.NoRed, KoiAllele.Red),
                new KoiAllelePair(KoiAllele.NoBlack, KoiAllele.Black),
                new KoiAllelePair(KoiAllele.HeadSpotPattern, KoiAllele.DistributedPattern),
                new KoiAllelePair(KoiAllele.NonMetallic, KoiAllele.Metallic),
                new KoiAllelePair(KoiAllele.StandardFin, KoiAllele.HirenagaFin));
        }

        private static void AllFounderCrossesInheritAndPreserveParents()
        {
            for (int a = 0; a < 8; a++)
            for (int b = 0; b < 8; b++)
            for (int fa = 0; fa < 2; fa++)
            for (int fb = 0; fb < 2; fb++)
            {
                var first = KoiGeneticsRules.CreateFounder((KoiFounderVariety)a, (KoiFinType)fa);
                var second = KoiGeneticsRules.CreateFounder((KoiFounderVariety)b, (KoiFinType)fb);
                string firstBefore = JsonUtility.ToJson(first);
                string secondBefore = JsonUtility.ToJson(second);
                for (int seed = 0; seed < 4; seed++)
                {
                    var child = KoiBreedingService.Breed(first, "parent-a", second, "parent-b", seed);
                    CheckInheritance(first, second, child.Genome);
                    var fin = KoiGeneticsRules.Resolve(child.Genome).Fin;
                    Check(fin == (fa == 0 && fb == 0 ? KoiFinType.Standard : KoiFinType.Hirenaga),
                        "Founder fin expression");
                }
                Check(firstBefore == JsonUtility.ToJson(first), "First parent was modified");
                Check(secondBefore == JsonUtility.ToJson(second), "Second parent was modified");
            }
        }

        private static void HiddenAllelesCanBeInherited()
        {
            var first = HeterozygousParent();
            var second = KoiGeneticsRules.CreateFounder(KoiFounderVariety.Tancho);
            var originals = Pairs(first);
            var firstSeen = new bool[originals.Length];
            var secondSeen = new bool[originals.Length];
            string before = JsonUtility.ToJson(first);
            for (int seed = 0; seed < 128; seed++)
            {
                var child = KoiBreedingService.Breed(first, "carrier", second, "tancho", seed);
                CheckInheritance(first, second, child.Genome);
                var pairs = Pairs(child.Genome);
                for (int i = 0; i < pairs.Length; i++)
                {
                    firstSeen[i] |= pairs[i].First == originals[i].First;
                    secondSeen[i] |= pairs[i].First == originals[i].Second;
                }
            }
            for (int i = 0; i < originals.Length; i++)
                Check(firstSeen[i] && secondSeen[i], "Both carrier alleles should be selectable at locus " + i);
            Check(before == JsonUtility.ToJson(first), "Carrier parent was modified");
        }

        private static bool SameColorAndPattern(KoiTraits a, KoiTraits b)
        {
            return a.BasePigmentation == b.BasePigmentation && a.HasRed == b.HasRed
                && a.HasBlack == b.HasBlack && a.Pattern == b.Pattern && a.IsMetallic == b.IsMetallic;
        }

        private static void FinTypeIsIndependentOfColorAndPattern()
        {
            var carrier = HeterozygousParent();
            var standardFin = new KoiAllelePair(KoiAllele.StandardFin, KoiAllele.StandardFin);
            var standard = new KoiGenome(carrier.BasePigmentation, carrier.Red, carrier.Black,
                carrier.Pattern, carrier.Metallic, standardFin);
            var mate = KoiGeneticsRules.CreateFounder(KoiFounderVariety.Kohaku);
            bool sawStandard = false;
            bool sawHirenaga = false;
            for (int seed = 0; seed < 128; seed++)
            {
                var c = KoiBreedingService.Breed(carrier, "carrier", mate, "mate", seed);
                var s = KoiBreedingService.Breed(standard, "standard", mate, "mate", seed);
                var ct = KoiGeneticsRules.Resolve(c.Genome);
                var st = KoiGeneticsRules.Resolve(s.Genome);
                Check(SameColorAndPattern(ct, st), "Fin genotype affected color/pattern inheritance");
                sawStandard |= ct.Fin == KoiFinType.Standard;
                sawHirenaga |= ct.Fin == KoiFinType.Hirenaga;
            }
            Check(sawStandard && sawHirenaga, "Carrier cross did not produce both fin types");
        }

        private static void MixedVarietiesProduceNonStarterTraits()
        {
            var gold = KoiGeneticsRules.CreateFounder(KoiFounderVariety.YamabukiOgon);
            var kohaku = KoiGeneticsRules.CreateFounder(KoiFounderVariety.Kohaku);
            var child = KoiBreedingService.Breed(gold, "gold", kohaku, "kohaku", 73);
            var traits = KoiGeneticsRules.Resolve(child.Genome);
            Check(traits.BasePigmentation == KoiBasePigmentation.Yellow && traits.HasRed
                && !traits.HasBlack && traits.Pattern == KoiPatternType.Distributed && traits.IsMetallic,
                "Hybrid traits were discarded or forced into a starter profile");
            for (int i = 0; i < 8; i++)
                Check(!SameColorAndPattern(traits,
                    KoiGeneticsRules.Resolve(KoiGeneticsRules.CreateFounder((KoiFounderVariety)i))),
                    "Hybrid should differ from starter " + (KoiFounderVariety)i);
        }

        private static void SeedsReproduceGenomesAndPermitVariation()
        {
            var first = HeterozygousParent();
            var second = HeterozygousParent();
            var genomes = new HashSet<string>();
            var seeds = new[] { int.MinValue, -1, 0, 1, 17, 2026, int.MaxValue };
            foreach (int seed in seeds)
            {
                var a = KoiBreedingService.Breed(first, "a", second, "b", seed);
                var b = KoiBreedingService.Breed(first, "a", second, "b", seed);
                Check(JsonUtility.ToJson(a.Genome) == JsonUtility.ToJson(b.Genome), "Same seed gave different genomes");
                Check(a.Id != b.Id, "Replay reused individual identity");
                genomes.Add(JsonUtility.ToJson(a.Genome));
            }
            Check(genomes.Count > 1, "Different seeds never changed a heterozygous cross");
        }

        private static void GameplayRandomIsIndependent()
        {
            var originalState = UnityEngine.Random.state;
            try
            {
                var first = HeterozygousParent();
                var second = HeterozygousParent();
                UnityEngine.Random.InitState(1234);
                float expectedNext = UnityEngine.Random.value;
                UnityEngine.Random.InitState(1234);
                var a = KoiBreedingService.Breed(first, "a", second, "b", 88);
                Check(UnityEngine.Random.value == expectedNext, "Breeding consumed gameplay randomness");
                UnityEngine.Random.InitState(9999);
                for (int i = 0; i < 20; i++) { float unused = UnityEngine.Random.value; }
                var b = KoiBreedingService.Breed(first, "a", second, "b", 88);
                Check(JsonUtility.ToJson(a.Genome) == JsonUtility.ToJson(b.Genome), "Gameplay randomness changed breeding");
            }
            finally { UnityEngine.Random.state = originalState; }
        }

        private static void UniqueIdsAndParentLineage()
        {
            var first = KoiGeneticsRules.CreateFounder(KoiFounderVariety.TaishoSanke);
            var second = KoiGeneticsRules.CreateFounder(KoiFounderVariety.ShowaSanshoku);
            var ids = new HashSet<string>();
            for (int i = 0; i < 1024; i++)
            {
                var child = KoiBreedingService.Breed(first, "taisho-id", second, "showa-id", 7);
                Check(Guid.TryParseExact(child.Id, "N", out var parsed), "Invalid offspring ID");
                Check(ids.Add(child.Id), "Duplicate offspring ID");
                Check(child.Id != "taisho-id" && child.Id != "showa-id", "Child identity equals parent identity");
                Check(child.FirstParentId == "taisho-id" && child.SecondParentId == "showa-id"
                    && child.BreedingSeed == 7, "Lineage or seed was not recorded");
            }
        }

        private static void OffspringSerializationPreservesIdentityAndGenome()
        {
            var a = HeterozygousParent();
            var b = KoiGeneticsRules.CreateFounder(KoiFounderVariety.Purachina);
            var child = KoiBreedingService.Breed(a, "a", b, "b", 317);
            string json = JsonUtility.ToJson(child);
            var restored = JsonUtility.FromJson<KoiOffspringRecord>(json);
            Check(JsonUtility.ToJson(restored) == json, "Offspring record serialization lost fields");
            Check(restored.Id == child.Id && restored.FirstParentId == "a"
                && restored.SecondParentId == "b" && restored.BreedingSeed == 317,
                "Serialized identity or lineage changed");
            CheckInheritance(a, b, restored.Genome);
        }

        private static void MutationConfigurationIsDisabledAndGuarded()
        {
            var a = HeterozygousParent();
            var b = HeterozygousParent();
            var settings = new KoiMutationSettings();
            Check(!settings.Enabled && settings.ProbabilityPerAllele == 0f, "Mutation defaults are not disabled");
            var baseline = KoiBreedingService.Breed(a, "a", b, "b", 26);
            var configured = KoiBreedingService.Breed(a, "a", b, "b", 26, new KoiMutationSettings(false, 1f));
            Check(JsonUtility.ToJson(baseline.Genome) == JsonUtility.ToJson(configured.Genome),
                "Disabled mutation configuration affected inheritance");
            Reject<NotSupportedException>(() => KoiBreedingService.Breed(a, "a", b, "b", 26,
                new KoiMutationSettings(true, 0.01f)));
            Reject<ArgumentOutOfRangeException>(() => new KoiMutationSettings(false, -0.1f));
            Reject<ArgumentOutOfRangeException>(() => new KoiMutationSettings(false, 1.1f));
            Reject<ArgumentOutOfRangeException>(() => new KoiMutationSettings(false, float.NaN));
            Reject<ArgumentOutOfRangeException>(() => new KoiMutationSettings(false, float.PositiveInfinity));
            var corrupt = JsonUtility.FromJson<KoiMutationSettings>("{\"enabled\":false,\"probabilityPerAllele\":2}");
            Reject<ArgumentOutOfRangeException>(() => KoiBreedingService.Breed(a, "a", b, "b", 26, corrupt));
        }

        private static void InvalidParentsAreRejected()
        {
            var a = HeterozygousParent();
            Reject<ArgumentException>(() => KoiBreedingService.Breed(a, "same", a, "same", 0));
            Reject<ArgumentException>(() => KoiBreedingService.Breed(a, " ", a, "b", 0));
            Reject<ArgumentException>(() => KoiBreedingService.Breed(a, "a", a, null, 0));
            Reject<ArgumentNullException>(() => KoiBreedingService.Breed(null, "a", a, "b", 0));
            Reject<ArgumentNullException>(() => KoiBreedingService.Breed(a, "a", null, "b", 0));
            var bad = new KoiGenome(new KoiAllelePair(KoiAllele.Red, KoiAllele.WhiteBase),
                a.Red, a.Black, a.Pattern, a.Metallic, a.Fin);
            Reject<ArgumentException>(() => KoiBreedingService.Breed(bad, "a", a, "b", 0));
            string json = JsonUtility.ToJson(a).Replace("\"schemaVersion\":1", "\"schemaVersion\":999");
            var unsupported = JsonUtility.FromJson<KoiGenome>(json);
            Reject<ArgumentException>(() => KoiBreedingService.Breed(a, "a", unsupported, "b", 0));
        }
    }
}
