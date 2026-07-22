using System;
using Game.Gameplay.AI.Core;
using Game.Gameplay.Controllers;
using UnityEngine;

namespace Game.Gameplay.AI.Gambit.Actions
{
    /// <summary>
    /// Hành động Gambit phản xạ: Lập tức ngắt đòn và di chuyển quay về điểm Spawn ban đầu.
    /// </summary>
    [Serializable]
    public class ReturnToSpawnGambitAction : IGambitAction
    {
        [Tooltip("Khoảng cách tối thiểu tới Spawn để dừng di chuyển.")]
        [SerializeField] private float stopDistance = 0.5f;

        public ReturnToSpawnGambitAction()
        {
        }

        public ReturnToSpawnGambitAction(float stopDist)
        {
            stopDistance = stopDist;
        }

        public void Execute(IAIContext context)
        {
            if (context == null || context.EntityController == null) return;

            if (context.DistanceToSpawn <= stopDistance)
            {
                context.EntityController.StopMove();
                var ws = context.WorldState;
                ws.SetState(AIWorldStateKey.IsAtSpawn, true);
                context.WorldState = ws;
            }
            else
            {
                Vector3 dir = (context.SpawnPosition - context.Position).normalized;
                context.EntityController.MoveInWorldSpace(dir);
            }
        }
    }
}
