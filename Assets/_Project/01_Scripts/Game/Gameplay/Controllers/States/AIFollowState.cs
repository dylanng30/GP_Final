using Dylanng.Core;
using Dylanng.Core.State;
using Game.Events;
using Game.Gameplay.Entities;
using Game.Managers.Party;
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
            
            EventBus.Subscribe<ActiveHeroChangedEvent>(OnActiveHeroChanged);

            RefreshTargetToFollow();
        }

        public override void Exit()
        {
            EventBus.Unsubscribe<ActiveHeroChangedEvent>(OnActiveHeroChanged);
        }

        public override void Update()
        {
            base.Update();
            
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

        private void OnActiveHeroChanged(ActiveHeroChangedEvent evt)
        {
            RefreshTargetToFollow();
        }

        private void RefreshTargetToFollow()
        {
            _targetToFollow = ServiceLocator.Get<PartyCombatManager>().GetActiveHero()?.transform;
        }
    }
}
