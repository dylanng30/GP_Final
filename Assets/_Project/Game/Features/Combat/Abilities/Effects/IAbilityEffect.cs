using Game.Combat;

namespace Dylanng.Gameplay.RPG.Abilities
{
    public interface IAbilityEffect
    {
        void Execute(in AbilityContext context);
    }
}
