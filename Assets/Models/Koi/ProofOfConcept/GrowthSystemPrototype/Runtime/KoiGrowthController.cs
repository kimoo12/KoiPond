using System;
using UnityEngine;

namespace KoiPond.GrowthPrototype
{
    public sealed class KoiGrowthController : MonoBehaviour
    {
        [Header("Persistent individual (initialized explicitly, never reborn on enable)")]
        [SerializeReference] private KoiIndividualState _individual;
        [SerializeField] private KoiGrowthProfile _profile;
        [SerializeField] private bool _paused;
        public KoiIndividualState Individual => _individual;
        public KoiGrowthProfile Profile => _profile;
        public bool Paused { get => _paused; set => _paused = value; }
        public bool Initialized => _individual != null && _individual.Birth != null && !string.IsNullOrWhiteSpace(_individual.Birth.Id);
        public KoiGrowthEvaluation Evaluation => KoiGrowthRules.Evaluate(_individual.Growth);
        public event Action Changed;
        public event Action<KoiGrowthStage, KoiGrowthStage> StageTransition;

        public void Initialize(KoiOffspringRecord birth, KoiGrowthProfile profile, double age = 0)
        {
            if (Initialized) throw new InvalidOperationException("Already bound. Reset age without recreating identity.");
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            var individual = new KoiIndividualState(birth, profile.CreateState(age));
            _profile = profile;
            _individual = individual;
            Changed?.Invoke();
        }
        private void Awake() { if (Initialized) _individual.Validate(); }
private void Update()
        {
            if (Initialized && !_paused) Advance(Time.deltaTime);
        }
        /// <summary>Explicit diagnostics may advance while paused; automatic ticking respects pause.</summary>
        public void Advance(double seconds)
        {
            if (!Initialized) throw new InvalidOperationException("Individual not initialized.");
            KoiGrowthRules.Advance(_individual.Growth, seconds, (from, to) => StageTransition?.Invoke(from, to));
            Changed?.Invoke();
        }
        public void ResetAge(bool useCurrentProfile = false)
        {
            if (!Initialized) throw new InvalidOperationException("Individual not initialized.");
            var old = _individual.Growth;
            var growth = useCurrentProfile ? _profile.CreateState() : new KoiGrowthState(old.FryDuration, old.JuvenileDuration);
            _individual = new KoiIndividualState(_individual.Birth, growth);
            Changed?.Invoke();
        }
        public string Snapshot() => _individual.Snapshot();
        public void RestoreSnapshot(string json)
        {
            var restored = KoiIndividualState.Restore(json);
            if (Initialized && JsonUtility.ToJson(restored.Birth) != JsonUtility.ToJson(_individual.Birth))
                throw new ArgumentException("Snapshot is not this individual's unchanged birth record.");
            _individual = Initialized ? new KoiIndividualState(_individual.Birth, restored.Growth) : restored;
            Changed?.Invoke();
        }
    }
}
