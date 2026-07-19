using System;
using UnityEngine;
using Dylanng.Dylanng_EventBus;
using Dylanng.Gameplay.RPG.Abilities.Events;
using Dylanng.Gameplay.RPG.Stats;
using Dylanng.Core.Pooling;
using Dylanng.Services;

namespace Dylanng.Gameplay.RPG.Abilities.Effects
{
    [Serializable]
    public class LifestealAttackEffect : IAbilityEffect
    {
        public float DamageMultiplier = 1.0f;
        public float LifestealRatio = 0.5f; // Heals for 50% of damage dealt
        public ElementType Element = ElementType.Physical;
        public PoolableObject HitVFXPrefab;
        public PoolableObject HealVFXPrefab;

        public void Execute(GameObject caster, GameObject target, Vector3 point)
        {
            if (caster == null || target == null) return;
            if (target.GetComponent<Game.Gameplay.Entities.ITargetable>()?.IsDead == true) return;

            var casterStats = caster.GetComponent<StatController>();
            float atk = casterStats != null ? casterStats.ATK : 50f;
            float finalDamage = atk * DamageMultiplier;
            bool isCrit = UnityEngine.Random.value <= 0.2f;

            // Deal Damage
            EventBus.Publish(new DamageDealtEvent(caster, target, finalDamage, Element, target.transform.position, isCrit));

            // Heal Caster
            if (casterStats != null)
            {
                float healAmount = finalDamage * LifestealRatio;
                float currentHp = casterStats.GetStatValue(StatType.MaxHP);
                casterStats.InitializeStat(StatType.MaxHP, currentHp + healAmount);
                Debug.Log($"[LifestealAttackEffect] {caster.name} hút {healAmount} máu từ {target.name}.");
            }

            var poolManager = ServiceLocator.Get<PoolManager>();
            if (poolManager == null) return;

            // VFX on Target
            if (HitVFXPrefab != null)
            {
                string hitKey = HitVFXPrefab.name;
                poolManager.CreatePool(hitKey, HitVFXPrefab, 3);
                var hitVfx = poolManager.Spawn<PoolableObject>(hitKey, target.transform.position + Vector3.up, Quaternion.identity);
            }

            // VFX on Caster
            if (HealVFXPrefab != null)
            {
                string healKey = HealVFXPrefab.name;
                poolManager.CreatePool(healKey, HealVFXPrefab, 3);
                var healVfx = poolManager.Spawn<PoolableObject>(healKey, caster.transform.position, Quaternion.identity);
                if (healVfx != null)
                {
                    healVfx.transform.SetParent(caster.transform);
                }
            }
        }
    }
}
