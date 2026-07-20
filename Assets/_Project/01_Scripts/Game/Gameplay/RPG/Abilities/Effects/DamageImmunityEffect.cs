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
    public class DamageImmunityEffect : IAbilityEffect
    {
        [Header("--- WORLD SET UP ---")]
        public float Height = 1f;
        public float ScaleSize = 1f;
        
        [Header("--- SETUP ---")]
        public float Duration = 3f;
        public ScienceFictionShield ShieldVFXPrefab;
        public bool ApplyToSelf = true;

        public void Execute(GameObject caster, GameObject target, Vector3 point)
        {
            if (caster == null) return;

            GameObject targetToBuff = target;
            if (ApplyToSelf || targetToBuff == null)
            {
                targetToBuff = caster;
            }

            var stats = targetToBuff.GetComponent<StatController>();
            if (stats == null) return;

            var mono = targetToBuff.GetComponent<MonoBehaviour>();
            if (mono != null)
            {
                mono.StartCoroutine(ImmunityCoroutine(targetToBuff, stats));
            }
        }

        private IEnumerator ImmunityCoroutine(GameObject target, StatController stats)
        {
            // Apply Damage Immunity Mod (Value > 0 means immune)
            var immunityMod = new StatModifier(100f, StatModType.Flat, this);
            stats.AddModifier(StatType.DamageImmunity, immunityMod);

            Debug.Log($"[DamageImmunityEffect] Cấp Miễn nhiễm sát thương cho {target.name} trong {Duration}s.");

            ScienceFictionShield shieldInstance = null;
            string shieldKey = null;
            var poolManager = ServiceLocator.Get<PoolManager>();

            if (ShieldVFXPrefab != null && poolManager != null)
            {
                shieldKey = ShieldVFXPrefab.name;
                poolManager.CreatePool(shieldKey, ShieldVFXPrefab, 1);
                shieldInstance = poolManager.Spawn<ScienceFictionShield>(shieldKey, target.transform.position, Quaternion.identity);
                
                if (shieldInstance != null)
                {
                    shieldInstance.Initialize(target);
                    shieldInstance.SetParent(target.transform);
                    shieldInstance.SetLocalPosition(new Vector3(0, Height, 0));
                    shieldInstance.SetLocalScale(Vector3.one * ScaleSize);
                }
            }

            yield return new WaitForSeconds(Duration);

            if (stats != null)
            {
                stats.RemoveModifier(StatType.DamageImmunity, immunityMod);
                Debug.Log($"[DamageImmunityEffect] Hết hạn Miễn nhiễm sát thương của {target.name}.");
            }

            if (shieldInstance != null && poolManager != null)
            {
                shieldInstance.SetParent(null);
                poolManager.Despawn(shieldKey, shieldInstance);
            }
        }
    }
}
