using Dylanng.Core;
using Dylanng.Core.State;
using Dylanng.Services;
using Game.Gameplay.Controllers.States;
using Game.Gameplay.Entities;
using Game.Managers.Party;
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
            var partyManager = ServiceLocator.Get<PartyCombatManager>();
            var inputManager = ServiceLocator.Get<InputManager>();
            
            if (partyManager != null)
            {
                // Teleport logic: Teleport far away heroes back near the active hero (regardless of toggle state)
                var activeHero = partyManager.GetActiveHero();
                if (activeHero != null && activeHero.transform != transform)
                {
                    float distance = Vector3.Distance(transform.position, activeHero.transform.position);
                    if (distance > 15.0f)
                    {
                        var cc = GetComponent<CharacterController>();
                        if (cc != null) cc.enabled = false;
                        
                        Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * 2f;
                        randomOffset.y = 0;
                        transform.position = activeHero.transform.position + randomOffset;
                        
                        if (cc != null) cc.enabled = true;
                        
                        GetComponent<IEntityController>()?.StopMove();
                    }
                }

                // Force follow mode if toggle is enabled and player is moving
                if (inputManager != null && partyManager.IsFollowModeEnabled && inputManager.MovementInput.sqrMagnitude > 0.01f)
                {
                    if (_stateMachine.CurrentState != _followState)
                    {
                        FollowActiveHero();
                    }
                }
            }

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
