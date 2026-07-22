using System;
using System.Collections.Generic;
using Game.Gameplay.AI.Core;

namespace Game.Gameplay.AI.Gambit
{
    public class GambitEvaluator
    {
        private readonly List<GambitRuleSO> _sortedRules = new List<GambitRuleSO>();

        public void SetRules(IEnumerable<GambitRuleSO> rules)
        {
            _sortedRules.Clear();
            if (rules != null)
            {
                _sortedRules.AddRange(rules);
                _sortedRules.Sort((a, b) =>
                {
                    if (a == null || b == null) return 0;
                    return b.Priority.CompareTo(a.Priority);
                });
            }
        }

        public GambitRuleSO EvaluatePriorityRule(IAIContext context)
        {
            if (context == null) return null;

            int count = _sortedRules.Count;
            for (int i = 0; i < count; i++)
            {
                var rule = _sortedRules[i];
                if (rule != null && rule.Evaluate(context))
                {
                    return rule;
                }
            }

            return null;
        }
    }
}
