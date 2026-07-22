using System;
using Game.Gameplay.AI.Core;
using UnityEngine;

namespace Game.Gameplay.AI.Gambit.Conditions
{
    [Serializable]
    public class HealthBelowThresholdCondition : IGambitCondition
    {
        [Range(0f, 1f)]
        [SerializeField] private float healthThreshold = 0.25f;

        public float HealthThreshold => healthThreshold;

        public HealthBelowThresholdCondition()
        {
        }

        public HealthBelowThresholdCondition(float threshold)
        {
            healthThreshold = threshold;
        }

        public bool Evaluate(IAIContext context)
        {
            if (context == null) return false;
            return context.HealthPercentage <= healthThreshold;
        }
    }
}
