using Dylanng.Core.Pooling;
using Dylanng.Core;
using Dylanng.Dylanng_EventBus;
using Dylanng.Events.SystemEvents;
using Dylanng.Gameplay.RPG.Abilities.Events;
using Game.Gameplay.Entities;
using UnityEngine;

namespace Game.UIs.FloatingText
{
    public class FloatingTextManager : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private PoolableObject damageTextPrefab;

        [Header("Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color critColor = Color.red;
        [SerializeField] private Color heroDamageColor = new Color(0.6f, 0f, 0.8f); // Purple

        private readonly string poolKey = "DamageText";
        private bool isPoolInitialized = false;

        private void Start()
        {
            EventBus.Subscribe<GameBootedEvent>(OnGameBooted);
            EventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<GameBootedEvent>(OnGameBooted);
            EventBus.Unsubscribe<DamageDealtEvent>(OnDamageDealt);
        }

        private void OnGameBooted(GameBootedEvent evt)
        {
            var poolManager = ServiceLocator.Get<PoolManager>();
            if (poolManager != null && damageTextPrefab != null)
            {
                poolManager.CreatePool(poolKey, damageTextPrefab, 20);
                isPoolInitialized = true;
            }
        }

        private void OnDamageDealt(DamageDealtEvent evt)
        {
            if (evt.Target == null || evt.Damage <= 0) return;

            // Randomize spawn position of damageText around hitPoint (offset upward slightly for better visibility)
            Vector3 spawnPos = evt.HitPoint + Random.insideUnitSphere * 0.5f + Vector3.up * 0.5f;

            // Determine color
            Color textColor = normalColor;

            if (evt.Target.GetComponent<HeroInstance>() != null)
            {
                // If the target being hit is a Hero
                textColor = heroDamageColor;
            }
            else
            {
                // The target is a monster
                if (evt.IsCritical)
                {
                    textColor = critColor;
                }
            }

            SpawnText(spawnPos, evt.Damage, textColor);
        }

        private void SpawnText(Vector3 position, float damage, Color color)
        {
            if (!isPoolInitialized) return;

            var poolManager = ServiceLocator.Get<PoolManager>();
            if (poolManager != null)
            {
                var damageTextObj = poolManager.Spawn<DamageText>(poolKey, position, Quaternion.identity);
                if (damageTextObj != null)
                {
                    damageTextObj.Setup(damage, color);
                }
            }
        }
    }
}
