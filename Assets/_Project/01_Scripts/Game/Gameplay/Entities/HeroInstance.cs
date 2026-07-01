using Dylanng.Core.Base;
using Game.Gameplay.Controllers;
using UnityEngine;

namespace Game.Gameplay.Entities
{
    [RequireComponent(typeof(CharacterController))]
    public class HeroInstance : MonoBehaviourBase, IEntityController
    {
        [Header("--- MOVEMENT SETTINGS ---")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _rotationSpeed = 15f;
        [SerializeField] private float _gravity = -15f;

        private CharacterController _characterController;
        private Transform _mainCameraTransform;

        private Vector2 _currentMoveInput;
        private float _velocityY;

        public string HeroId { get; private set; }

        private PlayerInputController _playerController;
        private HeroAIController _aiController;

        private void Awake()
        {
            _playerController = gameObject.AddComponent<PlayerInputController>();
            _aiController = gameObject.AddComponent<HeroAIController>();
            _characterController = GetComponent<CharacterController>();

            if (Camera.main != null)
            {
                _mainCameraTransform = Camera.main.transform;
            }
        }

        public void Setup(string heroId)
        {
            HeroId = heroId;
            _playerController.Initialize(this);
            _aiController.Initialize(this, this);

            // Default to AI
            SetAsActive(false);
        }

        public void SetAsActive(bool isActive)
        {
            if (isActive)
            {
                _playerController.enabled = true;
                _aiController.enabled = false;
            }
            else
            {
                _playerController.enabled = false;
                _aiController.enabled = true;
                _aiController.FollowActiveHero();
            }
        }

        private void Update()
        {
            if (_characterController == null) return;

            ApplyMovementAndGravity();
        }

        private void ApplyMovementAndGravity()
        {
            // 1. Calculate Camera-relative direction
            Vector3 moveDirection = Vector3.zero;
            
            if (_currentMoveInput.sqrMagnitude > 0.01f)
            {
                if (_mainCameraTransform != null)
                {
                    Vector3 cameraForward = _mainCameraTransform.forward;
                    Vector3 cameraRight = _mainCameraTransform.right;

                    cameraForward.y = 0f;
                    cameraRight.y = 0f;

                    cameraForward.Normalize();
                    cameraRight.Normalize();

                    moveDirection = cameraForward * _currentMoveInput.y + cameraRight * _currentMoveInput.x;
                }
                else
                {
                    // Fallback to world space if no camera
                    moveDirection = new Vector3(_currentMoveInput.x, 0f, _currentMoveInput.y);
                }

                // 2. Handle Rotation
                if (moveDirection != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
                }
            }

            // 3. Apply Gravity
            if (_characterController.isGrounded && _velocityY < 0)
            {
                _velocityY = -2f; // Small downward force to keep grounded
            }

            _velocityY += _gravity * Time.deltaTime;

            // 4. Final Velocity calculation
            Vector3 finalVelocity = moveDirection * _moveSpeed;
            finalVelocity.y = _velocityY;

            // 5. Move CharacterController
            _characterController.Move(finalVelocity * Time.deltaTime);
        }

        #region --- IEntityController ---
        public void Move(Vector2 direction)
        {
            _currentMoveInput = direction;
        }

        public void StopMove()
        {
            _currentMoveInput = Vector2.zero;
        }

        public void Attack()
        {
            // Debug.Log($"{HeroId} attacking");
        }

        public void UseSkill(int skillIndex)
        {
        }

        public void Jump()
        {

        }

        public void Dash()
        {

        }
        #endregion
    }
}
