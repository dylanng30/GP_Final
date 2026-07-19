using System.Collections.Generic;
using UnityEngine;

namespace Dylanng.Gameplay.RPG.Abilities.Projectiles
{
    public interface IAbilityProjectile
    {
        void InitializeProjectile(GameObject caster, GameObject target, Vector3 targetPoint, List<IAbilityEffect> collisionEffects = null);
    }
}
