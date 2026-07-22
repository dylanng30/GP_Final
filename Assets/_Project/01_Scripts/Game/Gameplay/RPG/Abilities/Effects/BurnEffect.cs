using System;
using System.Collections;
using Dylanng.Dylanng_EventBus;
using Dylanng.Gameplay.RPG.Abilities.Events;
using UnityEngine;

namespace Dylanng.Gameplay.RPG.Abilities.Effects
{
    [Serializable]
    public class BurnEffect : IAbilityEffect
    {
        [Header("--- Burn Settings ---")]
        public float Duration = 5.0f;
        public float DamagePerTick = 20.0f;
        public float TickInterval = 1.0f;
        public ElementType Element = ElementType.Fire;

        public void Execute(GameObject caster, GameObject target, Vector3 point)
        {
            if (target == null) return;

            // Chạy Coroutine gây sát thương thiêu đốt trên đối tượng nhận hiệu ứng (hoặc caster nếu đối tượng không phải MonoBehaviour)
            var targetMono = target.GetComponent<MonoBehaviour>();
            if (targetMono != null && targetMono.isActiveAndEnabled)
            {
                targetMono.StartCoroutine(ApplyDotCoroutine(caster, target));
            }
        }

        private IEnumerator ApplyDotCoroutine(GameObject caster, GameObject target)
        {
            float elapsed = 0f;
            while (elapsed < Duration)
            {
                yield return new WaitForSeconds(TickInterval);
                if (target == null) break;

                // Gây sát thương theo từng Tick qua EventBus
                EventBus.Publish(new DamageDealtEvent(caster, target, DamagePerTick, Element, target.transform.position));
                elapsed += TickInterval;
            }
        }
    }
}
