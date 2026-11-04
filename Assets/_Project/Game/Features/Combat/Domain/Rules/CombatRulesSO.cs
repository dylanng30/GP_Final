using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat.Domain
{
    /// <summary>Luật combat dùng chung. Chỉnh giá trị tại asset, không sửa trong code.</summary>
    [CreateAssetMenu(fileName = "CombatRules", menuName = "Dylanng/RPG/Combat/Combat Rules")]
    public class CombatRulesSO : ScriptableObject
    {
        [Header("--- ELEMENTS ---")]
        [SerializeField] private ElementChartSO elementChart;

        [Header("--- DAMAGE ---")]
        [Tooltip("DEF bằng giá trị này sẽ giảm 50% sát thương")]
        [SerializeField] private float defenseConstant = 100f;

        [Tooltip("Hệ số chí mạng khi nguồn sát thương không có chỉ số CritDamage")]
        [SerializeField] private float defaultCritMultiplier = 1.5f;

        [SerializeField] private float minimumDamage = 1f;

        [Header("--- TƯƠNG KHẮC ---")]
        [SerializeField] private List<ReactionRuleSO> reactionRules = new List<ReactionRuleSO>();

        [Header("--- TƯƠNG SINH ---")]
        [SerializeField] private List<ElementSynergySO> synergies = new List<ElementSynergySO>();

        [Tooltip("Số liên kết tương sinh tối đa, 0 là không giới hạn")]
        [SerializeField] private int maxActiveLinks;

        public IElementChart ElementChart => elementChart;
        public IReadOnlyList<ReactionRuleSO> ReactionRules => reactionRules;
        public IReadOnlyList<ElementSynergySO> Synergies => synergies;
        public int MaxActiveLinks => maxActiveLinks;

        public DamagePipeline CreateDamagePipeline(IRandomSource random)
        {
            return new DamagePipeline(new IDamageStep[]
            {
                new ElementRelationStep(elementChart),
                new OutgoingModifierStep(),
                new DamageBonusStep(),
                new CriticalStep(random, defaultCritMultiplier),
                new DefenseStep(defenseConstant),
                new MinimumDamageStep(minimumDamage)
            });
        }
    }
}
