using System.Collections.Generic;
using Game.Combat;

namespace Dylanng.Gameplay.RPG.Abilities.Projectiles
{
    public interface IAbilityProjectile
    {
        void InitializeProjectile(in AbilityContext context, List<IAbilityEffect> collisionEffects = null);
    }
}
