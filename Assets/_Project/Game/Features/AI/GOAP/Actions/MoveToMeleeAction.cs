using Game.Gameplay.AI.Core;
using Game.Gameplay.Controllers;
using UnityEngine;

namespace Game.Gameplay.AI.GOAP.Actions
{
    [CreateAssetMenu(fileName = "Action_MoveToMelee", menuName = "Dylanng/AI/GOAP/Actions/Move To Melee")]
    public class MoveToMeleeAction : GOAPActionBase
    {
        [SerializeField] private float stopDistance = 1.8f;

        protected override void EnsureInitialized()
        {
            base.EnsureInitialized();
            if (_preconditionsMask.StateMask == 0)
            {
                _preconditionsMask.SetState(AIWorldStateKey.TargetExists, true);
                _preconditionsValues.SetState(AIWorldStateKey.TargetExists, true);
            }
            if (_effectsMask.StateMask == 0)
            {
                _effectsMask.SetState(AIWorldStateKey.TargetInMeleeRange, true);
                _effectsValues.SetState(AIWorldStateKey.TargetInMeleeRange, true);
            }
        }

        public override bool CheckProceduralPrecondition(IAIContext context)
        {
            if (context == null || context.CurrentTarget == null) return false;
            // Chỉ cần di chuyển nếu đang ở xa hơn khoảng cách dừng
            return context.DistanceToTarget > stopDistance;
        }

        public override float CalculateDynamicCost(IAIContext context)
        {
            if (context == null || context.CurrentTarget == null) return Cost;
            // Khoảng cách càng xa thì chi phí hành vi di chuyển càng tăng nhẹ
            return Cost + (context.DistanceToTarget * 0.1f);
        }

        public override void Enter(IAIContext context)
        {
            base.Enter(context);
            if (context != null && context.EntityController != null && context.CurrentTarget != null)
            {
                Vector3 dir = (context.TargetPosition - context.Position).normalized;
                context.EntityController.MoveInWorldSpace(dir);
            }
        }

        public override ActionStatus Update(IAIContext context, float deltaTime)
        {
            if (context == null || context.CurrentTarget == null)
            {
                return ActionStatus.Failure;
            }

            if (context.DistanceToTarget <= stopDistance)
            {
                context.EntityController.StopMove();
                
                // Cập nhật lại world state khi đã tiếp cận thành công
                var ws = context.WorldState;
                ws.SetState(AIWorldStateKey.TargetInMeleeRange, true);
                context.WorldState = ws;

                return ActionStatus.Success;
            }

            // Tiếp tục cập nhật hướng di chuyển theo mục tiêu di động
            Vector3 dir = (context.TargetPosition - context.Position).normalized;
            context.EntityController.MoveInWorldSpace(dir);
            
            return ActionStatus.Running;
        }

        public override void Exit(IAIContext context)
        {
            base.Exit(context);
            context?.EntityController?.StopMove();
        }
    }
}
