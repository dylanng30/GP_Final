using System.Collections.Generic;
using Game.Gameplay.AI.Core;
using UnityEngine;

namespace Game.Gameplay.AI.Gambit
{
    [CreateAssetMenu(fileName = "NewGambitRule", menuName = "Dylanng/AI/Gambit/Gambit Rule")]
    public class GambitRuleSO : ScriptableObject
    {
        [Header("--- RULE METADATA ---")]
        [SerializeField] private string ruleName = "Priority Override Rule";
        [SerializeField] private int priority = 100;

        [Header("--- STRATEGY CONDITIONS ---")]
        [SerializeReference] private List<IGambitCondition> conditions = new List<IGambitCondition>();

        [Header("--- COMMAND ACTION ---")]
        [SerializeReference] private IGambitAction action;

        public string RuleName => ruleName;
        public int Priority => priority;
        public List<IGambitCondition> Conditions
        {
            get => conditions;
            set => conditions = value;
        }
        public IGambitAction Action
        {
            get => action;
            set => action = value;
        }

        public bool Evaluate(IAIContext context)
        {
            if (action == null) return false;
            
            int count = conditions.Count;
            for (int i = 0; i < count; i++)
            {
                if (conditions[i] == null || !conditions[i].Evaluate(context))
                {
                    return false;
                }
            }
            
            return true;
        }
    }
}
