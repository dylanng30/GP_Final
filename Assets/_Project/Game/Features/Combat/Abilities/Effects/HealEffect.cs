using System;
using Dylanng.Gameplay.RPG.Stats;
using Game.Combat;
using UnityEngine;

namespace Dylanng.Gameplay.RPG.Abilities.Effects
{
    [Serializable]
    public class HealEffect : IAbilityEffect
    {
        private const float FallbackAttack = 50f;

        public float BaseHeal = 50f;
        public float HealScalingMultiplier = 1.0f; // Multiplier on ATK
        public PoolableObject HealVFXPrefab;
        public bool HealSelf = false;

        public void Execute(in AbilityContext context)
        {
            GameObject caster = context.Caster;
            if (caster == null) return;

            GameObject healTarget = HealSelf || context.Target == null ? caster : context.Target;

            var casterStats = caster.GetComponent<StatController>();
            float attack = casterStats != null ? casterStats.ATK : FallbackAttack;
            float healAmount = BaseHeal + attack * HealScalingMultiplier;

            ServiceLocator.Get<CombatResolver>()?.Heal(caster, healTarget, healAmount);
            SpawnVfx(healTarget.transform);
        }

        private void SpawnVfx(Transform target)
        {
            var poolManager = ServiceLocator.Get<PoolManager>();
            if (HealVFXPrefab == null || poolManager == null) return;

            string vfxKey = HealVFXPrefab.name;
            poolManager.CreatePool(vfxKey, HealVFXPrefab, 3);

            var vfx = poolManager.Spawn<PoolableObject>(vfxKey, target.position, Quaternion.identity);
            if (vfx != null) vfx.transform.SetParent(target);
        }
    }
}
