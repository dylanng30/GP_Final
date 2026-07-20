using System;
using System.Collections;
using Dylanng.Core;
using UnityEngine;
using Dylanng.Gameplay.RPG.Stats;
using Dylanng.Core.Pooling;
using Dylanng.Services;

namespace Dylanng.Gameplay.RPG.Abilities.Effects
{
    [Serializable]
    public class ShieldEffect : IAbilityEffect
    {
        public float ShieldAmount = 100f;
        public float Duration = 3f;
        public PoolableObject ShieldVFXPrefab;
        public bool ShieldSelf = false;

        public void Execute(GameObject caster, GameObject target, Vector3 point)
        {
            if (caster == null) return;

            GameObject targetToShield = target;
            if (ShieldSelf || targetToShield == null)
            {
                targetToShield = caster;
            }

            var stats = targetToShield.GetComponent<StatController>();
            if (stats == null) return;

            var mono = targetToShield.GetComponent<MonoBehaviour>();
            if (mono != null)
            {
                mono.StartCoroutine(ShieldCoroutine(targetToShield, stats));
            }
        }

        private IEnumerator ShieldCoroutine(GameObject target, StatController stats)
        {
            var shieldMod = new StatModifier(ShieldAmount, StatModType.Flat, this);
            stats.AddModifier(StatType.Shield, shieldMod);

            Debug.Log($"[ShieldEffect] Cấp {ShieldAmount} Khiên cho {target.name} trong {Duration}s.");

            PoolableObject vfx = null;
            string vfxKey = null;
            var poolManager = ServiceLocator.Get<PoolManager>();

            if (ShieldVFXPrefab != null && poolManager != null)
            {
                vfxKey = ShieldVFXPrefab.name;
                poolManager.CreatePool(vfxKey, ShieldVFXPrefab, 3);
                vfx = poolManager.Spawn<PoolableObject>(vfxKey, target.transform.position, Quaternion.identity);
                
                if (vfx != null)
                {
                    vfx.transform.SetParent(target.transform);
                    vfx.transform.localPosition = Vector3.up * 1f;
                }
            }

            yield return new WaitForSeconds(Duration);

            if (stats != null)
            {
                stats.RemoveModifier(StatType.Shield, shieldMod);
                Debug.Log($"[ShieldEffect] Khiên của {target.name} đã hết hạn.");
            }

            if (vfx != null && poolManager != null)
            {
                vfx.transform.SetParent(null);
                poolManager.Despawn(vfxKey, vfx);
            }
        }
    }
}
