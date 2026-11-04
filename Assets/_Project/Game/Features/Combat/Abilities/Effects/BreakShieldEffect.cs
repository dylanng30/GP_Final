using Dylanng;
using System;
using Game.Combat;
using Game.Gameplay.Entities;
using UnityEngine;

namespace Game.Combat.Abilities.Effects
{
    /// <summary>Phá toàn bộ khiên của mục tiêu.</summary>
    [Serializable]
    public class BreakShieldEffect : IAbilityEffect
    {
        public void Execute(in AbilityContext context)
        {
            if (context.Target == null) return;

            context.Target.GetComponentInParent<ITargetable>()?.Health.Shields.Clear();
        }
    }
}
