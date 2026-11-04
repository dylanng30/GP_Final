using Dylanng;
using System;
using Game.Combat;
using Game.Combat.Domain;
using UnityEngine;

namespace Game.Combat.Abilities.Effects
{
    /// <summary>Áp một trạng thái lên mục tiêu, qua CombatResolver để kích hoạt tương khắc.</summary>
    [Serializable]
    public class ApplyStatusEffect : IAbilityEffect
    {
        public StatusDefinitionSO Status;
        [Range(0f, 1f)] public float Chance = 1f;
        [Tooltip("Áp lên người thi triển thay vì mục tiêu")]
        public bool ApplyToCaster = false;

        public void Execute(in AbilityContext context)
        {
            GameObject receiver = ApplyToCaster ? context.Caster : context.Target;
            if (Status == null || receiver == null || UnityEngine.Random.value > Chance) return;

            ServiceLocator.Get<CombatResolver>()?.ApplyStatus(context.Caster, receiver, Status);
        }
    }
}
