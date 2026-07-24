using Game.Gameplay.AI.Core;

namespace Game.Gameplay.AI.GOAP
{
    public enum ActionStatus
    {
        Running,
        Success,
        Failure
    }
    
    public interface IGOAPAction
    {
        string ActionName { get; }
        float Cost { get; }
        
        AIWorldState PreconditionsMask { get; }
        AIWorldState PreconditionsValues { get; }
        
        AIWorldState EffectsMask { get; }
        AIWorldState EffectsValues { get; }
        
        bool CheckProceduralPrecondition(IAIContext context);
        float CalculateDynamicCost(IAIContext context);

        void Enter(IAIContext context);
        ActionStatus Update(IAIContext context, float deltaTime);
        void Exit(IAIContext context);
    }
}
