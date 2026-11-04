using System;
using Game.Combat;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Dylanng.Gameplay.RPG.Abilities.Effects
{
    /// <summary>Dựng một vật thể chặn đường và chặn đạn phía trước người thi triển.</summary>
    [Serializable]
    public class SpawnStructureEffect : IAbilityEffect
    {
        public StoneWallStructure Prefab;
        public float ForwardDistance = 3f;
        public float Lifetime = 5f;

        public void Execute(in AbilityContext context)
        {
            if (Prefab == null || context.Caster == null) return;

            Transform caster = context.Caster.transform;
            Vector3 direction = context.Direction;
            Vector3 position = caster.position + direction * ForwardDistance;

            StoneWallStructure structure = Object.Instantiate(Prefab, position, Quaternion.LookRotation(direction));
            structure.Initialize(context.Caster, Lifetime);
        }
    }
}
