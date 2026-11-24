using UnityEngine;

namespace Game.Animation.Hero
{
    /// <summary>
    /// View bridge duy nhất có quyền giao tiếp với Unity Animator.
    /// Đặt component này lên Hero Prefab cùng với Animator.
    /// HeroInstance KHÔNG được gọi Animator trực tiếp — luôn dùng class này.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class HeroAnimatorController : MonoBehaviour
    {
        // ─── Internal state ───────────────────────────────────────────────────
        [SerializeField] private HeroAnimationConfig config;
        [SerializeField] private Animator animator;
        [SerializeField] private CharacterController characterController;

        private bool _isInitialized;
        private bool _wasGrounded = true;

        // Smoothed speed value để blend tree không bị jittery
        private float _currentSpeed;
        private const float SpeedSmoothTime = 0.1f;
        private float _speedSmoothVelocity;

        // ─── Setup ────────────────────────────────────────────────────────────

        private void Start()
        {
            if (config != null && !_isInitialized)
            {
                Setup(config);
            }
        }

        /// <summary>
        /// Gọi từ HeroInstance.Setup() sau khi HeroId đã biết.
        /// </summary>
        public void Setup(HeroAnimationConfig config)
        {
            if (config == null)
            {
                Debug.LogWarning(
                    $"[HeroAnimatorController] {name}: Không có HeroAnimationConfig, animation sẽ không hoạt động.");
                return;
            }

            this.config = config;
            this.config.InitializeHashes();
            
            _isInitialized = true;
        }

        // ─── Update loop ──────────────────────────────────────────────────────

        private void Update()
        {
            if (!_isInitialized || animator == null || config == null) return;

            UpdateGroundedState();
        }

        private void UpdateGroundedState()
        {
            if (characterController == null) return;

            bool isGrounded = characterController.isGrounded;
            animator.SetBool(config.IsGroundedHash, isGrounded);

            // Phát hiện Landed: frame trước đang bay, giờ chạm đất
            if (!_wasGrounded && isGrounded)
            {
                animator.SetTrigger(config.LandHash);
            }

            // Falling: không grounded và đang rơi xuống
            bool isFalling = !isGrounded;
            animator.SetBool(config.IsFallingHash, isFalling);

            _wasGrounded = isGrounded;
        }

        // ─── Movement ─────────────────────────────────────────────────────────

        /// <summary>
        /// Gọi mỗi frame từ HeroInstance với giá trị magnitude của input direction.
        /// 0 = Idle, 0..RunThreshold = Walk, RunThreshold..1 = Run
        /// </summary>
        public void SetMovementSpeed(float inputMagnitude)
        {
            if (!_isInitialized || animator == null) return;

            // Smooth để tránh animation pop
            _currentSpeed = Mathf.SmoothDamp(
                _currentSpeed, inputMagnitude,
                ref _speedSmoothVelocity, SpeedSmoothTime);

            animator.SetFloat(config.SpeedHash, _currentSpeed);
        }

        // ─── Combat Triggers ──────────────────────────────────────────────────

        /// <summary>Trigger animation đánh thường.</summary>
        public void PlayAttack()
        {
            if (!_isInitialized || animator == null) return;
            animator.SetTrigger(config.BasicAttackHash);
        }

        /// <summary>Trigger animation kỹ năng theo index (0 = Skill_0, 1 = Skill_1, ...).</summary>
        public void PlaySkill(int index)
        {
            if (!_isInitialized || animator == null) return;

            int hash = config.GetSkillHash(index);
            if (hash == 0)
            {
                Debug.LogWarning(
                    $"[HeroAnimatorController] {name}: Không tìm thấy SkillAnim tại index {index}. Kiểm tra HeroAnimationConfig.");
                return;
            }

            animator.SetTrigger(hash);
        }

        /// <summary>Trigger animation kỹ năng theo Animator hash (dùng khi AbilitySO tự cung cấp hash).</summary>
        public void PlaySkillByHash(int animHash)
        {
            if (!_isInitialized || animator == null || animHash == 0) return;
            animator.SetTrigger(animHash);
        }

        // ─── Movement Actions ─────────────────────────────────────────────────

        /// <summary>Trigger Jump animation.</summary>
        public void PlayJump()
        {
            if (!_isInitialized || animator == null) return;
            animator.SetTrigger(config.JumpHash);
        }

        /// <summary>Trigger Roll/Dash animation.</summary>
        public void PlayRoll()
        {
            if (!_isInitialized || animator == null) return;
            animator.SetTrigger(config.RollHash);
        }

        // ─── State ────────────────────────────────────────────────────────────

        /// <summary>Trigger Death animation và vô hiệu hóa controller.</summary>
        public void PlayDeath()
        {
            if (!_isInitialized || animator == null) return;
            animator.SetTrigger(config.DeathHash);
            this.enabled = false; // Ngừng Update sau khi chết
        }

        /// <summary>Đưa Animator về trạng thái đầu và bật lại controller sau khi actor hồi sinh.</summary>
        public void ResetAnimator()
        {
            enabled = true;
            _wasGrounded = true;
            _currentSpeed = 0f;
            _speedSmoothVelocity = 0f;
            if (animator == null) return;

            animator.Rebind();
            animator.Update(0f);
        }

        // ─── Animator Events (được gọi từ Animation Clip Events) ──────────────

        /// <summary>
        /// Gắn AnimationEvent này vào clip tại thời điểm hiệu ứng cần xảy ra
        /// (ví dụ: điểm đánh trúng của một đòn đánh thường).
        /// Sẽ forward lên StateMachine của HeroInstance nếu có.
        /// </summary>
        public void OnAnimationTransitionEvent()
        {
            // Forward lên HeroInstance để nó có thể notify StateMachine
            var heroInstance = GetComponentInParent<Game.Gameplay.Entities.HeroInstance>();
            heroInstance?.NotifyAnimationTransition();
        }

        public void OnAnimationEnterEvent()
        {
            var heroInstance = GetComponentInParent<Game.Gameplay.Entities.HeroInstance>();
            heroInstance?.NotifyAnimationEnter();
        }

        public void OnAnimationExitEvent()
        {
            var heroInstance = GetComponentInParent<Game.Gameplay.Entities.HeroInstance>();
            heroInstance?.NotifyAnimationExit();
        }
    }
}