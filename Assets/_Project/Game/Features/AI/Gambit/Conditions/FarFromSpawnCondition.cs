using System;
using Game.Gameplay.AI.Core;
using UnityEngine;

namespace Game.Gameplay.AI.Gambit.Conditions
{
    /// <summary>
    /// Điều kiện Gambit: Kiểm tra xem Bot đã đi xa khỏi điểm Spawn ban đầu quá ngưỡng cho phép hay chưa.
    /// </summary>
    [Serializable]
    public class FarFromSpawnCondition : IGambitCondition
    {
        [Tooltip("Khoảng cách tối đa cách điểm Spawn để coi là đi quá xa.")]
        [SerializeField] private float thresholdDistance = 15.0f;

        public FarFromSpawnCondition()
        {
        }

        public FarFromSpawnCondition(float threshold)
        {
            thresholdDistance = threshold;
        }

        public bool Evaluate(IAIContext context)
        {
            if (context == null) return false;
            return context.DistanceToSpawn > thresholdDistance;
        }
    }
}
