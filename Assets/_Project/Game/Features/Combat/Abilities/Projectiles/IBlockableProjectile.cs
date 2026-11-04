using Dylanng;
using UnityEngine;

namespace Game.Combat.Abilities.Projectiles
{
    public interface IBlockableProjectile
    {
        /// <summary>
        /// The caster (owner) of the projectile. Used to determine if the shield should block it.
        /// </summary>
        GameObject Caster { get; }

        /// <summary>
        /// Triggers when the projectile is blocked by a shield.
        /// Should handle its own destruction, pooling, or VFX.
        /// </summary>
        void Block();
    }
}
