using Game.Gameplay.AI.Core;
using Game.Gameplay.Controllers;
using UnityEngine;

namespace Game.Gameplay.AI.GOAP.Actions
{
    [CreateAssetMenu(fileName = "Action_MoveToSpellRange", menuName = "Dylanng/AI/GOAP/Actions/Move To Spell Range")]
    public class MoveToSpellRangeAction : GOAPActionBase
    {
        [SerializeField] private float stopDistance = 6.5f;

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
                _effectsMask.SetState(AIWorldStateKey.TargetInSpellRange, true);
                _effectsValues.SetState(AIWorldStateKey.TargetInSpellRange, true);
            }
        }

        public override bool CheckProceduralPrecondition(IAIContext context)
        {
            if (context == null || context.CurrentTarget == null) return false;
            return context.DistanceToTarget > stopDistance;
        }

        public override float CalculateDynamicCost(IAIContext context)
        {
            if (context == null || context.CurrentTarget == null) return Cost;
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

        public override ActionStatus OnUpdate(IAIContext context, float deltaTime)
        {
            if (context == null || context.CurrentTarget == null)
            {
                return ActionStatus.Failure;
            }

            if (context.DistanceToTarget <= stopDistance)
            {
                context.EntityController.StopMove();
                
                var ws = context.WorldState;
                ws.SetState(AIWorldStateKey.TargetInSpellRange, true);
                context.WorldState = ws;

                return ActionStatus.Success;
            }

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
