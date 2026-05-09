using Dylanng.Core.Base;
using UnityEngine;

namespace Dylanng.Core.Commands
{
    public class UseSkillCommand : CommandBase
    {
        private string _skillId;
        private MonoBehaviourBase _user;
        private object _skillData; 

        public UseSkillCommand(MonoBehaviourBase user, string skillId, object skillData = null)
        {
            _user = user;
            _skillId = skillId;
            _skillData = skillData;
        }

        public override void Execute()
        {
            Debug.Log($"{_user.name} is using skill: {_skillId}");
            // Thực hiện logic skill ở đây (ví dụ: kích hoạt hiệu ứng, trừ mana, v.v.)
        }

        public override void Undo()
        {
            Debug.Log($"Undoing skill: {_skillId}");
            // Hoàn tác skill (nếu có thể)
        }
    }
}
