using Dylanng;
using Game.Combat;
using System;
using System.Collections.Generic;
using Game.Combat.Abilities.Projectiles;
using UnityEngine;

namespace Game.Combat.Abilities.Effects
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

        public void Execute(in AbilityContext context)
        {
            var (caster, target, point) = context;
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
                Transform spawnPoint = caster.TryGetComponent<Game.Gameplay.Entities.HeroInstance>(out var heroInstance)
                    ? heroInstance.GetBone(SpawnPointName)
                    : AttachPoints.FindDeep(caster.transform, SpawnPointName);

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
                abilityProjectile.InitializeProjectile(context, CollisionEffects);
            }
        }

    }
}
