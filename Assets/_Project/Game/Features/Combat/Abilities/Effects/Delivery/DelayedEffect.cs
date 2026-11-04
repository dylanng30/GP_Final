using System;
using System.Collections;
using System.Collections.Generic;
using Game.Combat;
using UnityEngine;

namespace Dylanng.Gameplay.RPG.Abilities.Effects
{
    /// <summary>Chạy các effect sau một khoảng trễ, dùng cho đòn có báo hiệu trước.</summary>
    [Serializable]
    public class DelayedEffect : IAbilityEffect
    {
        public float Delay = 0.8f;
        [SerializeReference] public List<IAbilityEffect> Effects = new List<IAbilityEffect>();

        public void Execute(in AbilityContext context)
        {
            if (context.Caster != null && context.Caster.TryGetComponent<MonoBehaviourBase>(out var owner))
            {
                owner.StartCoroutine(RunAfterDelay(context));
            }
        }

        private IEnumerator RunAfterDelay(AbilityContext context)
        {
            yield return new WaitForSeconds(Delay);

            foreach (IAbilityEffect effect in Effects)
            {
                effect?.Execute(context);
            }
        }
    }
}
