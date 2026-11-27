using System.Collections.Generic;
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
        // Smoothed speed value để blend tree không bị jittery
        private const float SpeedSmoothTime = 0.1f;

        [SerializeField] private HeroAnimationConfig config;
        [SerializeField] private Animator animator;
        [SerializeField] private CharacterController characterController;

        [Header("--- Grounding ---")]
        [Tooltip("Rời đất lâu hơn chừng này mới coi là đang rơi. CharacterController.isGrounded nhấp nháy trên dốc và bậc thấp")]
        [SerializeField] private float airborneDelay = 0.15f;
        [Tooltip("Còn mặt đất trong khoảng này dưới chân thì vẫn coi là đứng trên đất")]
        [SerializeField] private float groundProbeDistance = 0.4f;
        [Tooltip("Chỉ phát Land (tiếp đất nặng) khi rơi từ đỉnh xuống sâu hơn chừng này (m). Nhảy thường rơi khoảng 2 m")]
        [SerializeField] private float hardLandingDropHeight = 4f;
        [Tooltip("Sau khi bấm nhảy, bỏ qua phép dò mặt đất trong chừng này giây")]
        [SerializeField] private float jumpGroundGrace = 0.25f;

        [Header("--- Triggers ---")]
        [Tooltip("Trigger hành động chưa được Animator dùng sau chừng này giây thì bị hủy, tránh animation phát trễ")]
        [SerializeField] private float actionTriggerLifetime = 0.2f;
        [Tooltip("Đánh thường không cắt ngang clip skill trước mốc này (normalized time)")]
        [SerializeField, Range(0f, 1f)] private float skillProtectedUntil = 0.85f;
        [Tooltip("Đánh thường không cắt ngang clip lăn/dash trước mốc này (normalized time)")]
        [SerializeField, Range(0f, 1f)] private float rollProtectedUntil = 0.6f;

        private static readonly string[] ProbeIgnoredLayers = { "Hero", "Monster" };

        private bool _isInitialized;
        private bool _isDead;
        private bool _isAirborne;
        private float _ungroundedTime;
        private float _airPeakY;
        private float _jumpGraceEndTime;

        private float _currentSpeed;
        private float _speedSmoothVelocity;

        private readonly HashSet<int> _parameters = new HashSet<int>();
        private readonly List<int> _actionTriggers = new List<int>();
        private int _pendingTrigger;
        private float _pendingTriggerTime;

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
            CacheParameters();

            _isInitialized = true;
        }

        private void CacheParameters()
        {
            _parameters.Clear();
            _actionTriggers.Clear();
            if (animator == null) return;

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                _parameters.Add(parameter.nameHash);
            }

            AddActionTrigger(config.BasicAttackHash);
            AddActionTrigger(config.RollHash);
            AddActionTrigger(config.JumpHash);
            foreach (int skillHash in config.SkillHashes) AddActionTrigger(skillHash);
        }

        private void AddActionTrigger(int hash)
        {
            if (HasParameter(hash) && !_actionTriggers.Contains(hash)) _actionTriggers.Add(hash);
        }

        private bool HasParameter(int hash) => hash != 0 && _parameters.Contains(hash);

        // ─── Update loop ──────────────────────────────────────────────────────

        private void Update()
        {
            if (!_isInitialized || animator == null || config == null) return;

            UpdateGroundedState();
            ExpirePendingTrigger();
        }

        private void UpdateGroundedState()
        {
            if (characterController == null) return;

            // Vừa nhảy thì chân còn sát đất, bỏ qua phép dò để Jump không bị coi là đã tiếp đất.
            bool justJumped = Time.time < _jumpGraceEndTime;
            bool touchingGround = !justJumped && (characterController.isGrounded || IsGroundBelow());
            _ungroundedTime = touchingGround ? 0f : _ungroundedTime + Time.deltaTime;

            bool wasAirborne = _isAirborne;
            _isAirborne = _ungroundedTime > airborneDelay;

            float y = characterController.transform.position.y;
            if (_isAirborne) _airPeakY = wasAirborne ? Mathf.Max(_airPeakY, y) : y;

            if (wasAirborne && !_isAirborne)
            {
                // Chỉ cú rơi sâu mới tiếp đất nặng, nhảy thường về thẳng Locomotion.
                if (_airPeakY - y >= hardLandingDropHeight) SetTriggerSafe(config.LandHash);
            }

            SetBoolSafe(config.IsGroundedHash, !_isAirborne);
            SetBoolSafe(config.IsFallingHash, _isAirborne);
        }

        private bool IsGroundBelow()
        {
            // Animator nằm trên model con, chân thật là root mang CharacterController.
            Vector3 feet = characterController.transform.position;
            Vector3 origin = feet + Vector3.up * 0.1f;
            int mask = Physics.DefaultRaycastLayers & ~LayerMask.GetMask(ProbeIgnoredLayers);
            return Physics.Raycast(origin, Vector3.down, 0.1f + groundProbeDistance, mask, QueryTriggerInteraction.Ignore);
        }

        private void ExpirePendingTrigger()
        {
            if (_pendingTrigger == 0) return;

            if (!animator.GetBool(_pendingTrigger))
            {
                _pendingTrigger = 0;
                return;
            }

            if (Time.time - _pendingTriggerTime < actionTriggerLifetime) return;

            animator.ResetTrigger(_pendingTrigger);
            _pendingTrigger = 0;
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

        /// <summary>
        /// Nhân tốc độ của state skill. Chỉ state skill dùng parameter này nên Locomotion, đánh thường, dash
        /// vẫn chạy tốc độ thường khi skill tụ lực.
        /// </summary>
        public void SetActionSpeed(float speed)
        {
            if (!_isInitialized || animator == null) return;
            SetFloatSafe(config.ActionSpeedHash, speed);
        }

        // ─── Combat Triggers ──────────────────────────────────────────────────

        /// <summary>Trigger animation đánh thường.</summary>
        public void PlayAttack() => PlayAction(config != null ? config.BasicAttackHash : 0);

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

            PlayAction(hash);
        }

        /// <summary>Trigger animation kỹ năng theo Animator hash (dùng khi AbilitySO tự cung cấp hash).</summary>
        public void PlaySkillByHash(int animHash) => PlayAction(animHash);

        // ─── Movement Actions ─────────────────────────────────────────────────

        /// <summary>Trigger Jump animation.</summary>
        public void PlayJump()
        {
            if (!_isInitialized || animator == null || _isDead) return;

            PlayAction(config.JumpHash);
            _jumpGraceEndTime = Time.time + jumpGroundGrace;
            _ungroundedTime = airborneDelay;
        }

        /// <summary>Trigger Roll/Dash animation.</summary>
        public void PlayRoll() => PlayAction(config != null ? config.RollHash : 0);

        /// <summary>
        /// Mỗi lúc chỉ giữ một trigger hành động: hành động mới hủy hành động đang chờ,
        /// và trigger không được dùng trong <see cref="actionTriggerLifetime"/> giây thì bị hủy.
        /// </summary>
        private void PlayAction(int hash)
        {
            if (!_isInitialized || animator == null || _isDead || !HasParameter(hash)) return;
            if (hash == config.BasicAttackHash && IsProtectedFromAttack()) return;

            ResetActionTriggers();
            animator.SetTrigger(hash);
            _pendingTrigger = hash;
            _pendingTriggerTime = Time.time;
        }

        /// <summary>
        /// Skill hoặc lăn đang chờ vào state, hoặc đang chạy mà chưa tới mốc bảo vệ: đánh thường không được cắt ngang.
        /// State trùng tên trigger nên so bằng hash.
        /// </summary>
        private bool IsProtectedFromAttack()
        {
            if (_pendingTrigger != 0 && (_pendingTrigger == config.RollHash || config.SkillHashes.Contains(_pendingTrigger))) return true;

            AnimatorStateInfo state = animator.IsInTransition(0)
                ? animator.GetNextAnimatorStateInfo(0)
                : animator.GetCurrentAnimatorStateInfo(0);
            if (config.SkillHashes.Contains(state.shortNameHash)) return state.normalizedTime < skillProtectedUntil;
            if (state.shortNameHash == config.RollHash) return state.normalizedTime < rollProtectedUntil;
            return false;
        }

        private void ResetActionTriggers()
        {
            foreach (int trigger in _actionTriggers) animator.ResetTrigger(trigger);
            _pendingTrigger = 0;
        }

        // ─── State ────────────────────────────────────────────────────────────

        /// <summary>Trigger Death animation và vô hiệu hóa controller.</summary>
        public void PlayDeath()
        {
            if (!_isInitialized || animator == null) return;

            ResetActionTriggers();
            SetFloatSafe(config.ActionSpeedHash, 1f);
            SetTriggerSafe(config.DeathHash);
            _isDead = true;
            this.enabled = false; // Ngừng Update sau khi chết
        }

        /// <summary>Đưa Animator về trạng thái đầu và bật lại controller sau khi actor hồi sinh.</summary>
        public void ResetAnimator()
        {
            enabled = true;
            _isDead = false;
            _isAirborne = false;
            _ungroundedTime = 0f;
            _currentSpeed = 0f;
            _speedSmoothVelocity = 0f;
            _pendingTrigger = 0;
            if (animator == null) return;

            animator.Rebind();
            animator.Update(0f);
        }

        private void SetTriggerSafe(int hash)
        {
            if (HasParameter(hash)) animator.SetTrigger(hash);
        }

        private void SetBoolSafe(int hash, bool value)
        {
            if (HasParameter(hash)) animator.SetBool(hash, value);
        }

        private void SetFloatSafe(int hash, float value)
        {
            if (HasParameter(hash)) animator.SetFloat(hash, value);
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
