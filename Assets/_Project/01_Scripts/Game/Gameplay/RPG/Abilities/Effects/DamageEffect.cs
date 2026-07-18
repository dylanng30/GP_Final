using System;
using Dylanng.Dylanng_EventBus;
using Dylanng.Gameplay.RPG.Abilities.Events;
using Dylanng.Gameplay.RPG.Stats;
using UnityEngine;

namespace Dylanng.Gameplay.RPG.Abilities.Effects
{
    [Serializable]
    public class DamageEffect : IAbilityEffect
    {
        public ElementType Element = ElementType.Pyro;
        public float BaseDamage = 100f;
        public float AtkMultiplier = 1.5f;

        public void Execute(GameObject caster, GameObject target, Vector3 point)
        {
            if (target == null) return;

            float finalDamage = BaseDamage;
            if (caster != null && caster.TryGetComponent<StatController>(out var casterStats))
            {
                finalDamage += casterStats.ATK * AtkMultiplier;
            }

            // Phát sự kiện sát thương qua EventBus để cập nhật UI và hiển thị số nhảy
            bool isCrit = UnityEngine.Random.value <= 0.2f;
            EventBus.Publish(new DamageDealtEvent(caster, target, finalDamage, Element, point, isCrit));
            
            Debug.Log($"[DamageEffect] Gây {finalDamage} sát thương hệ {Element} lên {target.name} tại {point}");
        }
    }
}
