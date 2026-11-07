using System;
using UnityEngine;

namespace KoiPond
{
    /// <summary>
    /// Opt-in mutation probabilities per inherited allele, in [0, 1]. Use small values
    /// (for example 0.001) for rare mutations; zero disables mutation at that locus.
    /// The original constructor/serialized fields retain uniform-rate compatibility.
    /// </summary>
    [Serializable]
    public sealed class KoiMutationSettings
    {
        [SerializeField] private bool enabled;
        [SerializeField] private float probabilityPerAllele;
        [SerializeField] private bool usePerTraitRates;
        [SerializeField] private float basePigmentationProbability;
        [SerializeField] private float redProbability;
        [SerializeField] private float blackProbability;
        [SerializeField] private float patternProbability;
        [SerializeField] private float metallicProbability;
        [SerializeField] private float finProbability;

        public bool Enabled => enabled;
        // Uniform fallback only; use GetProbability for the effective per-trait rate.
        public float ProbabilityPerAllele => probabilityPerAllele;

        public KoiMutationSettings(bool enabled = false, float probabilityPerAllele = 0f)
        {
            this.enabled = enabled;
            this.probabilityPerAllele = probabilityPerAllele;
            Validate();
        }

        public KoiMutationSettings(bool enabled, float basePigmentationProbability,
            float redProbability, float blackProbability, float patternProbability,
            float metallicProbability, float finProbability)
        {
            this.enabled = enabled;
            usePerTraitRates = true;
            this.basePigmentationProbability = basePigmentationProbability;
            this.redProbability = redProbability;
            this.blackProbability = blackProbability;
            this.patternProbability = patternProbability;
            this.metallicProbability = metallicProbability;
            this.finProbability = finProbability;
            Validate();
        }

        public float GetProbability(KoiGeneticTrait trait)
        {
            float rate;
            switch (trait)
            {
                case KoiGeneticTrait.BasePigmentation: rate = basePigmentationProbability; break;
                case KoiGeneticTrait.Red: rate = redProbability; break;
                case KoiGeneticTrait.Black: rate = blackProbability; break;
                case KoiGeneticTrait.Pattern: rate = patternProbability; break;
                case KoiGeneticTrait.Metallic: rate = metallicProbability; break;
                case KoiGeneticTrait.Fin: rate = finProbability; break;
                default: throw new ArgumentOutOfRangeException(nameof(trait));
            }
            return usePerTraitRates ? rate : probabilityPerAllele;
        }

        internal void Validate()
        {
            ValidateRate(probabilityPerAllele, nameof(probabilityPerAllele));
            ValidateRate(basePigmentationProbability, nameof(basePigmentationProbability));
            ValidateRate(redProbability, nameof(redProbability));
            ValidateRate(blackProbability, nameof(blackProbability));
            ValidateRate(patternProbability, nameof(patternProbability));
            ValidateRate(metallicProbability, nameof(metallicProbability));
            ValidateRate(finProbability, nameof(finProbability));
        }

        private static void ValidateRate(float rate, string name)
        {
            if (float.IsNaN(rate) || float.IsInfinity(rate) || rate < 0f || rate > 1f)
                throw new ArgumentOutOfRangeException(name,
                    "Mutation probability must be finite and between zero and one.");
        }
    }

    /// <summary>
    /// Serializable genetic birth record, with no growth state or visual references.
    /// Genome inheritance and mutation are reproducible for the same seed and settings;
    /// identity deliberately is not seed-derived, so replay creates a distinct individual.
    /// Parent IDs correspond to the first and second inherited allele slots; a mutation
    /// may subsequently replace an allele in either slot without changing lineage.
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
                mutationSettings.Validate();

            var random = new System.Random(seed);
            var genome = new KoiGenome(
                Inherit(firstParent.BasePigmentation, secondParent.BasePigmentation, random),
                Inherit(firstParent.Red, secondParent.Red, random),
                Inherit(firstParent.Black, secondParent.Black, random),
                Inherit(firstParent.Pattern, secondParent.Pattern, random),
                Inherit(firstParent.Metallic, secondParent.Metallic, random),
                Inherit(firstParent.Fin, secondParent.Fin, random));

            genome = KoiMutationRules.Apply(genome, mutationSettings, random);
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
