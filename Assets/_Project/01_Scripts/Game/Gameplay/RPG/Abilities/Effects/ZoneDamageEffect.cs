using System;
using UnityEngine;
using Dylanng.Gameplay.RPG.Abilities;
using Game.Gameplay.RPG.Abilities.Effects;
using Dylanng.Gameplay.RPG.Stats;

namespace Dylanng.Gameplay.RPG.Abilities.Effects
{
    [Serializable]
    public class ZoneDamageEffect : IAbilityEffect
    {
        public float Radius = 4f;
        public float Duration = 5f;
        public float TickInterval = 1f;
        public float DamagePerTickMultiplier = 0.5f; // e.g. 50% of ATK per tick
        public ElementType Element = ElementType.Magic;
        public ZoneDamageInstance ZoneVFXPrefab;
        public bool IsAura = false; // If true, it follows the caster. If false, it spawns at 'point'.

        public void Execute(GameObject caster, GameObject target, Vector3 point)
        {
            if (caster == null) return;

            Vector3 spawnPos = point;
            if (IsAura)
            {
                spawnPos = caster.transform.position;
            }

            // Calculate damage
            float atk = 50f;
            var stats = caster.GetComponent<StatController>();
            if (stats != null)
            {
                atk = stats.ATK;
            }
            float finalDamagePerTick = atk * DamagePerTickMultiplier;

            // Spawn the zone object
            ZoneDamageInstance zoneInstance = null;
            if (ZoneVFXPrefab != null)
            {
                var poolManager = Dylanng.Services.ServiceLocator.Get<Dylanng.Core.Pooling.PoolManager>();
                string poolKey = ZoneVFXPrefab.name;
                
                if (poolManager != null)
                {
                    poolManager.CreatePool(poolKey, ZoneVFXPrefab, 3);
                    zoneInstance = poolManager.Spawn<ZoneDamageInstance>(poolKey, spawnPos, Quaternion.identity);
                    if (zoneInstance != null)
                    {
                        zoneInstance.PoolKey = poolKey;
                    }
                }
            }

            if (zoneInstance != null)
            {
                zoneInstance.Initialize(caster, Radius, Duration, TickInterval, finalDamagePerTick, Element, IsAura);
            }
        }
    }
}
