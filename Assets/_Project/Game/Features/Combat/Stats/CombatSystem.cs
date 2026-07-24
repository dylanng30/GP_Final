using UnityEngine;

namespace Dylanng.Gameplay.RPG.Stats
{
    public class CombatSystem : SystemBase
    {
        public override void Initialize()
        {
            Debug.Log("[CombatSystem] Initialized.");
        }

        /*public void ExecuteSkill(IDamageable target, CharacterStats attackerStats, SkillSO skill)
        {
            var calcService = ServiceLocator.Get<DamageCalculationService>();
            var targetGo = (target as MonoBehaviour)?.gameObject;
            
            if (targetGo != null && targetGo.TryGetComponent<CharacterStats>(out var defenderStats))
            {
                var result = calcService.CalculateDamage(attackerStats, defenderStats, skill);
                
                ApplyDamageInternal(target, result.FinalDamage, attackerStats.gameObject, result.IsCritical);
            }
        }

        public void ApplyDamage(IDamageable target, float rawDamage, GameObject attacker = null)
        {
            ApplyDamageInternal(target, rawDamage, attacker, false);
        }

        private void ApplyDamageInternal(IDamageable target, float finalDamage, GameObject attacker, bool isCrit)
        {
            var targetGo = (target as MonoBehaviour)?.gameObject;
            
            target.TakeDamage(finalDamage);
            
            // Publish Event
            EventBus.Publish<EntityDamagedEvent>(new EntityDamagedEvent(targetGo, attacker, finalDamage, isCrit));
            
            if (target.CurrentHealth <= 0)
            {
                EventBus.Publish<EntityDiedEvent>(new EntityDiedEvent(targetGo));
            }

            Debug.Log($"[CombatSystem] {attacker?.name} dealt {finalDamage} damage to {targetGo?.name}");
        }*/
    }
}
