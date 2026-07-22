using Game.Gameplay.AI.Core;
using Game.Gameplay.Controllers;
using UnityEngine;

namespace Game.Gameplay.AI.GOAP.Actions
{
    /// <summary>
    /// Hành động di chuyển quay về điểm Spawn ban đầu khi Bot không còn mục tiêu tấn công hoặc bị lôi đi quá xa.
    /// </summary>
    [CreateAssetMenu(fileName = "Action_ReturnToSpawn", menuName = "Dylanng/AI/GOAP/Actions/Return To Spawn")]
    public class ReturnToSpawnAction : GOAPActionBase
    {
        [SerializeField] private float stopDistance = 0.5f;

        protected override void EnsureInitialized()
        {
            base.EnsureInitialized();
            if (_preconditionsMask.StateMask == 0)
            {
                _preconditionsMask.SetState(AIWorldStateKey.TargetExists, true);
                _preconditionsValues.SetState(AIWorldStateKey.TargetExists, false);
            }
            if (_effectsMask.StateMask == 0)
            {
                _effectsMask.SetState(AIWorldStateKey.IsAtSpawn, true);
                _effectsValues.SetState(AIWorldStateKey.IsAtSpawn, true);
            }
        }

        public override bool CheckProceduralPrecondition(IAIContext context)
        {
            if (context == null || context.CurrentTarget != null) return false;
            return context.DistanceToSpawn > stopDistance;
        }

        public override float CalculateDynamicCost(IAIContext context)
        {
            if (context == null) return Cost;
            return Cost + (context.DistanceToSpawn * 0.05f);
        }

        public override void Enter(IAIContext context)
        {
            base.Enter(context);
            if (context != null && context.EntityController != null)
            {
                Vector3 dir = (context.SpawnPosition - context.Position).normalized;
                context.EntityController.MoveInWorldSpace(dir);
            }
        }

        public override ActionStatus Update(IAIContext context, float deltaTime)
        {
            if (context == null)
            {
                return ActionStatus.Failure;
            }

            // Nếu đã về tới sát điểm Spawn
            if (context.DistanceToSpawn <= stopDistance)
            {
                context.EntityController?.StopMove();
                var ws = context.WorldState;
                ws.SetState(AIWorldStateKey.IsAtSpawn, true);
                context.WorldState = ws;

                return ActionStatus.Success;
            }

            // Tiếp tục hướng về điểm Spawn
            Vector3 dir = (context.SpawnPosition - context.Position).normalized;
            context.EntityController?.MoveInWorldSpace(dir);

            return ActionStatus.Running;
        }

        public override void Exit(IAIContext context)
        {
            base.Exit(context);
            context?.EntityController?.StopMove();
        }
    }
}
