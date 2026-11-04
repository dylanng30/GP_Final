using Game.Combat;
using Game.Data.Hero;
using System;
using System.Collections;
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
        public GameElement Element = GameElement.Fire;

        public void Execute(in AbilityContext context)
        {
            var (caster, target, point) = context;
            if (target == null) return;

            // Chạy Coroutine gây sát thương thiêu đốt trên đối tượng nhận hiệu ứng (hoặc caster nếu đối tượng không phải MonoBehaviour)
            var targetMono = target.GetComponent<MonoBehaviour>();
            if (targetMono != null && targetMono.isActiveAndEnabled)
            {
                targetMono.StartCoroutine(ApplyDotCoroutine(caster, target, context.ResolveElement(Element)));
            }
        }

        private IEnumerator ApplyDotCoroutine(GameObject caster, GameObject target, GameElement element)
        {
            float elapsed = 0f;
            while (elapsed < Duration)
            {
                yield return new WaitForSeconds(TickInterval);
                if (target == null) break;

                // Gây sát thương theo từng Tick qua EventBus
                ServiceLocator.Get<CombatResolver>()?.Deal(caster, target, DamagePerTick, element, target.transform.position, canCrit: false);
                elapsed += TickInterval;
            }
        }
    }
}
