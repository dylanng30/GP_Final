using System;
using Dylanng.Core;
using Dylanng.Core.Pooling;
using Dylanng.Gameplay.RPG.Abilities.Projectiles;
using UnityEngine;

namespace Dylanng.Gameplay.RPG.Abilities.Effects
{
    [Serializable]
    public class ProjectileEffect : IAbilityEffect
    {
        public string ProjectilePoolKey = "Fireball_Projectile";
        public Vector3 SpawnOffset = new Vector3(0, 1.5f, 1f);

        public void Execute(GameObject caster, GameObject target, Vector3 point)
        {
            if (caster == null) return;

            var poolManager = ServiceLocator.Get<PoolManager>();
            if (poolManager == null)
            {
                Debug.LogError("[ProjectileEffect] Không tìm thấy PoolManager thông qua ServiceLocator!");
                return;
            }

            Vector3 spawnPos = caster.transform.TransformPoint(SpawnOffset);
            Quaternion spawnRot = caster.transform.rotation;

            if (target != null)
            {
                Vector3 dir = (target.transform.position - spawnPos).normalized;
                if (dir != Vector3.zero)
                {
                    spawnRot = Quaternion.LookRotation(dir);
                }
            }
            else if (point != Vector3.zero && point != spawnPos)
            {
                Vector3 dir = (point - spawnPos).normalized;
                if (dir != Vector3.zero)
                {
                    spawnRot = Quaternion.LookRotation(dir);
                }
            }

            var projectileObj = poolManager.Spawn<PoolableObject>(ProjectilePoolKey, spawnPos, spawnRot);
            if (projectileObj != null && projectileObj is IAbilityProjectile abilityProjectile)
            {
                abilityProjectile.InitializeProjectile(caster);
            }
        }
    }
}
