using System;
using Dylanng.Gameplay.RPG.Abilities.Events;
using Dylanng.Gameplay.RPG.Stats;
using UnityEngine;

namespace Dylanng.Gameplay.RPG.Abilities.Effects
{
    [Serializable]
    public class DamageEffect : IAbilityEffect
    {
        public ElementType Element;
        public float BaseDamage = 100f;
        public float AtkMultiplier = 1.5f;

        [Header("--- Radius Falloff Settings ---")]
        public bool UseFalloff = false;
        public float FalloffRadius = 3f;
        public float MinDamagePercent = 0.3f;

        public void Execute(GameObject caster, GameObject target, Vector3 point)
        {
            if (target == null) return;

            float finalDamage = BaseDamage;
            if (caster != null && caster.TryGetComponent<StatController>(out var casterStats))
            {
                finalDamage += casterStats.ATK * AtkMultiplier;
            }

            if (UseFalloff && FalloffRadius > 0f)
            {
                float distance = Vector3.Distance(target.transform.position, point);
                float t = Mathf.Clamp01(distance / FalloffRadius);
                float falloffMultiplier = Mathf.Lerp(1.0f, MinDamagePercent, t);
                finalDamage *= falloffMultiplier;
            }

            // Phát sự kiện sát thương qua EventBus để cập nhật UI và hiển thị số nhảy
            bool isCrit = UnityEngine.Random.value <= 0.2f;
            EventBus.Publish(new DamageDealtEvent(caster, target, finalDamage, Element, point, isCrit));
            
            Debug.Log($"[DamageEffect] Gây {finalDamage} sát thương hệ {Element} lên {target.name} tại {point}");
        }
    }
}
