using Game.Combat;
using System;
using System.Collections;
using UnityEngine;
using Game.Gameplay.Controllers;
using Game.VFXs;

namespace Dylanng.Gameplay.RPG.Abilities.Effects
{
    [Serializable]
    public class StunEffect : IAbilityEffect
    {
        public float Duration = 2.0f;
        public BaseVFXView StunVFXPrefab;
        [Range(0f, 1f)] public float Chance = 1.0f;

        public void Execute(in AbilityContext context)
        {
            var (caster, target, point) = context;
            if (target == null || UnityEngine.Random.value > Chance) return;
            
            // Use the target's MonoBehaviour to run the coroutine
            var mono = target.GetComponent<MonoBehaviour>();
            if (mono != null)
            {
                mono.StartCoroutine(StunCoroutine(target, Duration));
            }
        }

        private IEnumerator StunCoroutine(GameObject target, float time)
        {
            var aiController = target.GetComponent<HeroAIController>();
            var inputController = target.GetComponent<PlayerInputController>();
            var entityController = target.GetComponent<IEntityController>();
            var targetingSystem = target.GetComponent<PlayerTargetingSystem>();
            
            // Disable AI and Input to "stun" the entity
            if (aiController != null) aiController.enabled = false;
            if (inputController != null) inputController.enabled = false;
            if (targetingSystem != null) targetingSystem.enabled = false;
            
            entityController?.StopMove();

            var poolManager = ServiceLocator.Get<PoolManager>();
            string vfxPoolKey = StunVFXPrefab != null ? StunVFXPrefab.name : null;
            PoolableObject vfxPoolable = null;

            if (StunVFXPrefab != null && poolManager != null)
            {
                poolManager.CreatePool(vfxPoolKey, StunVFXPrefab, 3);
                vfxPoolable = poolManager.Spawn<PoolableObject>(vfxPoolKey, target.transform.position + Vector3.up * 2f, Quaternion.identity);
                if (vfxPoolable != null)
                {
                    vfxPoolable.SetParent(target.transform);
                    vfxPoolable.transform.localEulerAngles = new Vector3(-90, 0, 0);
                }
            }

            Debug.Log($"[StunEffect] {target.name} bị choáng trong {time}s.");

            yield return new WaitForSeconds(time);

            if (target != null)
            {
                // Re-enable everything
                if (aiController != null) aiController.enabled = true;
                if (inputController != null) inputController.enabled = true;
                if (targetingSystem != null) targetingSystem.enabled = true;
                
                if (vfxPoolable != null)
                {
                    vfxPoolable.transform.SetParent(null); // Unparent before despawning
                    if (poolManager != null)
                    {
                        poolManager.Despawn(vfxPoolKey, vfxPoolable);
                    }
                    else
                    {
                        UnityEngine.Object.Destroy(vfxPoolable.gameObject);
                    }
                }
                
                Debug.Log($"[StunEffect] {target.name} đã hết choáng.");
            }
        }
    }
}
