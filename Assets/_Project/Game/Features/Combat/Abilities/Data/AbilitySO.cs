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

        [Tooltip("Khóa di chuyển trong lúc cast.")]
        public bool LocksMovement = true;

        [Header("--- Aiming Config ---")]
        public float Range = 8.0f;
        public float IndicatorRadius = 2.0f;
        public SkillIndicatorType IndicatorType = SkillIndicatorType.AreaTarget;
        public bool RequiresTarget = false;

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
