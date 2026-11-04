using System;

namespace Game.Combat.Domain
{
    /// <summary>Nộ của một actor.</summary>
    public sealed class EnergyPool
    {
        public float Current { get; private set; }
        public float Max { get; }

        public float Ratio => Max > 0f ? Current / Max : 0f;

        public EnergyPool(float max)
        {
            Max = max;
        }

        public void Gain(float amount)
        {
            Current = Math.Min(Max, Math.Max(0f, Current + amount));
        }

        public bool CanSpend(float amount)
        {
            return Current >= amount;
        }

        public bool TrySpend(float amount)
        {
            if (!CanSpend(amount)) return false;

            Current -= amount;
            return true;
        }

        public void Clear()
        {
            Current = 0f;
        }
    }
}
