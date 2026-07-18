using UnityEngine;

namespace Dylanng.Gameplay.RPG.Abilities
{
    public interface IAbilityEffect
    {
        void Execute(GameObject caster, GameObject target, Vector3 point);
    }
}
