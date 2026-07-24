using System;
using Game.Gameplay.AI.Core;

namespace Game.Gameplay.AI.Gambit.Conditions
{
    [Serializable]
    public class TargetCastingSpellCondition : IGambitCondition
    {
        public bool Evaluate(IAIContext context)
        {
            if (context == null) return false;
            return context.WorldState.HasState(AIWorldStateKey.TargetCastingSpell);
        }
    }
}
