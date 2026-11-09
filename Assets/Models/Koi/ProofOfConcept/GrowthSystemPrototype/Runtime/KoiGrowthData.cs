using System;
using UnityEngine;

namespace KoiPond.GrowthPrototype
{
    public enum KoiGrowthStage { Fry = 0, Juvenile = 1, Adult = 2 }

    [Serializable]
    public sealed class KoiGrowthState
    {
        public const int CurrentSchema = 1;
        [SerializeField] private int _schema = CurrentSchema;
        [SerializeField] private double _age;
        [SerializeField] private double _fryDuration;
        [SerializeField] private double _juvenileDuration;
        public int Schema => _schema;
        public double Age => _age;
        public double FryDuration => _fryDuration;
        public double JuvenileDuration => _juvenileDuration;
        public double AdultAge => _fryDuration + _juvenileDuration;

        public KoiGrowthState(double fryDuration, double juvenileDuration, double age = 0)
        {
            Positive(fryDuration, nameof(fryDuration));
            Positive(juvenileDuration, nameof(juvenileDuration));
            Positive(fryDuration + juvenileDuration, "totalDuration");
            Nonnegative(age, nameof(age));
            _fryDuration = fryDuration;
            _juvenileDuration = juvenileDuration;
            _age = Math.Min(age, AdultAge);
        }

        public void Validate()
        {
            if (_schema != CurrentSchema) throw new ArgumentException("Unsupported growth snapshot schema.");
            Positive(_fryDuration, "fryDuration");
            Positive(_juvenileDuration, "juvenileDuration");
            Positive(AdultAge, "totalDuration");
            Nonnegative(_age, "age");
            if (_age > AdultAge) throw new ArgumentException("Snapshot age exceeds adulthood.");
        }

        internal void SetAge(double age) { _age = age; }
        internal static void Nonnegative(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
                throw new ArgumentOutOfRangeException(name, "Value must be finite and nonnegative.");
        }
        internal static void Positive(double value, string name)
        {
            Nonnegative(value, name);
            if (value == 0) throw new ArgumentOutOfRangeException(name, "Duration must be positive.");
        }
    }

    [Serializable]
    public sealed class KoiIndividualState
    {
        [SerializeField] private KoiOffspringRecord _birth;
        [SerializeField] private KoiGrowthState _growth;
        public KoiOffspringRecord Birth => _birth;
        public KoiGrowthState Growth => _growth;
        public KoiIndividualState(KoiOffspringRecord birth, KoiGrowthState growth)
        {
            _birth = birth;
            _growth = growth;
            Validate();
        }
        public void Validate()
        {
            if (_birth == null || _growth == null) throw new ArgumentException("Birth record and growth state required.");
            if (string.IsNullOrWhiteSpace(_birth.Id) || string.IsNullOrWhiteSpace(_birth.FirstParentId)
                || string.IsNullOrWhiteSpace(_birth.SecondParentId) || _birth.FirstParentId == _birth.SecondParentId
                || _birth.Id == _birth.FirstParentId || _birth.Id == _birth.SecondParentId)
                throw new ArgumentException("Invalid individual identity or lineage.");
            KoiGeneticsRules.Validate(_birth.Genome);
            _growth.Validate();
        }
        public string Snapshot() { Validate(); return JsonUtility.ToJson(this); }
        public static KoiIndividualState Restore(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Snapshot is empty.");
            var state = JsonUtility.FromJson<KoiIndividualState>(json);
            if (state == null) throw new ArgumentException("Invalid snapshot.");
            state.Validate();
            return state;
        }
    }
}
