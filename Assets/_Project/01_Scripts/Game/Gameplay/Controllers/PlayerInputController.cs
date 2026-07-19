using Dylanng.Core;
using Dylanng.Services;
using UnityEngine;

namespace Game.Gameplay.Controllers
{
    public class PlayerInputController : MonoBehaviour
    {
        private IEntityController _entityController;
        private InputManager _inputManager;
        private PlayerTargetingSystem _targetingSystem;

        public void Initialize(IEntityController entityController)
        {
            _entityController = entityController;
            _inputManager = ServiceLocator.Get<InputManager>();

            _targetingSystem = GetComponent<PlayerTargetingSystem>();
        }

        private void OnEnable()
        {
            if (_targetingSystem == null)
            {
                _targetingSystem = GetComponent<PlayerTargetingSystem>();
            }
            if (_targetingSystem != null)
            {
                _targetingSystem.enabled = true;
            }
        }

        private void OnDisable()
        {
            if (_targetingSystem != null)
            {
                _targetingSystem.ClearTarget();
                _targetingSystem.enabled = false;
            }
        }

        private void Update()
        {
            if (_inputManager == null || _entityController == null) return;

            // Handle Movement
            if (_inputManager.MovementInput.sqrMagnitude > 0.01f)
            {
                _entityController.Move(_inputManager.MovementInput);
            }
            else
            {
                var aiController = GetComponent<HeroAIController>();
                if (aiController == null || !aiController.IsInCombatState)
                {
                    _entityController.StopMove();
                }
            }

            // Handle Attack
            if (_inputManager.AttackInput)
            {
                _entityController.Attack();
            }

            // Handle Jump
            if (_inputManager.JumpInput)
            {
                _entityController.Jump();
            }
        }
    }
}
