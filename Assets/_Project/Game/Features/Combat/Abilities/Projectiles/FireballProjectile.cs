using Dylanng;
using Game.Combat;
using System.Collections.Generic;
using Game.Combat.Abilities.Events;
using Game.Gameplay.Entities;
using UnityEngine;

namespace Game.Combat.Abilities.Projectiles
{
    public class FireballProjectile : PoolableObject, IAbilityProjectile, IUpdatable, IBlockableProjectile
    {
        [SerializeField] private float speed = 15f;
        [SerializeField] private float explosionRadius = 3f;
        [SerializeField] private float maxLifetime = 5f;
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private float arcHeight = 3f;

        [Header("--- Explosion VFX ---")]
        [SerializeField] private PoolableObject explosionVFXPrefab;
        [SerializeField] private string explosionVFXPoolKey = "VFX_Fireball_Explosion";

        private AbilityContext _context;
        private GameObject _caster;
        private List<IAbilityEffect> _collisionEffects;

        private Vector3 _startPosition;
        private GameObject _target;
        private Vector3 _targetPoint;
        private float _travelDuration;
        private float _timeElapsed;
        private bool _isExploded;

        public GameObject Caster => _caster;

        public void InitializeProjectile(in AbilityContext context, List<IAbilityEffect> collisionEffects = null)
        {
            _context = context;
            _caster = context.Caster;
            _target = context.Target;
            _targetPoint = context.Point;
            _collisionEffects = collisionEffects;
            _startPosition = transform.position;
            _timeElapsed = 0f;
            _isExploded = false;

            if (enemyLayer.value == 0 && _caster != null && _caster.TryGetComponent<HeroInstance>(out var heroInst))
            {
                LayerMask casterMask = heroInst.GetTargetLayerMask();
                if (casterMask.value != 0)
                {
                    enemyLayer = casterMask;
                }
            }

            Vector3 endPos = GetTargetPosition();
            float distance = Vector3.Distance(_startPosition, endPos);
            _travelDuration = distance / speed;
            if (_travelDuration <= 0f) _travelDuration = 0.1f;

        }

        private Vector3 GetTargetPosition()
        {
            return _target != null ? _target.transform.position : _targetPoint;
        }

        public override void OnSpawn()
        {
            base.OnSpawn();
            _timeElapsed = 0f;
            _isExploded = false;

            // Register pool for explosion VFX if prefab is assigned
            if (explosionVFXPrefab != null && !string.IsNullOrEmpty(explosionVFXPoolKey))
            {
                var poolManager = ServiceLocator.Get<PoolManager>();
                poolManager?.CreatePool(explosionVFXPoolKey, explosionVFXPrefab, 1);
            }

            var tickSystem = ServiceLocator.Get<ITickSystem>();
            if (tickSystem != null)
            {
                tickSystem.Register(this);
            }
        }

        public override void OnDespawn()
        {
            var tickSystem = ServiceLocator.Get<ITickSystem>();
            if (tickSystem != null)
            {
                tickSystem.Unregister(this);
            }
            base.OnDespawn();
        }

        public void OnUpdate(float deltaTime)
        {
            if (_isExploded) return;

            _timeElapsed += deltaTime;
            float t = Mathf.Clamp01(_timeElapsed / _travelDuration);

            Vector3 currentTargetPos = GetTargetPosition();
            Vector3 p0 = _startPosition;
            Vector3 p2 = currentTargetPos;
            Vector3 p1 = (p0 + p2) * 0.5f + Vector3.up * arcHeight;

            Vector3 nextPos = (1f - t) * (1f - t) * p0 + 2f * (1f - t) * t * p1 + t * t * p2;

            Vector3 moveDirection = (nextPos - transform.position).normalized;
            if (moveDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(moveDirection);
            }

            transform.position = nextPos;

            // Also check maxLifetime fallback
            if (t >= 1f || _timeElapsed >= maxLifetime)
            {
                Explode();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_caster != null && other.gameObject == _caster) return;
            if (enemyLayer.value != 0 && ((1 << other.gameObject.layer) & enemyLayer.value) == 0) return;
            Explode();
        }

        public void Block()
        {
            Explode();
        }

        private void Explode()
        {
            if (_isExploded) return;
            _isExploded = true;

            Vector3 hitPoint = transform.position;

            Collider[] hits = Physics.OverlapSphere(hitPoint, explosionRadius, enemyLayer.value != 0 ? enemyLayer : (LayerMask)(-1));
            foreach (var hit in hits)
            {
                if (hit.gameObject != gameObject && (_caster == null || hit.gameObject != _caster))
                {
                    // Execute dynamic collision effects from outside (e.g. DamageEffect & BurnEffect configured in SO)
                    if (_collisionEffects != null)
                    {
                        foreach (var effect in _collisionEffects)
                        {
                            effect?.Execute(_context.WithHit(hit.gameObject, hitPoint));
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(explosionVFXPoolKey))
            {
                EventBus.Publish(new SpawnVFXEvent(explosionVFXPoolKey, hitPoint, Quaternion.identity, 2f, explosionRadius));
            }
            EventBus.Publish(new PlayAudioEvent("SFX_Fire_Explosion", hitPoint));

            ReturnToPool();
        }

        private void ReturnToPool()
        {
            var poolManager = ServiceLocator.Get<PoolManager>();
            if (poolManager != null)
            {
                poolManager.Despawn("Fireball_Projectile", this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            var tickSystem = ServiceLocator.Get<ITickSystem>();
            if (tickSystem != null)
            {
                tickSystem.Unregister(this);
            }
        }
    }
}
