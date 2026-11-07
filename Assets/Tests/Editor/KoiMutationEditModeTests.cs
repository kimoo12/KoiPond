using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace KoiPond.EditorTests
{
    /// <summary>
    /// Matches the existing self-contained EditMode test convention. No scene access,
    /// package installation, or assembly changes. RunAll throws on any failed assertion.
    /// </summary>
    public static class KoiMutationEditModeTests
    {
        private static int _assertions;
        private static int _tests;
        private static readonly KoiAllele[][] Domains =
        {
            new[] { KoiAllele.WhiteBase, KoiAllele.YellowBase, KoiAllele.BrownBase, KoiAllele.BlackBase },
            new[] { KoiAllele.NoRed, KoiAllele.Red },
            new[] { KoiAllele.NoBlack, KoiAllele.Black },
            new[] { KoiAllele.SolidPattern, KoiAllele.HeadSpotPattern, KoiAllele.DistributedPattern },
            new[] { KoiAllele.NonMetallic, KoiAllele.Metallic },
            new[] { KoiAllele.StandardFin, KoiAllele.HirenagaFin }
        };

        [MenuItem("Tools/Koi Pond/Tests/Run Mutation EditMode Tests")]
        public static void RunFromMenu() { Debug.Log(RunAll()); }

        public static string RunAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Run mutation tests in EditMode.");
            _assertions = 0;
            _tests = 0;
            Run(DisabledDefaultsAndZeroRatesPreserveInheritance);
            Run(EachTraitCanMutateIndependently);
            Run(ForcedMutationsAreValidAndActuallyChangeEveryAllele);
            Run(RareMutationProbabilitiesMatchConfiguredRates);
            Run(SeededReplayAndGameplayRandomIndependence);
            Run(ParentsAndAncestorGenomesArePreserved);
            Run(SettingsSerializationAndInvalidDataAreGuarded);
            return "PASS: " + _tests + " mutation EditMode tests; " + _assertions + " assertions.";
        }

        private static void Run(Action test)
        {
            test();
            _tests++;
            Debug.Log("[KoiMutationTests] PASS " + test.Method.Name);
        }

        private static void Check(bool condition, string message)
        {
            _assertions++;
            if (!condition) throw new InvalidOperationException("Mutation test failed: " + message);
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

        private static KoiGenome GenomeFromPairs(KoiAllelePair[] pairs)
        {
            return new KoiGenome(pairs[0], pairs[1], pairs[2], pairs[3], pairs[4], pairs[5]);
        }

        private static KoiGenome Carrier()
        {
            var pairs = new KoiAllelePair[6];
            for (int i = 0; i < pairs.Length; i++)
                pairs[i] = new KoiAllelePair(Domains[i][0], Domains[i][Domains[i].Length - 1]);
            return GenomeFromPairs(pairs);
        }

        private static KoiMutationSettings Rates(bool enabled, float[] rates)
        {
            return new KoiMutationSettings(enabled, rates[0], rates[1], rates[2],
                rates[3], rates[4], rates[5]);
        }

        private static KoiOffspringRecord Breed(KoiGenome a, KoiGenome b, int seed,
            KoiMutationSettings settings = null)
        {
            return KoiBreedingService.Breed(a, "parent-a", b, "parent-b", seed, settings);
        }

        private static bool EqualPair(KoiAllelePair a, KoiAllelePair b)
        {
            return a.First == b.First && a.Second == b.Second;
        }

        private static void DisabledDefaultsAndZeroRatesPreserveInheritance()
        {
            var a = Carrier();
            var b = KoiGeneticsRules.CreateFounder(KoiFounderVariety.Tancho);
            var defaults = new KoiMutationSettings();
            Check(!defaults.Enabled, "Mutations enabled by default");
            for (int i = 0; i < 6; i++)
                Check(defaults.GetProbability((KoiGeneticTrait)i) == 0f, "Nonzero default trait rate");
            var configs = new[] { defaults, new KoiMutationSettings(false, 1f),
                Rates(false, new[] { 1f, 1f, 1f, 1f, 1f, 1f }),
                new KoiMutationSettings(true, 0f), Rates(true, new float[6]) };
            for (int seed = 0; seed < 128; seed++)
            {
                string baseline = JsonUtility.ToJson(Breed(a, b, seed).Genome);
                foreach (var config in configs)
                    Check(baseline == JsonUtility.ToJson(Breed(a, b, seed, config).Genome),
                        "Disabled or zero-rate settings changed seeded inheritance");
            }
        }

        private static void EachTraitCanMutateIndependently()
        {
            var a = Carrier();
            var b = KoiGeneticsRules.CreateFounder(KoiFounderVariety.Kohaku);
            for (int target = 0; target < 6; target++)
            {
                var rates = new float[6];
                rates[target] = 1f;
                for (int seed = 0; seed < 128; seed++)
                {
                    var before = Pairs(Breed(a, b, seed).Genome);
                    var child = Breed(a, b, seed, Rates(true, rates)).Genome;
                    KoiGeneticsRules.Validate(child);
                    var after = Pairs(child);
                    for (int locus = 0; locus < 6; locus++)
                    {
                        if (locus == target)
                            Check(before[locus].First != after[locus].First
                                && before[locus].Second != after[locus].Second,
                                "Forced mutation did not change both alleles at target " + target);
                        else
                            Check(EqualPair(before[locus], after[locus]),
                                "Mutation leaked into zero-rate locus " + locus);
                    }
                }
            }
            // Fin-only mutation works in both directions for every color/pattern founder.
            for (int variety = 0; variety < 8; variety++)
            for (int fin = 0; fin < 2; fin++)
            {
                var parent = KoiGeneticsRules.CreateFounder((KoiFounderVariety)variety, (KoiFinType)fin);
                var child = Breed(parent, parent, 73, Rates(true, new[] { 0f, 0f, 0f, 0f, 0f, 1f })).Genome;
                Check(KoiGeneticsRules.Resolve(child).Fin == (fin == 0 ? KoiFinType.Hirenaga : KoiFinType.Standard),
                    "Fin mutation depends on founder color/pattern");
            }
        }

        private static void ForcedMutationsAreValidAndActuallyChangeEveryAllele()
        {
            // Exercise every source allele and see every other permissible destination.
            for (int source = 0; source < 4; source++)
            {
                var pairs = new KoiAllelePair[6];
                var destinations = new HashSet<KoiAllele>[6];
                for (int locus = 0; locus < 6; locus++)
                {
                    var allele = Domains[locus][source % Domains[locus].Length];
                    pairs[locus] = new KoiAllelePair(allele, allele);
                    destinations[locus] = new HashSet<KoiAllele>();
                }
                var parent = GenomeFromPairs(pairs);
                for (int seed = 0; seed < 256; seed++)
                {
                    var child = Breed(parent, parent, seed, new KoiMutationSettings(true, 1f));
                    KoiGeneticsRules.Validate(child.Genome);
                    Check(child.Genome.SchemaVersion == KoiGenome.CurrentSchemaVersion, "Schema corrupted");
                    var actual = Pairs(child.Genome);
                    for (int locus = 0; locus < 6; locus++)
                    {
                        foreach (var allele in new[] { actual[locus].First, actual[locus].Second })
                        {
                            Check(allele != pairs[locus].First, "Mutation was a no-op");
                            Check(Array.IndexOf(Domains[locus], allele) >= 0, "Cross-locus or undefined allele");
                            destinations[locus].Add(allele);
                        }
                    }
                    var restored = JsonUtility.FromJson<KoiOffspringRecord>(JsonUtility.ToJson(child));
                    Check(JsonUtility.ToJson(child) == JsonUtility.ToJson(restored), "Mutated birth record lost serialized data");
                    KoiGeneticsRules.Validate(restored.Genome);
                }
                for (int locus = 0; locus < 6; locus++)
                    Check(destinations[locus].Count == Domains[locus].Length - 1, "Valid alternative never reachable");
            }
        }

        private static void RareMutationProbabilitiesMatchConfiguredRates()
        {
            const int births = 30000;
            var rates = new[] { 0.001f, 0.005f, 0.01f, 0.025f, 0.05f, 0.1f };
            var settings = Rates(true, rates);
            var parent = KoiGeneticsRules.CreateFounder(KoiFounderVariety.Purachina);
            var original = Pairs(parent);
            var firstChanges = new int[6];
            var secondChanges = new int[6];
            var seedRandom = new System.Random(451829);
            for (int birth = 0; birth < births; birth++)
            {
                var pairs = Pairs(Breed(parent, parent, seedRandom.Next(), settings).Genome);
                for (int locus = 0; locus < 6; locus++)
                {
                    if (pairs[locus].First != original[locus].First) firstChanges[locus]++;
                    if (pairs[locus].Second != original[locus].Second) secondChanges[locus]++;
                }
            }
            for (int locus = 0; locus < 6; locus++)
            {
                double expected = births * (double)rates[locus];
                double tolerance = 6 * Math.Sqrt(expected * (1 - rates[locus])) + 2;
                Check(firstChanges[locus] > 0 && Math.Abs(firstChanges[locus] - expected) <= tolerance,
                    "First-allele mutation probability mismatch at " + locus);
                Check(secondChanges[locus] > 0 && Math.Abs(secondChanges[locus] - expected) <= tolerance,
                    "Second-allele mutation probability mismatch at " + locus);
                Debug.Log("[KoiMutationTests] trait " + (KoiGeneticTrait)locus + ": rate=" + rates[locus]
                    + ", changed=" + firstChanges[locus] + "/" + secondChanges[locus] + " of " + births + " per allele slot");
            }
        }

        private static void SeededReplayAndGameplayRandomIndependence()
        {
            var a = Carrier();
            var b = Carrier();
            var settings = Rates(true, new[] { 0.05f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f });
            var snapshots = new HashSet<string>();
            var state = UnityEngine.Random.state;
            try
            {
                foreach (int seed in new[] { int.MinValue, -1, 0, 1, 73, 9021, int.MaxValue })
                {
                    UnityEngine.Random.InitState(836);
                    float expected = UnityEngine.Random.value;
                    UnityEngine.Random.InitState(836);
                    var child = Breed(a, b, seed, settings);
                    Check(UnityEngine.Random.value == expected, "Mutation consumed Unity randomness");
                    UnityEngine.Random.InitState(917);
                    var replay = Breed(a, b, seed, settings);
                    string snapshot = JsonUtility.ToJson(child.Genome);
                    Check(snapshot == JsonUtility.ToJson(replay.Genome), "Mutation replay is not reproducible");
                    Check(child.Id != replay.Id, "Replay reused offspring identity");
                    Check(child.FirstParentId == "parent-a" && child.SecondParentId == "parent-b"
                        && child.BreedingSeed == seed, "Mutation corrupted birth metadata");
                    snapshots.Add(snapshot);
                }
                Check(snapshots.Count > 1, "Different seeds cannot vary genomes");
            }
            finally { UnityEngine.Random.state = state; }
        }

        private static void ParentsAndAncestorGenomesArePreserved()
        {
            var a = Carrier();
            var b = KoiGeneticsRules.CreateFounder(KoiFounderVariety.ShowaSanshoku, KoiFinType.Hirenaga);
            string beforeA = JsonUtility.ToJson(a);
            string beforeB = JsonUtility.ToJson(b);
            var settings = new KoiMutationSettings(true, 1f);
            string beforeSettings = JsonUtility.ToJson(settings);
            for (int seed = 0; seed < 128; seed++)
            {
                var child = Breed(a, b, seed, settings);
                Check(!ReferenceEquals(a, child.Genome) && !ReferenceEquals(b, child.Genome), "Child aliases parent");
                string beforeChild = JsonUtility.ToJson(child.Genome);
                var grandchild = KoiBreedingService.Breed(child.Genome, child.Id, b, "parent-b", seed, settings);
                KoiGeneticsRules.Validate(grandchild.Genome);
                Check(beforeChild == JsonUtility.ToJson(child.Genome), "Offspring genome modified by later breeding");
                Check(beforeA == JsonUtility.ToJson(a) && beforeB == JsonUtility.ToJson(b), "Parent genome modified");
                Check(beforeSettings == JsonUtility.ToJson(settings), "Mutation settings were modified");
            }
        }

        private static void SettingsSerializationAndInvalidDataAreGuarded()
        {
            var a = Carrier();
            var rates = new[] { 0.01f, 0.02f, 0.03f, 0.04f, 0.05f, 0.06f };
            var settings = Rates(true, rates);
            var restored = JsonUtility.FromJson<KoiMutationSettings>(JsonUtility.ToJson(settings));
            Check(restored.Enabled, "Serialized enable flag lost");
            for (int locus = 0; locus < 6; locus++)
                Check(restored.GetProbability((KoiGeneticTrait)locus) == rates[locus], "Serialized trait rate lost");
            Check(JsonUtility.ToJson(Breed(a, a, 9, settings).Genome)
                == JsonUtility.ToJson(Breed(a, a, 9, restored).Genome), "Settings round trip changed replay");
            var legacy = JsonUtility.FromJson<KoiMutationSettings>("{\"enabled\":true,\"probabilityPerAllele\":1}");
            Check(JsonUtility.ToJson(Breed(a, a, 9, legacy).Genome)
                == JsonUtility.ToJson(Breed(a, a, 9, new KoiMutationSettings(true, 1f)).Genome),
                "Legacy uniform settings lost compatibility");
            Reject<ArgumentOutOfRangeException>(() => settings.GetProbability((KoiGeneticTrait)99));
            var fields = new[] { "basePigmentationProbability", "redProbability", "blackProbability",
                "patternProbability", "metallicProbability", "finProbability" };
            for (int locus = 0; locus < 6; locus++)
            {
                foreach (float invalid in new[] { -0.001f, 1.001f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
                {
                    var badRates = new float[6];
                    badRates[locus] = invalid;
                    Reject<ArgumentOutOfRangeException>(() => Rates(true, badRates));
                    Reject<ArgumentOutOfRangeException>(() => Rates(false, badRates));
                }
                var corrupt = JsonUtility.FromJson<KoiMutationSettings>("{\"enabled\":true,\"usePerTraitRates\":true,\""
                    + fields[locus] + "\":2}");
                Reject<ArgumentOutOfRangeException>(() => Breed(a, a, 9, corrupt));
            }
            var invalidGenome = new KoiGenome(new KoiAllelePair((KoiAllele)9999, KoiAllele.WhiteBase),
                a.Red, a.Black, a.Pattern, a.Metallic, a.Fin);
            Reject<ArgumentException>(() => Breed(invalidGenome, a, 9, new KoiMutationSettings(true, 1f)));
            var crossLocus = new KoiGenome(a.BasePigmentation, a.Red, a.Black, a.Pattern,
                a.Metallic, new KoiAllelePair(KoiAllele.Red, KoiAllele.HirenagaFin));
            Reject<ArgumentException>(() => Breed(a, crossLocus, 9, new KoiMutationSettings(true, 1f)));
            var badSchema = JsonUtility.FromJson<KoiGenome>(JsonUtility.ToJson(a).Replace("\"schemaVersion\":1", "\"schemaVersion\":99"));
            Reject<ArgumentException>(() => Breed(a, badSchema, 9, new KoiMutationSettings(true, 1f)));
            Reject<ArgumentNullException>(() => Breed(null, a, 9, settings));
        }
    }
}
