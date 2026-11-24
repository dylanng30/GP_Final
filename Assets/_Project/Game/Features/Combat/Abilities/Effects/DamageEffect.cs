using Dylanng;
using Game.Combat;
using Game.Data.Hero;
using System;
using Game.Combat.Domain;
using Game.Combat.Stats;
using Game.Gameplay.Entities;
using UnityEngine;

namespace Game.Combat.Abilities.Effects
{
    [Serializable]
    public class DamageEffect : IAbilityEffect
    {
        public GameElement Element;
        public float BaseDamage = 100f;
        public float AtkMultiplier = 1.5f;

        [Header("--- Radius Falloff Settings ---")]
        public bool UseFalloff = false;
        public float FalloffRadius = 3f;
        public float MinDamagePercent = 0.3f;

        [Tooltip("Mục tiêu càng mất nhiều máu càng nhận thêm sát thương. 1 nghĩa là gấp đôi khi gần hết máu.")]
        public float MissingHealthBonus = 0f;

        public void Execute(in AbilityContext context)
        {
            var (caster, target, point) = context;
            if (target == null) return;

            float finalDamage = BaseDamage;
            if (caster != null && caster.TryGetComponent<StatController>(out var casterStats))
            {
                finalDamage += casterStats.ATK * AtkMultiplier;
            }

            finalDamage *= context.Potency;

            if (MissingHealthBonus > 0f && target.TryGetComponent<ITargetable>(out var targetable))
            {
                finalDamage *= AbilityScaling.MissingHealthMultiplier(targetable.Health.Ratio, MissingHealthBonus);
            }

            if (UseFalloff && FalloffRadius > 0f)
            {
                float distance = Vector3.Distance(target.transform.position, point);
                float t = Mathf.Clamp01(distance / FalloffRadius);
                float falloffMultiplier = Mathf.Lerp(1.0f, MinDamagePercent, t);
                finalDamage *= falloffMultiplier;
            }

            // Phát sự kiện sát thương qua EventBus để cập nhật UI và hiển thị số nhảy
            ServiceLocator.Get<CombatResolver>()?.Deal(caster, target, finalDamage, context.ResolveElement(Element), point);
            
        }
    }
}
