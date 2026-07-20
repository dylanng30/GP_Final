using System;
using UnityEngine;
using Dylanng.Core.Pooling;
using Dylanng.Services;
using Dylanng.Core;

namespace Dylanng.Gameplay.RPG.Abilities.Effects
{
    [Serializable]
    public class BlinkEffect : IAbilityEffect
    {
        public float MaxDistance = 10f;
        public PoolableObject BlinkVFXPrefab;
        public bool BlinkToTarget = false;

        public void Execute(GameObject caster, GameObject target, Vector3 point)
        {
            if (caster == null) return;

            Vector3 destination = point;
            if (BlinkToTarget && target != null)
            {
                destination = target.transform.position;
            }

            // Validate max distance
            Vector3 direction = (destination - caster.transform.position);
            if (direction.magnitude > MaxDistance)
            {
                destination = caster.transform.position + direction.normalized * MaxDistance;
            }

            // Spawn VFX at original position
            SpawnVFX(caster.transform.position);

            // Teleport
            var cc = caster.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            
            caster.transform.position = destination;
            
            if (cc != null) cc.enabled = true;

            // Spawn VFX at new position
            SpawnVFX(destination);
        }

        private void SpawnVFX(Vector3 pos)
        {
            if (BlinkVFXPrefab == null) return;

            var poolManager = ServiceLocator.Get<PoolManager>();
            string vfxPoolKey = BlinkVFXPrefab.name;
            if (poolManager != null)
            {
                poolManager.CreatePool(vfxPoolKey, BlinkVFXPrefab, 5);
                var vfx = poolManager.Spawn<PoolableObject>(vfxPoolKey, pos, Quaternion.identity);
                // Assume VFX handles its own despawning or is brief
            }
            else
            {
                UnityEngine.Object.Instantiate(BlinkVFXPrefab, pos, Quaternion.identity);
            }
        }
    }
}
