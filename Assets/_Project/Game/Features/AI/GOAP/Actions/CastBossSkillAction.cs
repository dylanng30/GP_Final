using Game.Gameplay.AI.Core;
using UnityEngine;

namespace Game.Gameplay.AI.GOAP.Actions
{
    /// <summary>
    /// Hành động thi triển kỹ năng đặc biệt của Boss (có kiểm tra Cooldown và cast duration).
    /// </summary>
    [CreateAssetMenu(fileName = "Action_CastBossSkill", menuName = "Dylanng/AI/GOAP/Actions/Cast Boss Skill")]
    public class CastBossSkillAction : GOAPActionBase
    {
        [SerializeField] private int skillIndex = 1;
        [SerializeField] private float castDuration = 2.0f;
        [SerializeField] private float skillCooldown = 8.0f;
        [SerializeField] private AIWorldStateKey skillReadyKey = AIWorldStateKey.CanUseBossSkill1;

        private float _castTimer;

        public override bool CheckProceduralPrecondition(IAIContext context)
        {
            if (context == null || !context.CanCastBossSkill(skillIndex)) return false;
            return context.WorldState.HasState(skillReadyKey);
        }

        public override void Enter(IAIContext context)
        {
            base.Enter(context);
            _castTimer = 0f;
            if (context != null)
            {
                context.EntityController?.StopMove();
                context.EntityController?.UseSkill(skillIndex);
                context.StartBossSkillCooldown(skillIndex, skillCooldown);

                var ws = context.WorldState;
                ws.SetState(skillReadyKey, false);
                ws.SetState(AIWorldStateKey.IsCasting, true);
                context.WorldState = ws;
            }
        }

        public override ActionStatus OnUpdate(IAIContext context, float deltaTime)
        {
            if (context == null) return ActionStatus.Failure;

            _castTimer += deltaTime;
            if (_castTimer >= castDuration)
            {
                var ws = context.WorldState;
                ws.SetState(AIWorldStateKey.IsCasting, false);
                context.WorldState = ws;

                return ActionStatus.Success;
            }

            return ActionStatus.Running;
        }

        public override void Exit(IAIContext context)
        {
            base.Exit(context);
            if (context != null)
            {
                var ws = context.WorldState;
                ws.SetState(AIWorldStateKey.IsCasting, false);
                context.WorldState = ws;
            }
        }
    }
}
