using Game.Gameplay.AI.Core;

namespace Game.Gameplay.AI.Gambit
{
    public interface IGambitCondition
    {
        bool Evaluate(IAIContext context);
    }
}
