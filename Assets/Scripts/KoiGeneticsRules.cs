using System;

namespace KoiPond
{
    /// <summary>
    /// Deterministic gameplay inheritance/expression rules, not a biological simulation.
    /// Dominance is deliberately simplified and explicit (highest priority last):
    /// base White < Yellow < Brown < Black; pattern Solid < HeadSpot < Distributed;
    /// red, black, metallic, and Hirenaga presence dominate their absence.
    /// No mating, mutation, random generation, or visual variety classification occurs here.
    /// </summary>
    public static class KoiGeneticsRules
    {
        private static readonly KoiAllele[] BaseAlleles =
            { KoiAllele.WhiteBase, KoiAllele.YellowBase, KoiAllele.BrownBase, KoiAllele.BlackBase };
        private static readonly KoiAllele[] RedAlleles = { KoiAllele.NoRed, KoiAllele.Red };
        private static readonly KoiAllele[] BlackAlleles = { KoiAllele.NoBlack, KoiAllele.Black };
        private static readonly KoiAllele[] PatternAlleles =
            { KoiAllele.SolidPattern, KoiAllele.HeadSpotPattern, KoiAllele.DistributedPattern };
        private static readonly KoiAllele[] MetallicAlleles = { KoiAllele.NonMetallic, KoiAllele.Metallic };
        private static readonly KoiAllele[] FinAlleles = { KoiAllele.StandardFin, KoiAllele.HirenagaFin };

        /// <summary>
        /// Rejects corrupt, cross-locus, or unsupported schema data rather than guessing.
        /// Extend the allele domains and introduce explicit schema migration when needed.
        /// Unfamiliar combinations of valid alleles are allowed, regardless of starter varieties.
        /// </summary>
        public static void Validate(KoiGenome genome)
        {
            if (genome == null) throw new ArgumentNullException(nameof(genome));
            if (genome.SchemaVersion != KoiGenome.CurrentSchemaVersion)
                throw new ArgumentException("Unsupported koi genome schema version: " + genome.SchemaVersion, nameof(genome));

            ValidatePair(genome.BasePigmentation, BaseAlleles, "BasePigmentation");
            ValidatePair(genome.Red, RedAlleles, "Red");
            ValidatePair(genome.Black, BlackAlleles, "Black");
            ValidatePair(genome.Pattern, PatternAlleles, "Pattern");
            ValidatePair(genome.Metallic, MetallicAlleles, "Metallic");
            ValidatePair(genome.Fin, FinAlleles, "Fin");
        }

        public static KoiTraits Resolve(KoiGenome genome)
        {
            Validate(genome);
            var baseAllele = Express(genome.BasePigmentation, BaseAlleles);
            var patternAllele = Express(genome.Pattern, PatternAlleles);
            KoiBasePigmentation baseColor;
            switch (baseAllele)
            {
                case KoiAllele.WhiteBase: baseColor = KoiBasePigmentation.White; break;
                case KoiAllele.YellowBase: baseColor = KoiBasePigmentation.Yellow; break;
                case KoiAllele.BrownBase: baseColor = KoiBasePigmentation.Brown; break;
                case KoiAllele.BlackBase: baseColor = KoiBasePigmentation.Black; break;
                default: throw new InvalidOperationException("Missing base pigmentation expression rule.");
            }

            KoiPatternType pattern;
            switch (patternAllele)
            {
                case KoiAllele.SolidPattern: pattern = KoiPatternType.Solid; break;
                case KoiAllele.HeadSpotPattern: pattern = KoiPatternType.HeadSpot; break;
                case KoiAllele.DistributedPattern: pattern = KoiPatternType.Distributed; break;
                default: throw new InvalidOperationException("Missing pattern expression rule.");
            }

            return new KoiTraits(baseColor,
                Express(genome.Red, RedAlleles) == KoiAllele.Red,
                Express(genome.Black, BlackAlleles) == KoiAllele.Black,
                pattern,
                Express(genome.Metallic, MetallicAlleles) == KoiAllele.Metallic,
                Express(genome.Fin, FinAlleles) == KoiAllele.HirenagaFin
                    ? KoiFinType.Hirenaga : KoiFinType.Standard);
        }

        /// <summary>
        /// Fresh homozygous starter genomes reproduce the eight variety trait profiles.
        /// They are authored baselines, not claims about the real fish's hidden ancestry.
        /// Author heterozygous pairs through KoiGenome to introduce known carrier alleles.
        /// Fin form is orthogonal: every color/pattern profile supports either fin type.
        /// </summary>
        public static KoiGenome CreateFounder(KoiFounderVariety variety,
            KoiFinType finType = KoiFinType.Standard)
        {
            if (finType != KoiFinType.Standard && finType != KoiFinType.Hirenaga)
                throw new ArgumentOutOfRangeException(nameof(finType));

            KoiAllele baseColor;
            KoiAllele red = KoiAllele.NoRed;
            KoiAllele black = KoiAllele.NoBlack;
            KoiAllele pattern = KoiAllele.SolidPattern;
            KoiAllele metallic = KoiAllele.NonMetallic;
            switch (variety)
            {
                case KoiFounderVariety.TaishoSanke:
                    baseColor = KoiAllele.WhiteBase;
                    red = KoiAllele.Red;
                    black = KoiAllele.Black;
                    pattern = KoiAllele.DistributedPattern;
                    break;
                case KoiFounderVariety.Chagoi:
                    baseColor = KoiAllele.BrownBase;
                    break;
                case KoiFounderVariety.Kohaku:
                    baseColor = KoiAllele.WhiteBase;
                    red = KoiAllele.Red;
                    pattern = KoiAllele.DistributedPattern;
                    break;
                case KoiFounderVariety.Purachina:
                    baseColor = KoiAllele.WhiteBase;
                    metallic = KoiAllele.Metallic;
                    break;
                case KoiFounderVariety.ShiroUtsuri:
                    baseColor = KoiAllele.BlackBase;
                    black = KoiAllele.Black;
                    pattern = KoiAllele.DistributedPattern;
                    break;
                case KoiFounderVariety.ShowaSanshoku:
                    baseColor = KoiAllele.BlackBase;
                    red = KoiAllele.Red;
                    black = KoiAllele.Black;
                    pattern = KoiAllele.DistributedPattern;
                    break;
                case KoiFounderVariety.Tancho:
                    // The existing Tancho is represented as a white, red-head-spot profile.
                    baseColor = KoiAllele.WhiteBase;
                    red = KoiAllele.Red;
                    pattern = KoiAllele.HeadSpotPattern;
                    break;
                case KoiFounderVariety.YamabukiOgon:
                    baseColor = KoiAllele.YellowBase;
                    metallic = KoiAllele.Metallic;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(variety));
            }

            var fin = finType == KoiFinType.Hirenaga ? KoiAllele.HirenagaFin : KoiAllele.StandardFin;
            return new KoiGenome(Homozygous(baseColor), Homozygous(red), Homozygous(black),
                Homozygous(pattern), Homozygous(metallic), Homozygous(fin));
        }

        private static KoiAllelePair Homozygous(KoiAllele allele)
        {
            return new KoiAllelePair(allele, allele);
        }

        private static void ValidatePair(KoiAllelePair pair, KoiAllele[] domain, string locus)
        {
            if (Array.IndexOf(domain, pair.First) < 0 || Array.IndexOf(domain, pair.Second) < 0)
                throw new ArgumentException("Invalid allele for " + locus + ": " + pair.First + "/" + pair.Second);
        }

        private static KoiAllele Express(KoiAllelePair pair, KoiAllele[] priority)
        {
            int firstRank = Array.IndexOf(priority, pair.First);
            int secondRank = Array.IndexOf(priority, pair.Second);
            return firstRank >= secondRank ? pair.First : pair.Second;
        }
    }
}
