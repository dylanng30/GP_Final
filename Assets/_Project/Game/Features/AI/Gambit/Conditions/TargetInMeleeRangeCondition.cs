using System;
using Game.Gameplay.AI.Core;
using UnityEngine;

namespace Game.Gameplay.AI.Gambit.Conditions
{
    [Serializable]
    public class TargetInMeleeRangeCondition : IGambitCondition
    {
        [SerializeField] private float meleeRange = 2.0f;

        public TargetInMeleeRangeCondition()
        {
        }

        public TargetInMeleeRangeCondition(float range)
        {
            meleeRange = range;
        }

        public bool Evaluate(IAIContext context)
        {
            if (context == null || context.CurrentTarget == null) return false;
            return context.DistanceToTarget <= meleeRange;
        }
    }
}
