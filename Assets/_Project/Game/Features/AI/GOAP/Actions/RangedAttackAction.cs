using Game.Gameplay.AI.Core;
using UnityEngine;

namespace Game.Gameplay.AI.GOAP.Actions
{
    /// <summary>
    /// Hành động tấn công tầm xa / bắn Hero (Ranged Attack / Spell Attack).
    /// </summary>
    [CreateAssetMenu(fileName = "Action_RangedAttack", menuName = "Dylanng/AI/GOAP/Actions/Ranged Attack")]
    public class RangedAttackAction : GOAPActionBase
    {
        [SerializeField] private float attackDuration = 1.2f;
        [Tooltip("Chỉ số skill cần dùng (nếu < 0 thì dùng đòn bắn thường Attack).")]
        [SerializeField] private int skillIndex = -1;
        private float _attackTimer;

        protected override void EnsureInitialized()
        {
            base.EnsureInitialized();
            if (_preconditionsMask.StateMask == 0)
            {
                _preconditionsMask.SetState(AIWorldStateKey.TargetInSpellRange, true);
                _preconditionsValues.SetState(AIWorldStateKey.TargetInSpellRange, true);
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
                
                if (skillIndex >= 0)
                {
                    if (context.EntityInstance != null)
                    {
                        context.EntityInstance.UseSkill(skillIndex, context.CurrentTarget.Transform.gameObject);
                    }
                    else
                    {
                        context.EntityController.UseSkill(skillIndex);
                    }
                }
                else
                {
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
        }

        public override ActionStatus OnUpdate(IAIContext context, float deltaTime)
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
