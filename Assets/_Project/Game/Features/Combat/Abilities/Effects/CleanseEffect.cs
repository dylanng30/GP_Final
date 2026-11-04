using Dylanng;
using System;
using Game.Combat;
using Game.Gameplay.Entities;
using UnityEngine;

namespace Game.Combat.Abilities.Effects
{
    /// <summary>Xóa mọi trạng thái trên mục tiêu.</summary>
    [Serializable]
    public class CleanseEffect : IAbilityEffect
    {
        public void Execute(in AbilityContext context)
        {
            if (context.Target == null) return;

            context.Target.GetComponentInParent<ITargetable>()?.Status.Set.Clear();
        }
    }
}
