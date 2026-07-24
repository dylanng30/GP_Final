using Game.Gameplay.AI.Core;

namespace Game.Gameplay.AI.Gambit
{
    public interface IGambitAction
    {
        void Execute(IAIContext context);
    }
}
