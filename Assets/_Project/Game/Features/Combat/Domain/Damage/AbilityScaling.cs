using System;

namespace Game.Combat.Domain
{
    /// <summary>Các hệ số nhân sát thương của skill, tách khỏi Unity để test được.</summary>
    public static class AbilityScaling
    {
        /// <summary>
        /// Hệ số của lần trúng thứ hitIndex trong chuỗi nảy, bắt đầu từ 0.
        /// Mỗi lần nảy giảm falloffPerBounce so với sát thương gốc, không xuống dưới minimumPotency.
        /// </summary>
        public static float ChainPotency(int hitIndex, float falloffPerBounce, float minimumPotency)
        {
            if (hitIndex <= 0) return 1f;

            float potency = 1f - Math.Max(0f, falloffPerBounce) * hitIndex;
            return Math.Max(Math.Max(0f, minimumPotency), potency);
        }

        /// <summary>
        /// Tăng sát thương theo lượng máu đã mất của mục tiêu.
        /// healthRatio = 1 thì không tăng, healthRatio = 0 thì tăng đủ maxBonus.
        /// </summary>
        public static float MissingHealthMultiplier(float healthRatio, float maxBonus)
        {
            float missing = 1f - Math.Min(1f, Math.Max(0f, healthRatio));
            return 1f + Math.Max(0f, maxBonus) * missing;
        }
    }
}
