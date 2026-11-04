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

        public DamagePipeline CreateDamagePipeline(IRandomSource random)
        {
            return new DamagePipeline(new IDamageStep[]
            {
                new ElementRelationStep(elementChart),
                new DamageBonusStep(),
                new CriticalStep(random, defaultCritMultiplier),
                new DefenseStep(defenseConstant),
                new MinimumDamageStep(minimumDamage)
            });
        }
    }
}
