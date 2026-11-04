using System;
using Game.Gameplay.Controllers;
using UnityEngine;

namespace Game.Combat
{
    [Serializable]
    public struct MotorSettings
    {
        public float MoveSpeed;
        public float RotationSpeed;
        public float Gravity;
        public float JumpForce;
        public float DashDistance;
        public float DashDuration;
        public float DashInvincibilityDuration;
    }

    /// <summary>Di chuyển theo hướng camera, trọng lực, nhảy và dash của một CharacterController.</summary>
    public sealed class CharacterMotor
    {
        private const float MoveInputDeadZone = 0.01f;
        private const float GroundedVelocity = -2f;

        private readonly Transform _transform;
        private readonly CharacterController _controller;

        private MotorSettings _settings;
        private float _velocityY;
        private bool _isJumping;

        private Vector3 _dashDirection;
        private Vector3 _dashStart;
        private float _dashElapsed;
        private float _invincibleTimer;

        public Vector2 MoveInput { get; private set; }
        public bool IsDashing { get; private set; }
        public bool IsInvincible => _invincibleTimer > 0f;
        public bool IsGrounded => _controller != null && _controller.isGrounded;
        public float MoveMagnitude => IsDashing ? 0f : MoveInput.magnitude;

        public CharacterMotor(Transform transform, CharacterController controller, MotorSettings settings)
        {
            _transform = transform;
            _controller = controller;
            _settings = settings;
        }

        public void SetMoveSpeed(float moveSpeed)
        {
            _settings.MoveSpeed = moveSpeed;
        }

        public void SetMoveInput(Vector2 input)
        {
            MoveInput = input;
        }

        public bool TryJump()
        {
            if (!IsGrounded || _isJumping || IsDashing) return false;

            _isJumping = true;
            _velocityY = _settings.JumpForce;
            return true;
        }

        public bool TryDash()
        {
            if (IsDashing) return false;

            Vector3 inputDirection = ToWorldDirection(MoveInput);
            _dashDirection = inputDirection != Vector3.zero ? inputDirection.normalized : _transform.forward;
            _dashStart = _transform.position;
            _dashElapsed = 0f;
            _invincibleTimer = _settings.DashInvincibilityDuration;
            IsDashing = true;
            return true;
        }

        public void Tick(float deltaTime, bool canMove)
        {
            if (_invincibleTimer > 0f) _invincibleTimer -= deltaTime;

            ApplyGravity(deltaTime);

            if (IsDashing)
            {
                TickDash(deltaTime);
                return;
            }

            Vector3 direction = canMove ? ToWorldDirection(MoveInput) : Vector3.zero;
            RotateTowards(direction, deltaTime);

            Vector3 velocity = direction * _settings.MoveSpeed;
            velocity.y = _velocityY;
            _controller.Move(velocity * deltaTime);
        }

        private void ApplyGravity(float deltaTime)
        {
            if (_controller.isGrounded && _velocityY < 0f)
            {
                _velocityY = GroundedVelocity;
                _isJumping = false;
            }

            _velocityY += _settings.Gravity * deltaTime;
        }

        private void TickDash(float deltaTime)
        {
            _dashElapsed += deltaTime;
            float t = Mathf.Clamp01(_dashElapsed / _settings.DashDuration);

            // Ease-out: bắt đầu nhanh, chậm dần về cuối.
            float easedT = 1f - (1f - t) * (1f - t);
            Vector3 destination = _dashStart + _dashDirection * (_settings.DashDistance * easedT);
            destination.y = _transform.position.y;

            _controller.Move(destination - _transform.position);

            if (t >= 1f) IsDashing = false;
        }

        private void RotateTowards(Vector3 direction, float deltaTime)
        {
            if (direction == Vector3.zero) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            _transform.rotation = Quaternion.Slerp(
                _transform.rotation, targetRotation, _settings.RotationSpeed * deltaTime);
        }

        private static Vector3 ToWorldDirection(Vector2 input)
        {
            if (input.sqrMagnitude <= MoveInputDeadZone) return Vector3.zero;

            Transform view = ResolveViewTransform();
            if (view == null) return new Vector3(input.x, 0f, input.y);

            Vector3 forward = Flatten(view.forward, Vector3.forward);
            Vector3 right = Flatten(view.right, Vector3.right);
            return forward * input.y + right * input.x;
        }

        private static Transform ResolveViewTransform()
        {
            if (CameraController.Instance != null) return CameraController.Instance.transform;
            return Camera.main != null ? Camera.main.transform : null;
        }

        private static Vector3 Flatten(Vector3 direction, Vector3 fallback)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0.001f ? direction.normalized : fallback;
        }
    }
}
