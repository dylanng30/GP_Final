using Game.Gameplay.AI.Core;
using UnityEngine;

namespace Game.Gameplay.AI.GOAP.Actions
{
    /// <summary>
    /// Hành động tấn công cận chiến cơ bản lên mục tiêu đang ở trong tầm đòn.
    /// </summary>
    [CreateAssetMenu(fileName = "Action_MeleeAttack", menuName = "Dylanng/AI/GOAP/Actions/Melee Attack")]
    public class MeleeAttackAction : GOAPActionBase
    {
        [SerializeField] private float attackDuration = 1.0f;
        private float _attackTimer;

        protected override void EnsureInitialized()
        {
            base.EnsureInitialized();
            if (_preconditionsMask.StateMask == 0)
            {
                _preconditionsMask.SetState(AIWorldStateKey.TargetInMeleeRange, true);
                _preconditionsValues.SetState(AIWorldStateKey.TargetInMeleeRange, true);
            }
            if (_effectsMask.StateMask == 0)
            {
                _effectsMask.SetState(AIWorldStateKey.TargetIsDead, true);
                _effectsValues.SetState(AIWorldStateKey.TargetIsDead, true);
            }
        }

        public override bool CheckProceduralPrecondition(IAIContext context)
        {
            return context != null && context.CurrentTarget != null && !context.CurrentTarget.IsDead;
        }

        public override void Enter(IAIContext context)
        {
            base.Enter(context);
            _attackTimer = 0f;
            if (context?.EntityController != null)
            {
                context.EntityController.StopMove();
                if (context.EntityInstance != null)
                {
                    context.EntityInstance.Attack(context.CurrentTarget);
                }
                else
                {
                    context.EntityController.Attack();
                }
            }
        }

        public override ActionStatus Update(IAIContext context, float deltaTime)
        {
            if (context == null || context.CurrentTarget == null)
            {
                return ActionStatus.Failure;
            }

            _attackTimer += deltaTime;
            if (_attackTimer >= attackDuration)
            {
                return ActionStatus.Success;
            }

            return ActionStatus.Running;
        }
    }
}
