using Dylanng.Gameplay.RPG.Stats;
using UnityEngine;

namespace Dylanng.Gameplay.RPG.Combat
{
    public struct DamageResult
    {
        public float FinalDamage;
        public bool IsCritical;
    }

    public class DamageCalculationService : ServiceBase
    {
        public override void Initialize()
        {
            Debug.Log("[DamageCalculationService] Initialized.");
        }

        public DamageResult CalculateDamage(CharacterStats attacker, CharacterStats defender, SkillSO skill = null)
        {
            float damage = attacker.ATK;
            
            if (skill != null)
            {
                damage *= skill.DamageMultiplier;
            }

            // Simple mitigation: Damage / (1 + Def/100) or similar. Let's use simple subtraction for now but bounded.
            float mitigation = defender.DEF;
            float finalDamage = Mathf.Max(1, damage - mitigation);

            // Simple crit logic
            bool isCrit = Random.value > 0.9f; // 10% chance
            if (isCrit) finalDamage *= 1.5f;

            return new DamageResult
            {
                FinalDamage = finalDamage,
                IsCritical = isCrit
            };
        }
    }
}
