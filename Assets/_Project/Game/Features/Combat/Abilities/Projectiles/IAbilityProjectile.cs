using Dylanng;
using System.Collections.Generic;
using Game.Combat;

namespace Game.Combat.Abilities.Projectiles
{
    public interface IAbilityProjectile
    {
        void InitializeProjectile(in AbilityContext context, List<IAbilityEffect> collisionEffects = null);
    }
}
