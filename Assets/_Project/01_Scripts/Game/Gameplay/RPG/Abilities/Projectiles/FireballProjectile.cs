using Dylanng.Core;
using Dylanng.Core.Pooling;
using Dylanng.Dylanng_EventBus;
using Dylanng.Gameplay.RPG.Abilities.Effects;
using Dylanng.Gameplay.RPG.Abilities.Events;
using Dylanng.Gameplay.RPG.Stats;
using UnityEngine;

namespace Dylanng.Gameplay.RPG.Abilities.Projectiles
{
    public class FireballProjectile : PoolableObject, IAbilityProjectile
    {
        [SerializeField] private float speed = 15f;
        [SerializeField] private float explosionRadius = 3f;
        [SerializeField] private float maxLifetime = 5f;
        [SerializeField] private LayerMask enemyLayer;

        private GameObject _caster;
        private DamageEffect _damageEffect;
        private StatusEffect _stunEffect;
        private float _timer;

        public void InitializeProjectile(GameObject caster)
        {
            _caster = caster;
            _timer = 0f;

            _damageEffect = new DamageEffect
            {
                Element = ElementType.Pyro,
                BaseDamage = 150f,
                AtkMultiplier = 2.0f
            };

            _stunEffect = new StatusEffect
            {
                TargetStat = StatType.Speed,
                ModifierValue = -1000f,
                ModType = StatModType.Flat,
                Duration = 2.0f,
                Chance = 0.3f
            };
        }

        public override void OnSpawn()
        {
            base.OnSpawn();
            _timer = 0f;
        }

        private void Update()
        {
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
            _timer += Time.deltaTime;

            if (_timer >= maxLifetime)
            {
                ReturnToPool();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_caster != null && other.gameObject == _caster) return;

            Vector3 hitPoint = transform.position;

            Collider[] hits = Physics.OverlapSphere(hitPoint, explosionRadius, enemyLayer);
            foreach (var hit in hits)
            {
                if (hit.gameObject != gameObject && (_caster == null || hit.gameObject != _caster))
                {
                    _damageEffect?.Execute(_caster, hit.gameObject, hitPoint);
                    _stunEffect?.Execute(_caster, hit.gameObject, hitPoint);
                }
            }

            EventBus.Publish(new SpawnVFXEvent("VFX_Fireball_Explosion", hitPoint, Quaternion.identity, 2f));
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
    }
}
