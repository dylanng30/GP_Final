using Dylanng;
using Game.Combat;
using Game.Data.Hero;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat.Abilities
{
    [CreateAssetMenu(fileName = "NewAbility", menuName = "Dylanng/RPG/Abilities/Ability Data")]
    public class AbilitySO : ScriptableData
    {
        [Header("--- Basic Info ---")]
        public string AbilityID;
        public string AbilityName;
        [TextArea(3, 5)] public string Description;
        public Sprite Icon;
        public AbilityType Type;
        public GameElement Element;

        [Header("--- Costs & Cooldown ---")]
        public float Cooldown = 4.0f;
        public float EnergyCost = 20.0f;
        public float StaminaCost = 0.0f;

        [Tooltip("Cấp tối thiểu của hero để mở khóa skill này.")]
        public int UnlockLevel = 1;

        [Header("--- Animation & Timings ---")]
        [Tooltip("Tên Trigger trong Animator (ví dụ: Skill_0). Tự động tính hash khi runtime.")]
        public string AnimationTriggerName = "";

        [Tooltip("Hash được tính tự động từ AnimationTriggerName. Không cần điền tay.")]
        [HideInInspector] public int AnimationTriggerHash;

        public float TotalCastDuration = 1.2f;

        [Tooltip("Tốc độ animation cho tới lúc skill kích hoạt (< 1 kéo dài pha tụ lực). Sau khi kích hoạt trở về 1.")]
        public float AnimationSpeed = 1f;

        [Tooltip("Khóa di chuyển trong lúc cast.")]
        public bool LocksMovement = true;

        [Header("--- Aiming Config ---")]
        public float Range = 8.0f;
        public float IndicatorRadius = 2.0f;
        public SkillIndicatorType IndicatorType = SkillIndicatorType.AreaTarget;
        public bool RequiresTarget = false;

        [Header("--- Presentation ---")]
        [Tooltip("VFX tụ lực trong lúc cast, gắn vào điểm CastVfxAttachPoint (tên xương) nếu có")]
        public GameObject CastVfxPrefab;
        public string CastVfxAttachPoint = "";
        public float CastVfxScale = 1f;

        [Tooltip("Thời gian giữ VFX tụ lực. 0 nghĩa là tới hết TotalCastDuration")]
        public float CastVfxLifetime = 0f;
        [Tooltip("Key trong CombatSoundBank, phát khi bắt đầu cast")]
        public string CastSoundKey = "";
        [Tooltip("Key trong CombatSoundBank, phát khi skill kích hoạt")]
        public string ExecuteSoundKey = "";
        [Tooltip("Để lại bóng mờ của người thi triển lúc bắt đầu cast")]
        public bool CastAfterimage = false;

        [Header("--- Strategy Effects (Polymorphic) ---")]
        [SerializeReference]
        public List<IAbilityEffect> Effects = new List<IAbilityEffect>();

        /// <summary>
        /// Lấy Animator hash cho animation này.
        /// Ưu tiên AnimationTriggerName (string), fallback sang AnimationTriggerHash (int) nếu Name rỗng.
        /// </summary>
        public int GetAnimationHash()
        {
            if (!string.IsNullOrEmpty(AnimationTriggerName))
            {
                return Animator.StringToHash(AnimationTriggerName);
            }
            return AnimationTriggerHash;
        }

        public void ExecuteEffects(in AbilityContext context)
        {
            if (Effects == null) return;
            for (int i = 0; i < Effects.Count; i++)
            {
                Effects[i]?.Execute(context);
            }
        }
    }
}
