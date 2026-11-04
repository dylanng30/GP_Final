using Game.Combat;
using System;
using System.Collections;
using Dylanng.Gameplay.RPG.Stats;
using UnityEngine;

namespace Dylanng.Gameplay.RPG.Abilities.Effects
{
    [Serializable]
    public class StatusEffect : IAbilityEffect
    {
        public StatType TargetStat = StatType.Speed;
        public float ModifierValue = -50f;
        public StatModType ModType = StatModType.PercentAdd;
        public float Duration = 3.0f;
        [Range(0f, 1f)] public float Chance = 1.0f;

        public void Execute(in AbilityContext context)
        {
            var (caster, target, point) = context;
            if (target == null || UnityEngine.Random.value > Chance) return;

            if (target.TryGetComponent<StatController>(out var targetStats))
            {
                var mod = new StatModifier(ModifierValue, ModType, this);
                targetStats.AddModifier(TargetStat, mod);
                Debug.Log($"[StatusEffect] Áp dụng modifier {ModifierValue} ({ModType}) lên {TargetStat} của {target.name} trong {Duration}s.");

                if (Duration > 0f)
                {
                    targetStats.StartCoroutine(RemoveAfterDurationCoroutine(targetStats, TargetStat, mod, Duration));
                }
            }
        }

        private IEnumerator RemoveAfterDurationCoroutine(StatController stats, StatType statType, StatModifier mod, float time)
        {
            yield return new WaitForSeconds(time);
            if (stats != null)
            {
                stats.RemoveModifier(statType, mod);
                Debug.Log($"[StatusEffect] Đã gỡ modifier trên {statType} của {stats.name} sau {time}s.");
            }
        }
    }
}
