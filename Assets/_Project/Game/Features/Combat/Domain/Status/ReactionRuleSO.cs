using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat.Domain
{
    /// <summary>
    /// Luật tương khắc: khi Incoming được áp lên mục tiêu đang có RequiredOnTarget thì rule kích hoạt.
    /// </summary>
    [CreateAssetMenu(fileName = "NewReaction", menuName = "Dylanng/RPG/Combat/Reaction Rule")]
    public class ReactionRuleSO : ScriptableObject
    {
        public string DisplayName;
        public StatusDefinitionSO Incoming;
        public StatusDefinitionSO RequiredOnTarget;
        public bool RemovesRequiredStatus = true;

        [Tooltip("Sát thương thưởng, tính theo tỉ lệ ATK của người kích hoạt")]
        public float BonusAttackRatio = 0.15f;
    }

    public static class ReactionTable
    {
        /// <summary>Lấy các rule sẽ kích hoạt khi áp incoming lên actor có targetStatuses.</summary>
        public static void FindMatches(
            IReadOnlyList<ReactionRuleSO> rules,
            StatusDefinitionSO incoming,
            StatusSet targetStatuses,
            List<ReactionRuleSO> results)
        {
            results.Clear();
            for (int i = 0; i < rules.Count; i++)
            {
                ReactionRuleSO rule = rules[i];
                if (rule.Incoming == incoming && targetStatuses.Has(rule.RequiredOnTarget))
                {
                    results.Add(rule);
                }
            }
        }
    }
}
