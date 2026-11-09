using UnityEngine;

namespace KoiPond.GrowthPrototype
{
    [CreateAssetMenu(menuName = "Koi Pond/Growth Prototype/Profile")]
    public sealed class KoiGrowthProfile : ScriptableObject
    {
        [Header("Active simulation seconds (diagnostic defaults)")]
        [SerializeField, Tooltip("Time to grow from 30% to 60% adult length.")]
        private double _fryDurationSeconds = 120;
        [SerializeField, Tooltip("Time to grow from 60% to 100% adult length.")]
        private double _juvenileDurationSeconds = 180;
        public double FryDurationSeconds => _fryDurationSeconds;
        public double JuvenileDurationSeconds => _juvenileDurationSeconds;
        public KoiGrowthState CreateState(double age = 0)
        { return new KoiGrowthState(_fryDurationSeconds, _juvenileDurationSeconds, age); }
    }
}
