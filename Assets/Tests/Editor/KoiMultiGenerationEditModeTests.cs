using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace KoiPond.EditorTests
{
    /// <summary>
    /// Data-only EditMode tests. Bounded seed sweeps demonstrate possible inheritance,
    /// not exact Mendelian frequency estimates. No gameplay implementation is changed.
    /// </summary>
    public static class KoiMultiGenerationEditModeTests
    {
        private static int _assertions;
        private static int _births;
        private static readonly HashSet<string> Ids = new HashSet<string>();
        private static readonly Dictionary<string, KoiGenome> Genomes = new Dictionary<string, KoiGenome>();
        private static readonly Dictionary<string, string> Snapshots = new Dictionary<string, string>();

        [MenuItem("Tools/Koi Pond/Tests/Run Multi-Generation EditMode Tests")]
        public static void RunFromMenu()
        {
            Debug.Log(RunAll());
        }

        public static string RunAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Run multi-generation tests in EditMode.");
            _assertions = 0;
            _births = 0;
            Ids.Clear();
            Genomes.Clear();
            Snapshots.Clear();
            RecessiveAllelesReappearAfterTwoCarrierGenerations();
            Debug.Log("[KoiMultiGenTests] PASS recessive alleles at all six loci: F1/F2 carriers, F3 expression");
            FinInheritanceRemainsIndependentAcrossThreeGenerations();
            Debug.Log("[KoiMultiGenTests] PASS independent color/pattern and Standard/Hirenaga inheritance through F3");
            HybridsProduceFurtherNonStarterHybrids();
            Debug.Log("[KoiMultiGenTests] PASS non-starter hybrid continuation through F3");
            FiveGenerationPopulationPreservesLineageAndGenomes();
            Debug.Log("[KoiMultiGenTests] PASS five-generation population: identity, lineage, replay, allele provenance");
            foreach (var entry in Genomes)
                Check(JsonUtility.ToJson(entry.Value) == Snapshots[entry.Key],
                    "Ancestor genome changed: " + entry.Key);
            Debug.Log("[KoiMultiGenTests] PASS all parent/grandparent/ancestor genome snapshots unchanged");
            return "PASS: 5 multi-generation validation groups; " + _assertions
                + " assertions; " + _births + " unique offspring records (including deterministic replays).";
        }

        private static void Check(bool condition, string message)
        {
            _assertions++;
            if (!condition) throw new InvalidOperationException("Multi-generation test failed: " + message);
        }

        private static KoiAllelePair[] Pairs(KoiGenome genome)
        {
            return new[] { genome.BasePigmentation, genome.Red, genome.Black,
                genome.Pattern, genome.Metallic, genome.Fin };
        }

        private static void Remember(string id, KoiGenome genome)
        {
            string json = JsonUtility.ToJson(genome);
            if (Genomes.ContainsKey(id))
            {
                Check(Snapshots[id] == json, "Identity's genome changed: " + id);
                return;
            }
            Check(Ids.Add(id), "Duplicate ID: " + id);
            Genomes.Add(id, genome);
            Snapshots.Add(id, json);
        }

        // Every simulated breeding request checks all requested global invariants.
        private static KoiOffspringRecord Breed(KoiGenome first, string firstId,
            KoiGenome second, string secondId, int seed)
        {
            Remember(firstId, first);
            Remember(secondId, second);
            var child = KoiBreedingService.Breed(first, firstId, second, secondId, seed);
            var replay = KoiBreedingService.Breed(first, firstId, second, secondId, seed);
            Check(JsonUtility.ToJson(child.Genome) == JsonUtility.ToJson(replay.Genome),
                "Ordered parents and seed did not reproduce genome");
            Check(child.Id != replay.Id, "Replay reused offspring ID");
            foreach (var record in new[] { child, replay })
            {
                Check(Guid.TryParseExact(record.Id, "N", out var parsed), "Invalid unique ID format");
                Check(record.FirstParentId == firstId && record.SecondParentId == secondId
                    && record.BreedingSeed == seed, "Incorrect parent IDs or seed");
                Check(!ReferenceEquals(record.Genome, first) && !ReferenceEquals(record.Genome, second),
                    "Offspring genome aliases a parent");
                KoiGeneticsRules.Validate(record.Genome);
                var a = Pairs(first);
                var b = Pairs(second);
                var c = Pairs(record.Genome);
                for (int locus = 0; locus < c.Length; locus++)
                {
                    Check(c[locus].First == a[locus].First || c[locus].First == a[locus].Second,
                        "First-parent allele missing at locus " + locus);
                    Check(c[locus].Second == b[locus].First || c[locus].Second == b[locus].Second,
                        "Second-parent allele missing at locus " + locus);
                }
                Remember(record.Id, record.Genome);
                _births++;
            }
            Check(JsonUtility.ToJson(first) == Snapshots[firstId], "First parent mutated");
            Check(JsonUtility.ToJson(second) == Snapshots[secondId], "Second parent mutated");
            return child;
        }

        private static KoiOffspringRecord Breed(KoiOffspringRecord first,
            KoiOffspringRecord second, int seed)
        {
            return Breed(first.Genome, first.Id, second.Genome, second.Id, seed);
        }

        private static KoiGenome HomozygousGenome(KoiAllele[] alleles)
        {
            return new KoiGenome(new KoiAllelePair(alleles[0], alleles[0]),
                new KoiAllelePair(alleles[1], alleles[1]), new KoiAllelePair(alleles[2], alleles[2]),
                new KoiAllelePair(alleles[3], alleles[3]), new KoiAllelePair(alleles[4], alleles[4]),
                new KoiAllelePair(alleles[5], alleles[5]));
        }

        private static bool RecessiveTraitExpressed(KoiTraits traits, int locus)
        {
            switch (locus)
            {
                case 0: return traits.BasePigmentation == KoiBasePigmentation.White;
                case 1: return !traits.HasRed;
                case 2: return !traits.HasBlack;
                case 3: return traits.Pattern == KoiPatternType.HeadSpot;
                case 4: return !traits.IsMetallic;
                case 5: return traits.Fin == KoiFinType.Standard;
                default: throw new ArgumentOutOfRangeException(nameof(locus));
            }
        }

        private static void RecessiveAllelesReappearAfterTwoCarrierGenerations()
        {
            var low = new[] { KoiAllele.WhiteBase, KoiAllele.NoRed, KoiAllele.NoBlack,
                KoiAllele.HeadSpotPattern, KoiAllele.NonMetallic, KoiAllele.StandardFin };
            var high = new[] { KoiAllele.BlackBase, KoiAllele.Red, KoiAllele.Black,
                KoiAllele.DistributedPattern, KoiAllele.Metallic, KoiAllele.HirenagaFin };
            var recessiveGrandparent = HomozygousGenome(low);
            var dominantGrandparent = HomozygousGenome(high);
            var f1a = Breed(recessiveGrandparent, "recessive-founder", dominantGrandparent, "dominant-founder", 11);
            var f1b = Breed(recessiveGrandparent, "recessive-founder", dominantGrandparent, "dominant-founder", 12);
            for (int locus = 0; locus < 6; locus++)
            {
                Check(!RecessiveTraitExpressed(KoiGeneticsRules.Resolve(f1a.Genome), locus),
                    "F1 recessive allele should be hidden at locus " + locus);
                Check(!RecessiveTraitExpressed(KoiGeneticsRules.Resolve(f1b.Genome), locus),
                    "Second F1 recessive allele should be hidden");
            }

            var carrierA = new KoiOffspringRecord[6];
            var carrierB = new KoiOffspringRecord[6];
            for (int seed = 0; seed < 128; seed++)
            {
                var f2 = Breed(f1a, f1b, seed);
                var pairs = Pairs(f2.Genome);
                for (int locus = 0; locus < 6; locus++)
                    if (!pairs[locus].IsHomozygous)
                    {
                        Check(!RecessiveTraitExpressed(KoiGeneticsRules.Resolve(f2.Genome), locus),
                            "F2 carrier should still hide recessive trait");
                        if (carrierA[locus] == null) carrierA[locus] = f2;
                        else if (carrierB[locus] == null) carrierB[locus] = f2;
                    }
            }
            for (int locus = 0; locus < 6; locus++)
            {
                Check(carrierA[locus] != null && carrierB[locus] != null, "No F2 carrier pair found");
                bool expressed = false;
                for (int seed = 0; seed < 128; seed++)
                {
                    var f3 = Breed(carrierA[locus], carrierB[locus], 1000 + seed);
                    var pair = Pairs(f3.Genome)[locus];
                    if (pair.First == low[locus] && pair.Second == low[locus])
                    {
                        Check(RecessiveTraitExpressed(KoiGeneticsRules.Resolve(f3.Genome), locus),
                            "Homozygous recessive allele failed to express in F3");
                        expressed = true;
                    }
                }
                Check(expressed, "No later recessive expression at locus " + locus);
            }
        }

        private static bool SameColorAndPattern(KoiTraits a, KoiTraits b)
        {
            return a.BasePigmentation == b.BasePigmentation && a.HasRed == b.HasRed
                && a.HasBlack == b.HasBlack && a.Pattern == b.Pattern && a.IsMetallic == b.IsMetallic;
        }

        private static void FinInheritanceRemainsIndependentAcrossThreeGenerations()
        {
            var standard = KoiGeneticsRules.CreateFounder(KoiFounderVariety.Kohaku);
            var longfin = KoiGeneticsRules.CreateFounder(KoiFounderVariety.Kohaku, KoiFinType.Hirenaga);
            var a = Breed(standard, "fin-standard", longfin, "fin-long", 1);
            var b = Breed(standard, "fin-standard", longfin, "fin-long", 2);
            var expectedColor = KoiGeneticsRules.Resolve(standard);
            Check(SameColorAndPattern(KoiGeneticsRules.Resolve(a.Genome), expectedColor), "F1 fin cross changed colors");
            var carriers = new List<KoiOffspringRecord>();
            bool standardF2 = false;
            bool longF2 = false;
            for (int seed = 0; seed < 128; seed++)
            {
                var child = Breed(a, b, seed);
                var traits = KoiGeneticsRules.Resolve(child.Genome);
                Check(SameColorAndPattern(traits, expectedColor), "F2 fin segregation changed color/pattern");
                standardF2 |= traits.Fin == KoiFinType.Standard;
                longF2 |= traits.Fin == KoiFinType.Hirenaga;
                if (!child.Genome.Fin.IsHomozygous) carriers.Add(child);
            }
            Check(standardF2 && longF2 && carriers.Count >= 2, "F2 fin segregation missing");
            bool standardF3 = false;
            bool longF3 = false;
            for (int seed = 0; seed < 128; seed++)
            {
                var child = Breed(carriers[0], carriers[1], 500 + seed);
                var traits = KoiGeneticsRules.Resolve(child.Genome);
                Check(SameColorAndPattern(traits, expectedColor), "F3 fin segregation changed color/pattern");
                standardF3 |= traits.Fin == KoiFinType.Standard;
                longF3 |= traits.Fin == KoiFinType.Hirenaga;
            }
            Check(standardF3 && longF3, "Both fin types must remain possible in F3");
        }

        private static bool IsStarterColorProfile(KoiTraits traits)
        {
            for (int i = 0; i < 8; i++)
                if (SameColorAndPattern(traits, KoiGeneticsRules.Resolve(
                    KoiGeneticsRules.CreateFounder((KoiFounderVariety)i)))) return true;
            return false;
        }

        private static void HybridsProduceFurtherNonStarterHybrids()
        {
            var gold = KoiGeneticsRules.CreateFounder(KoiFounderVariety.YamabukiOgon);
            var kohaku = KoiGeneticsRules.CreateFounder(KoiFounderVariety.Kohaku);
            var chagoi = KoiGeneticsRules.CreateFounder(KoiFounderVariety.Chagoi);
            var tancho = KoiGeneticsRules.CreateFounder(KoiFounderVariety.Tancho);
            var f1a = Breed(gold, "hybrid-gold", kohaku, "hybrid-kohaku", 3);
            var f1b = Breed(chagoi, "hybrid-chagoi", tancho, "hybrid-tancho", 4);
            Check(!IsStarterColorProfile(KoiGeneticsRules.Resolve(f1a.Genome)), "F1 hybrid was reduced to a starter");
            Check(!IsStarterColorProfile(KoiGeneticsRules.Resolve(f1b.Genome)), "Second F1 hybrid was reduced to a starter");
            KoiOffspringRecord f2hybrid = null;
            for (int seed = 0; seed < 128; seed++)
            {
                var f2 = Breed(f1a, f1b, seed);
                var traits = KoiGeneticsRules.Resolve(f2.Genome);
                if (traits.BasePigmentation == KoiBasePigmentation.Brown && traits.HasRed
                    && traits.IsMetallic && !IsStarterColorProfile(traits)) f2hybrid = f2;
            }
            Check(f2hybrid != null, "F1 hybrids never combined brown/red/metallic in F2");
            bool furtherHybrid = false;
            for (int seed = 0; seed < 128; seed++)
            {
                var f3 = Breed(f2hybrid, f1a, 900 + seed);
                var traits = KoiGeneticsRules.Resolve(f3.Genome);
                furtherHybrid |= traits.HasRed && traits.IsMetallic && !IsStarterColorProfile(traits);
            }
            Check(furtherHybrid, "F2 hybrid could not continue non-starter traits into F3");
        }

        private static void FiveGenerationPopulationPreservesLineageAndGenomes()
        {
            // Start with all eight existing variety profiles, each in both fin forms.
            var genomes = new List<KoiGenome>();
            var ids = new List<string>();
            for (int variety = 0; variety < 8; variety++)
            for (int fin = 0; fin < 2; fin++)
            {
                var genome = KoiGeneticsRules.CreateFounder((KoiFounderVariety)variety, (KoiFinType)fin);
                string id = "population-founder-" + variety + "-" + fin;
                Remember(id, genome);
                genomes.Add(genome);
                ids.Add(id);
            }
            for (int generation = 1; generation <= 5; generation++)
            {
                var nextGenomes = new List<KoiGenome>();
                var nextIds = new List<string>();
                for (int index = 0; index < 32; index++)
                {
                    int first = index % genomes.Count;
                    int second = (first + generation * 2 + 1) % genomes.Count;
                    var child = Breed(genomes[first], ids[first], genomes[second], ids[second],
                        20000 + generation * 100 + index);
                    nextGenomes.Add(child.Genome);
                    nextIds.Add(child.Id);
                }
                genomes = nextGenomes;
                ids = nextIds;
            }
        }
    }
}
