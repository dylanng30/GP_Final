using System;
using Game.Gameplay.AI.Core;
using UnityEngine;

namespace Game.Gameplay.AI.Gambit.Conditions
{
    [Serializable]
    public class TargetInSpellRangeCondition : IGambitCondition
    {
        [SerializeField] private float spellRange = 8.0f;

        public TargetInSpellRangeCondition()
        {
        }

        public TargetInSpellRangeCondition(float range)
        {
            spellRange = range;
        }

        public bool Evaluate(IAIContext context)
        {
            if (context == null || context.CurrentTarget == null) return false;
            return context.DistanceToTarget <= spellRange;
        }
    }
}
