using Dylanng;
using Game.Combat;

namespace Game.Combat.Abilities
{
    public interface IAbilityEffect
    {
        void Execute(in AbilityContext context);
    }
}
