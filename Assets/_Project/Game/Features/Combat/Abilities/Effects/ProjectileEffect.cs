using System;
using System.Collections.Generic;
using Dylanng.Gameplay.RPG.Abilities.Projectiles;
using UnityEngine;

namespace Dylanng.Gameplay.RPG.Abilities.Effects
{
    [Serializable]
    public class ProjectileEffect : IAbilityEffect
    {
        [Header("--- Projectile Settings ---")]
        public string ProjectilePoolKey = "Fireball_Projectile";
        public PoolableObject ProjectilePrefab;
        public Vector3 SpawnOffset = new Vector3(0, 1.5f, 1f);

        [Tooltip("Tên của Transform con (như FirePoint) để spawn đạn. Nếu trống, sẽ dùng SpawnOffset.")]
        public string SpawnPointName = "";

        [Header("--- Collision Effects (Polymorphic) ---")]
        [SerializeReference]
        public List<IAbilityEffect> CollisionEffects = new List<IAbilityEffect>();

        public void Execute(GameObject caster, GameObject target, Vector3 point)
        {
            if (caster == null) return;

            var poolManager = ServiceLocator.Get<PoolManager>();
            if (poolManager == null)
            {
                Debug.LogError("[ProjectileEffect] Không tìm thấy PoolManager thông qua ServiceLocator!");
                return;
            }

            // Tự động tạo Pool cho viên đạn nếu chưa tồn tại
            if (ProjectilePrefab != null)
            {
                poolManager.CreatePool(ProjectilePoolKey, ProjectilePrefab, 5);
            }

            Vector3 spawnPos = caster.transform.TransformPoint(SpawnOffset);
            Quaternion spawnRot = caster.transform.rotation;

            if (!string.IsNullOrEmpty(SpawnPointName))
            {
                Transform spawnPoint = null;
                if (caster.TryGetComponent<Game.Gameplay.Entities.HeroInstance>(out var heroInstance))
                {
                    spawnPoint = heroInstance.GetBone(SpawnPointName);
                }
                else
                {
                    spawnPoint = FindChildRecursive(caster.transform, SpawnPointName);
                }
                
                if (spawnPoint != null)
                {
                    spawnPos = spawnPoint.position;
                    spawnRot = spawnPoint.rotation;
                }
            }

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
                abilityProjectile.InitializeProjectile(caster, target, point, CollisionEffects);
            }
        }

        private Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            foreach (Transform child in parent)
            {
                Transform result = FindChildRecursive(child, name);
                if (result != null) return result;
            }
            return null;
        }
    }
}
