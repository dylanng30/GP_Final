using UnityEngine;

namespace Dylanng.Gameplay.RPG.Inventory
{
    public static class EquipmentStatCalculator
    {
        private const float LEVEL_BONUS_RATE = 0.1f; // Tăng 10% mỗi cấp

        public static float GetScaledBonus(float baseBonus, int itemLevel)
        {
            if (baseBonus <= 0) return 0f;
            return (float)System.Math.Round(baseBonus * (1f + LEVEL_BONUS_RATE * Mathf.Max(0, itemLevel - 1)), 2);
        }
    }
}
