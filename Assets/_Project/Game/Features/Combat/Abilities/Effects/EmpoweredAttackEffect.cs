using Game.Data.Hero;
using System;
using UnityEngine;
using Dylanng.Gameplay.RPG.Abilities.Events;

namespace Dylanng.Gameplay.RPG.Abilities.Effects
{
    [Serializable]
    public class EmpoweredAttackEffect : IAbilityEffect
    {
        public float BonusDamage = 100f;
        public GameElement Element = GameElement.Physical;
        public float Duration = 5f; // Buff expires if not used in 5s
        public PoolableObject BuffVFXPrefab;
        public PoolableObject HitVFXPrefab;

        public void Execute(GameObject caster, GameObject target, Vector3 point)
        {
            if (caster == null) return;

            // Remove existing handler if any
            var existing = caster.GetComponent<EmpoweredAttackHandler>();
            if (existing != null)
            {
                UnityEngine.Object.Destroy(existing);
            }

            var handler = caster.AddComponent<EmpoweredAttackHandler>();
            handler.Initialize(BonusDamage, Element, Duration, BuffVFXPrefab, HitVFXPrefab);
        }
    }

    public class EmpoweredAttackHandler : MonoBehaviour
    {
        private float _bonusDamage;
        private GameElement _element;
        private PoolableObject _hitVfxPrefab;
        private PoolableObject _buffVfxInstance;
        private string _buffVfxKey;
        private bool _isUsed = false;

        public void Initialize(float bonusDamage, GameElement element, float duration, PoolableObject buffVfxPrefab, PoolableObject hitVfxPrefab)
        {
            _bonusDamage = bonusDamage;
            _element = element;
            _hitVfxPrefab = hitVfxPrefab;

            var poolManager = ServiceLocator.Get<PoolManager>();
            if (buffVfxPrefab != null && poolManager != null)
            {
                _buffVfxKey = buffVfxPrefab.name;
                poolManager.CreatePool(_buffVfxKey, buffVfxPrefab, 2);
                _buffVfxInstance = poolManager.Spawn<PoolableObject>(_buffVfxKey, transform.position, Quaternion.identity);
                if (_buffVfxInstance != null)
                {
                    _buffVfxInstance.transform.SetParent(transform);
                    _buffVfxInstance.transform.localPosition = Vector3.up; // Offset a bit
                }
            }

            EventBus.Subscribe<DamageDealtEvent>(OnDamageDealt);
            Destroy(this, duration); // Auto expire after duration
        }

        private void OnDamageDealt(DamageDealtEvent evt)
        {
            if (_isUsed) return;
            
            // Only trigger if this object is the caster
            if (evt.Caster == gameObject && evt.Target != null)
            {
                _isUsed = true;
                Debug.Log($"[EmpoweredAttack] Đòn đánh được cường hóa! Gây thêm {_bonusDamage} sát thương.");

                // Deal bonus damage
                EventBus.Publish(new DamageDealtEvent(gameObject, evt.Target, _bonusDamage, _element, evt.HitPoint, false));

                // Spawn Hit VFX
                var poolManager = ServiceLocator.Get<PoolManager>();
                if (_hitVfxPrefab != null && poolManager != null)
                {
                    string hitKey = _hitVfxPrefab.name;
                    poolManager.CreatePool(hitKey, _hitVfxPrefab, 3);
                    poolManager.Spawn<PoolableObject>(hitKey, evt.Target.transform.position + Vector3.up, Quaternion.identity);
                }

                // Cleanup
                Destroy(this);
            }
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<DamageDealtEvent>(OnDamageDealt);

            if (_buffVfxInstance != null)
            {
                var poolManager = ServiceLocator.Get<PoolManager>();
                if (poolManager != null)
                {
                    _buffVfxInstance.transform.SetParent(null);
                    poolManager.Despawn(_buffVfxKey, _buffVfxInstance);
                }
            }
        }
    }
}
