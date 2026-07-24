using System;
using UnityEngine;
using Dylanng.Gameplay.RPG.Stats;

namespace Dylanng.Gameplay.RPG.Abilities.Effects
{
    [Serializable]
    public class HealEffect : IAbilityEffect
    {
        public float BaseHeal = 50f;
        public float HealScalingMultiplier = 1.0f; // Multiplier on ATK
        public PoolableObject HealVFXPrefab;
        public bool HealSelf = false;

        public void Execute(GameObject caster, GameObject target, Vector3 point)
        {
            if (caster == null) return;

            GameObject targetToHeal = target;
            if (HealSelf || targetToHeal == null)
            {
                targetToHeal = caster;
            }

            var stats = targetToHeal.GetComponent<StatController>();
            if (stats == null) return;

            // Calculate heal amount based on Caster's ATK
            float casterAtk = 50f;
            var casterStats = caster.GetComponent<StatController>();
            if (casterStats != null)
            {
                casterAtk = casterStats.ATK;
            }

            float finalHeal = BaseHeal + (casterAtk * HealScalingMultiplier);

            // Assuming MaxHP acts as current HP for now. 
            // TODO: Change to CurrentHP when a dedicated health manager is implemented.
            float currentHp = stats.GetStatValue(StatType.MaxHP);
            stats.InitializeStat(StatType.MaxHP, currentHp + finalHeal);

            Debug.Log($"[HealEffect] {caster.name} hồi {finalHeal} máu cho {targetToHeal.name}.");

            // Spawn VFX
            if (HealVFXPrefab != null)
            {
                var poolManager = ServiceLocator.Get<PoolManager>();
                string vfxKey = HealVFXPrefab.name;
                
                if (poolManager != null)
                {
                    poolManager.CreatePool(vfxKey, HealVFXPrefab, 3);
                    var vfx = poolManager.Spawn<PoolableObject>(vfxKey, targetToHeal.transform.position, Quaternion.identity);
                    if (vfx != null)
                    {
                        vfx.transform.SetParent(targetToHeal.transform);
                        // VFX should ideally auto-despawn itself via its own script
                    }
                }
            }
        }
    }
}
