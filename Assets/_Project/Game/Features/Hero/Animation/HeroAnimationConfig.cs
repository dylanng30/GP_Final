using System.Collections.Generic;
using UnityEngine;

namespace Game.Animation.Hero
{
    /// <summary>
    /// ScriptableObject định nghĩa tất cả animation name của một Hero cụ thể.
    /// Mỗi Hero Prefab được gán một HeroAnimationConfig riêng thông qua HeroAsset.
    /// </summary>
    [CreateAssetMenu(fileName = "NewHeroAnimConfig", menuName = "SO/Hero/Animation Config")]
    public class HeroAnimationConfig : ScriptableObject
    {
        [Header("--- Movement ---")]
        [Tooltip("Tên parameter float 'Speed' trong Animator (0=Idle, 0.5=Walk, 1=Run)")]
        public string SpeedParam      = "Speed";

        [Tooltip("Tên parameter bool 'IsGrounded' trong Animator")]
        public string IsGroundedParam = "IsGrounded";

        [Tooltip("Tên parameter bool 'IsFalling' trong Animator")]
        public string IsFallingParam  = "IsFalling";

        [Tooltip("Tên parameter float nhân tốc độ của state skill. Skill làm chậm animation qua đây, không đụng Animator.speed")]
        public string ActionSpeedParam = "ActionSpeed";

        [Header("--- Actions (Trigger names) ---")]
        public string JumpAnim        = "Jump";
        public string LandAnim        = "Land";
        public string RollAnim        = "Roll";
        public string BasicAttackAnim = "Attack";
        public string DeathAnim       = "Death";

        [Header("--- Skill Anims (index-based) ---")]
        [Tooltip("Tên Trigger animation cho từng skill (index 0 = Skill_0, 1 = Skill_1, ...)")]
        public List<string> SkillAnims = new List<string> { "Skill_0", "Skill_1" };

        [Header("--- Movement Thresholds ---")]
        [Tooltip("Giá trị Speed >= WalkThreshold thì bắt đầu Walk animation")]
        [Range(0.01f, 1f)] public float WalkThreshold = 0.1f;

        [Tooltip("Giá trị Speed >= RunThreshold thì bắt đầu Run animation")]
        [Range(0.1f, 1f)]  public float RunThreshold  = 0.6f;

        // ─── Runtime Hash Cache ─────────────────────────────────────────────────
        // Được khởi tạo một lần duy nhất khi HeroAnimatorController.Setup() được gọi.

        [HideInInspector] public int SpeedHash;
        [HideInInspector] public int IsGroundedHash;
        [HideInInspector] public int IsFallingHash;
        [HideInInspector] public int ActionSpeedHash;
        [HideInInspector] public int JumpHash;
        [HideInInspector] public int LandHash;
        [HideInInspector] public int RollHash;
        [HideInInspector] public int BasicAttackHash;
        [HideInInspector] public int DeathHash;
        [HideInInspector] public List<int> SkillHashes = new List<int>();

        // NonSerialized: Unity giữ field private qua domain reload, khiến hash của parameter mới không được tính.
        [System.NonSerialized] private bool _isInitialized;

        /// <summary>
        /// Tính Animator.StringToHash cho tất cả tên animation.
        /// Phải gọi một lần trước khi dùng bất kỳ Hash nào.
        /// </summary>
        public void InitializeHashes()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            SpeedHash        = Animator.StringToHash(SpeedParam);
            IsGroundedHash   = Animator.StringToHash(IsGroundedParam);
            IsFallingHash    = Animator.StringToHash(IsFallingParam);
            ActionSpeedHash  = Animator.StringToHash(ActionSpeedParam);
            JumpHash         = Animator.StringToHash(JumpAnim);
            LandHash         = Animator.StringToHash(LandAnim);
            RollHash         = Animator.StringToHash(RollAnim);
            BasicAttackHash  = Animator.StringToHash(BasicAttackAnim);
            DeathHash        = Animator.StringToHash(DeathAnim);

            SkillHashes.Clear();
            for (int i = 0; i < SkillAnims.Count; i++)
            {
                SkillHashes.Add(Animator.StringToHash(SkillAnims[i]));
            }
        }

        /// <summary>
        /// Reset cache để bắt buộc tính lại hash — hữu ích khi thay đổi tên animation trong Editor.
        /// </summary>
        public void ResetCache()
        {
            _isInitialized = false;
        }

        /// <summary>
        /// Trả về Trigger hash cho skill theo index. Trả về 0 nếu out of range.
        /// </summary>
        public int GetSkillHash(int index)
        {
            InitializeHashes();
            if (index >= 0 && index < SkillHashes.Count)
                return SkillHashes[index];
            return 0;
        }
    }
}
