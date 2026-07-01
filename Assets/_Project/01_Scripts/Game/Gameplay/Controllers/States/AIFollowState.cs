using Dylanng.Core.State;
using Game.Gameplay.Entities;
using UnityEngine;

namespace Game.Gameplay.Controllers.States
{
    public class AIFollowState : StateBase
    {
        private readonly IEntityController _entityController;
        private readonly HeroInstance _heroInstance;
        private Transform _targetToFollow;
        
        public AIFollowState(IEntityController entityController, HeroInstance heroInstance)
        {
            _entityController = entityController;
            _heroInstance = heroInstance;
        }

        public override void Enter()
        {
            base.Enter();
            // TODO: Get active hero transform from PartyCombatManager to set as _targetToFollow
            // _targetToFollow = ServiceLocator.Get<PartyCombatManager>().GetActiveHero()?.transform;
        }

        public override void Update()
        {
            base.Update();
            
            // Dummy follow logic
            if (_targetToFollow != null)
            {
                float distance = Vector3.Distance(_heroInstance.transform.position, _targetToFollow.position);
                if (distance > 2.0f)
                {
                    Vector3 direction = (_targetToFollow.position - _heroInstance.transform.position).normalized;
                    _entityController.Move(new Vector2(direction.x, direction.z));
                }
                else
                {
                    _entityController.StopMove();
                }
            }
        }
    }
}
