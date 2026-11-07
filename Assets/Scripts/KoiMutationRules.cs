using System;

namespace KoiPond
{
    /// <summary>
    /// Separate post-inheritance mutation pass. Each allele gets one independent trial.
    /// Successful trials choose uniformly from other valid alleles at the same locus.
    /// Never mutates the supplied genome, parents, settings, or global random state.
    /// </summary>
    internal static class KoiMutationRules
    {
        internal static KoiGenome Apply(KoiGenome inherited, KoiMutationSettings settings,
            System.Random random)
        {
            KoiGeneticsRules.Validate(inherited);
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (settings == null) return inherited;
            settings.Validate();
            if (!settings.Enabled) return inherited;

            var mutated = new KoiGenome(
                Mutate(inherited.BasePigmentation, KoiGeneticTrait.BasePigmentation, settings, random),
                Mutate(inherited.Red, KoiGeneticTrait.Red, settings, random),
                Mutate(inherited.Black, KoiGeneticTrait.Black, settings, random),
                Mutate(inherited.Pattern, KoiGeneticTrait.Pattern, settings, random),
                Mutate(inherited.Metallic, KoiGeneticTrait.Metallic, settings, random),
                Mutate(inherited.Fin, KoiGeneticTrait.Fin, settings, random));
            KoiGeneticsRules.Validate(mutated);
            return mutated;
        }

        private static KoiAllelePair Mutate(KoiAllelePair pair, KoiGeneticTrait trait,
            KoiMutationSettings settings, System.Random random)
        {
            float probability = settings.GetProbability(trait);
            // A zero-rate locus consumes no randomness and retains both inherited alleles.
            if (probability == 0f) return pair;
            var domain = KoiGeneticsRules.GetAlleleDomain(trait);
            return new KoiAllelePair(MutateAllele(pair.First, probability, domain, random),
                MutateAllele(pair.Second, probability, domain, random));
        }

        private static KoiAllele MutateAllele(KoiAllele allele, float probability,
            System.Collections.Generic.IReadOnlyList<KoiAllele> domain, System.Random random)
        {
            if (random.NextDouble() >= probability) return allele;
            int currentIndex = -1;
            for (int i = 0; i < domain.Count; i++)
                if (domain[i] == allele) { currentIndex = i; break; }
            if (currentIndex < 0 || domain.Count < 2)
                throw new InvalidOperationException("Invalid mutation allele domain.");
            int alternativeIndex = random.Next(domain.Count - 1);
            if (alternativeIndex >= currentIndex) alternativeIndex++;
            return domain[alternativeIndex];
        }
    }
}
