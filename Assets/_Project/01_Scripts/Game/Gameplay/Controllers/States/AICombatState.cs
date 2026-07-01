using Dylanng.Core.State;
using Game.Gameplay.Entities;
using UnityEngine;

namespace Game.Gameplay.Controllers.States
{
    public class AICombatState : StateBase
    {
        private readonly IEntityController _entityController;
        private readonly HeroInstance _heroInstance;
        private Transform _targetEnemy;

        public AICombatState(IEntityController entityController, HeroInstance heroInstance)
        {
            _entityController = entityController;
            _heroInstance = heroInstance;
        }

        public override void Enter()
        {
            base.Enter();
            // Find nearest enemy logic
        }

        public override void Update()
        {
            base.Update();
            
            // Dummy combat logic
            if (_targetEnemy != null)
            {
                float distance = Vector3.Distance(_heroInstance.transform.position, _targetEnemy.position);
                if (distance > 1.5f)
                {
                    Vector3 direction = (_targetEnemy.position - _heroInstance.transform.position).normalized;
                    _entityController.Move(new Vector2(direction.x, direction.z));
                }
                else
                {
                    _entityController.StopMove();
                    _entityController.Attack();
                }
            }
            else
            {
                // Transition back to follow if no enemies
            }
        }
    }
}
