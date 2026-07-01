using Dylanng.Core.State;
using Game.Gameplay.Controllers.States;
using Game.Gameplay.Entities;
using UnityEngine;

namespace Game.Gameplay.Controllers
{
    public class HeroAIController : MonoBehaviour
    {
        private StateMachine _stateMachine;
        private AIFollowState _followState;
        private AICombatState _combatState;

        public void Initialize(IEntityController entityController, HeroInstance heroInstance)
        {
            _stateMachine = new StateMachine();
            
            _followState = new AIFollowState(entityController, heroInstance);
            _combatState = new AICombatState(entityController, heroInstance);

            _stateMachine.Initialize(_followState);
        }

        private void Update()
        {
            _stateMachine?.Update();
        }

        public void SetTargetEnemy(Transform enemy)
        {
            _stateMachine.TransitionTo(_combatState);
        }

        public void FollowActiveHero()
        {
            _stateMachine.TransitionTo(_followState);
        }
    }
}
