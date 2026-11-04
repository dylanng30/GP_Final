using System;
using Game.Combat;
using Game.Combat.Domain;
using UnityEngine;

namespace Dylanng.Gameplay.RPG.Abilities.Effects
{
    /// <summary>Áp một trạng thái lên mục tiêu, qua CombatResolver để kích hoạt tương khắc.</summary>
    [Serializable]
    public class ApplyStatusEffect : IAbilityEffect
    {
        public StatusDefinitionSO Status;
        [Range(0f, 1f)] public float Chance = 1f;

        public void Execute(in AbilityContext context)
        {
            if (Status == null || context.Target == null || UnityEngine.Random.value > Chance) return;

            ServiceLocator.Get<CombatResolver>()?.ApplyStatus(context.Caster, context.Target, Status);
        }
    }
}
