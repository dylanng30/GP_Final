using UnityEngine;

namespace Game.Gameplay.Controllers
{
    public interface IEntityController
    {
        void Move(Vector2 direction);
        void StopMove();
        void Attack();
        void UseSkill(int skillIndex);
        void Jump();
        void Dash();
    }
}
