using System;
using UnityEngine;

namespace KoiPond
{
    // Stable serialized IDs. Append new values; never renumber existing alleles.
    // Alleles are shared by the pair container, but rules validate each locus's domain.
    public enum KoiAllele
    {
        WhiteBase = 100,
        YellowBase = 101,
        BrownBase = 102,
        BlackBase = 103,
        NoRed = 200,
        Red = 201,
        NoBlack = 300,
        Black = 301,
        SolidPattern = 400,
        HeadSpotPattern = 401,
        DistributedPattern = 402,
        NonMetallic = 500,
        Metallic = 501,
        StandardFin = 600,
        HirenagaFin = 601
    }

    public enum KoiBasePigmentation { White = 0, Yellow = 1, Brown = 2, Black = 3 }
    public enum KoiPatternType { Solid = 0, HeadSpot = 1, Distributed = 2 }
    public enum KoiFinType { Standard = 0, Hirenaga = 1 }

    // Starter profile identifiers only: an arbitrary genome need not belong to this list.
    public enum KoiFounderVariety
    {
        TaishoSanke = 0,
        Chagoi = 1,
        Kohaku = 2,
        Purachina = 3,
        ShiroUtsuri = 4,
        ShowaSanshoku = 5,
        Tancho = 6,
        YamabukiOgon = 7
    }

    [Serializable]
    public struct KoiAllelePair
    {
        [SerializeField] private KoiAllele first;
        [SerializeField] private KoiAllele second;

        public KoiAllele First => first;
        public KoiAllele Second => second;
        public bool IsHomozygous => first == second;

        public KoiAllelePair(KoiAllele first, KoiAllele second)
        {
            this.first = first;
            this.second = second;
        }
    }

    /// <summary>
    /// Six diploid gameplay loci. Both alleles are retained, including unexpressed carriers.
    /// No variety assignment, growth state, or Unity asset references are stored here.
    /// Constructors supply valid data; deserialized data must be validated by the rules.
    /// </summary>
    [Serializable]
    public sealed class KoiGenome
    {
        public const int CurrentSchemaVersion = 1;

        [SerializeField] private int schemaVersion = CurrentSchemaVersion;
        [SerializeField] private KoiAllelePair basePigmentation;
        [SerializeField] private KoiAllelePair red;
        [SerializeField] private KoiAllelePair black;
        [SerializeField] private KoiAllelePair pattern;
        [SerializeField] private KoiAllelePair metallic;
        [SerializeField] private KoiAllelePair fin;

        public int SchemaVersion => schemaVersion;
        public KoiAllelePair BasePigmentation => basePigmentation;
        public KoiAllelePair Red => red;
        public KoiAllelePair Black => black;
        public KoiAllelePair Pattern => pattern;
        public KoiAllelePair Metallic => metallic;
        public KoiAllelePair Fin => fin;

        public KoiGenome(KoiAllelePair basePigmentation, KoiAllelePair red,
            KoiAllelePair black, KoiAllelePair pattern, KoiAllelePair metallic,
            KoiAllelePair fin)
        {
            this.basePigmentation = basePigmentation;
            this.red = red;
            this.black = black;
            this.pattern = pattern;
            this.metallic = metallic;
            this.fin = fin;
        }
    }

    /// <summary>
    /// Mesh-independent phenotype. Pattern records marking organization/potential even
    /// when red/black are absent. Black base plus distributed markings describes the
    /// black foundation of Utsuri/Showa; white areas are implicit in this starter model.
    /// </summary>
    [Serializable]
    public struct KoiTraits
    {
        [SerializeField] private KoiBasePigmentation basePigmentation;
        [SerializeField] private bool hasRed;
        [SerializeField] private bool hasBlack;
        [SerializeField] private KoiPatternType pattern;
        [SerializeField] private bool isMetallic;
        [SerializeField] private KoiFinType fin;

        public KoiBasePigmentation BasePigmentation => basePigmentation;
        public bool HasRed => hasRed;
        public bool HasBlack => hasBlack;
        public KoiPatternType Pattern => pattern;
        public bool IsMetallic => isMetallic;
        public KoiFinType Fin => fin;

        public KoiTraits(KoiBasePigmentation basePigmentation, bool hasRed,
            bool hasBlack, KoiPatternType pattern, bool isMetallic, KoiFinType fin)
        {
            this.basePigmentation = basePigmentation;
            this.hasRed = hasRed;
            this.hasBlack = hasBlack;
            this.pattern = pattern;
            this.isMetallic = isMetallic;
            this.fin = fin;
        }
    }
}
