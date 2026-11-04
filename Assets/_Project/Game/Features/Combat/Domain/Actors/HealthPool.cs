using System;

namespace Game.Combat.Domain
{
    public readonly struct DamageOutcome
    {
        public readonly float Absorbed;
        public readonly float HealthLost;
        public readonly bool IsLethal;

        public DamageOutcome(float absorbed, float healthLost, bool isLethal)
        {
            Absorbed = absorbed;
            HealthLost = healthLost;
            IsLethal = isLethal;
        }
    }

    /// <summary>Máu hiện tại của một actor, tách khỏi chỉ số MaxHP.</summary>
    public sealed class HealthPool
    {
        public event Action Changed;
        public event Action Died;

        public float Current { get; private set; }
        public float Max { get; private set; }
        public bool IsDead { get; private set; }
        public ShieldPool Shields { get; } = new ShieldPool();

        public float Ratio => Max > 0f ? Current / Max : 1f;

        /// <summary>Đặt lại đầy máu, xóa khiên và trạng thái chết.</summary>
        public void Restore(float max)
        {
            Max = Math.Max(0f, max);
            Current = Max;
            IsDead = false;
            Shields.Clear();
            Changed?.Invoke();
        }

        /// <summary>Đổi máu tối đa, giữ nguyên máu hiện tại trong giới hạn mới.</summary>
        public void SetMax(float max)
        {
            Max = Math.Max(0f, max);
            Current = Math.Min(Current, Max);
            Changed?.Invoke();
        }

        public DamageOutcome TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f) return default;

            float remaining = Shields.Absorb(amount);
            float healthLost = Math.Min(Current, remaining);
            Current -= healthLost;

            bool isLethal = Current <= 0f;
            IsDead = isLethal;

            Changed?.Invoke();
            if (isLethal) Died?.Invoke();

            return new DamageOutcome(amount - remaining, healthLost, isLethal);
        }

        /// <summary>Hồi máu và trả về lượng máu thực nhận.</summary>
        public float Heal(float amount)
        {
            if (IsDead || amount <= 0f) return 0f;

            float healed = Math.Min(amount, Max - Current);
            Current += healed;
            Changed?.Invoke();
            return healed;
        }
    }
}
