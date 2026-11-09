using System;

namespace KoiPond.GrowthPrototype
{
    public readonly struct KoiGrowthEvaluation
    {
        public KoiGrowthStage Stage { get; }
        public double Progress { get; }
        public float SizeRatio { get; }
        internal KoiGrowthEvaluation(KoiGrowthStage stage, double progress, float ratio)
        { Stage = stage; Progress = progress; SizeRatio = ratio; }
    }

    /// <summary>Only time and growth data; no genetics, randomness or visual dependencies.</summary>
    public static class KoiGrowthRules
    {
        public static KoiGrowthEvaluation Evaluate(KoiGrowthState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            state.Validate();
            if (state.Age >= state.AdultAge) return new KoiGrowthEvaluation(KoiGrowthStage.Adult, 1, 1);
            if (state.Age >= state.FryDuration)
            {
                double p = (state.Age - state.FryDuration) / state.JuvenileDuration;
                return new KoiGrowthEvaluation(KoiGrowthStage.Juvenile, p, (float)(.60 + .25 * p));
            }
            double fry = state.Age / state.FryDuration;
            return new KoiGrowthEvaluation(KoiGrowthStage.Fry, fry, (float)(.30 + .10 * fry));
        }

        /// <summary>Commits final age before ordered notifications; leftover time is never lost.</summary>
        public static void Advance(KoiGrowthState state, double delta,
            Action<KoiGrowthStage, KoiGrowthStage> crossed = null)
        {
            var before = Evaluate(state).Stage;
            KoiGrowthState.Nonnegative(delta, nameof(delta));
            double remaining = state.AdultAge - state.Age;
            state.SetAge(delta >= remaining ? state.AdultAge : state.Age + delta);
            var after = Evaluate(state).Stage;
            for (int s = (int)before; s < (int)after; s++)
                crossed?.Invoke((KoiGrowthStage)s, (KoiGrowthStage)(s + 1));
        }
    }
}
