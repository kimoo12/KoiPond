using System;
using UnityEngine;

namespace KoiPond
{
    /// <summary>
    /// Reserved configuration only. Mutation behavior is not implemented in Phase 3 Step 2.
    /// Enabling mutations is rejected rather than silently ignored.
    /// </summary>
    [Serializable]
    public sealed class KoiMutationSettings
    {
        [SerializeField] private bool enabled;
        [SerializeField] private float probabilityPerAllele;

        public bool Enabled => enabled;
        public float ProbabilityPerAllele => probabilityPerAllele;

        public KoiMutationSettings(bool enabled = false, float probabilityPerAllele = 0f)
        {
            this.enabled = enabled;
            this.probabilityPerAllele = probabilityPerAllele;
            Validate();
        }

        internal void Validate()
        {
            if (float.IsNaN(probabilityPerAllele) || float.IsInfinity(probabilityPerAllele)
                || probabilityPerAllele < 0f || probabilityPerAllele > 1f)
                throw new ArgumentOutOfRangeException(nameof(probabilityPerAllele),
                    "Mutation probability must be finite and between zero and one.");
        }
    }

    /// <summary>
    /// Serializable genetic birth record, with no growth state or visual references.
    /// Genome inheritance is seed-reproducible; identity deliberately is not seed-derived,
    /// so repeating a breeding request produces a distinct individual with the same genome.
    /// Parent IDs correspond to the first and second alleles in every inherited pair.
    /// </summary>
    [Serializable]
    public sealed class KoiOffspringRecord
    {
        [SerializeField] private string id;
        [SerializeField] private string firstParentId;
        [SerializeField] private string secondParentId;
        [SerializeField] private int breedingSeed;
        [SerializeField] private KoiGenome genome;

        public string Id => id;
        public string FirstParentId => firstParentId;
        public string SecondParentId => secondParentId;
        public int BreedingSeed => breedingSeed;
        public KoiGenome Genome => genome;

        internal KoiOffspringRecord(string firstParentId, string secondParentId,
            int breedingSeed, KoiGenome genome)
        {
            do
            {
                id = Guid.NewGuid().ToString("N");
            }
            while (string.Equals(id, firstParentId, StringComparison.Ordinal)
                || string.Equals(id, secondParentId, StringComparison.Ordinal));

            this.firstParentId = firstParentId;
            this.secondParentId = secondParentId;
            this.breedingSeed = breedingSeed;
            this.genome = genome;
        }
    }

    /// <summary>
    /// Stateless, data-only inheritance. A dedicated System.Random is created per request;
    /// UnityEngine.Random and unrelated gameplay random streams are never read or modified.
    /// Parent order is significant for lineage and deterministic replay.
    /// </summary>
    public static class KoiBreedingService
    {
        public static KoiOffspringRecord Breed(KoiGenome firstParent, string firstParentId,
            KoiGenome secondParent, string secondParentId, int seed,
            KoiMutationSettings mutationSettings = null)
        {
            if (string.IsNullOrWhiteSpace(firstParentId))
                throw new ArgumentException("A nonempty first parent ID is required.", nameof(firstParentId));
            if (string.IsNullOrWhiteSpace(secondParentId))
                throw new ArgumentException("A nonempty second parent ID is required.", nameof(secondParentId));
            if (string.Equals(firstParentId, secondParentId, StringComparison.Ordinal))
                throw new ArgumentException("Breeding requires two distinct parent IDs.", nameof(secondParentId));

            KoiGeneticsRules.Validate(firstParent);
            KoiGeneticsRules.Validate(secondParent);
            if (mutationSettings != null)
            {
                mutationSettings.Validate();
                if (mutationSettings.Enabled)
                    throw new NotSupportedException("Mutation behavior is not implemented. Keep mutations disabled.");
            }

            var random = new System.Random(seed);
            var genome = new KoiGenome(
                Inherit(firstParent.BasePigmentation, secondParent.BasePigmentation, random),
                Inherit(firstParent.Red, secondParent.Red, random),
                Inherit(firstParent.Black, secondParent.Black, random),
                Inherit(firstParent.Pattern, secondParent.Pattern, random),
                Inherit(firstParent.Metallic, secondParent.Metallic, random),
                Inherit(firstParent.Fin, secondParent.Fin, random));

            KoiGeneticsRules.Validate(genome);
            return new KoiOffspringRecord(firstParentId, secondParentId, seed, genome);
        }

        private static KoiAllelePair Inherit(KoiAllelePair firstParent,
            KoiAllelePair secondParent, System.Random random)
        {
            var first = random.Next(2) == 0 ? firstParent.First : firstParent.Second;
            var second = random.Next(2) == 0 ? secondParent.First : secondParent.Second;
            return new KoiAllelePair(first, second);
        }
    }
}
