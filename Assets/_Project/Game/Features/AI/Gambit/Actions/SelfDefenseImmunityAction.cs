using System;
using Game.Gameplay.AI.Core;
using UnityEngine;

namespace Game.Gameplay.AI.Gambit.Actions
{
    [Serializable]
    public class SelfDefenseImmunityAction : IGambitAction
    {
        [Tooltip("Index of the defensive/invincibility skill to activate in EntityController.")]
        [SerializeField] private int defensiveSkillIndex = 0;

        public SelfDefenseImmunityAction()
        {
        }

        public SelfDefenseImmunityAction(int skillIndex)
        {
            defensiveSkillIndex = skillIndex;
        }

        public void Execute(IAIContext context)
        {
            if (context == null || context.EntityController == null) return;
            
            // Dừng ngay di chuyển hiện tại
            context.EntityController.StopMove();
            // Kích hoạt kỹ năng phòng thủ ngay lập tức
            context.EntityController.UseSkill(defensiveSkillIndex);
            
            // Cập nhật World State để GOAP biết Bot đang có Shield/Immunity
            var ws = context.WorldState;
            ws.SetState(AIWorldStateKey.HasShield, true);
            context.WorldState = ws;
        }
    }
}
