using Dylanng.Core.Pooling;
using UnityEngine;

namespace Dylanng.Gameplay.RPG.Combat.Projectiles
{
    public class Projectile : PoolableObject
    {
        [SerializeField] private float speed = 20f;
        [SerializeField] private float lifetime = 3f;

        private float _damage;
        private GameObject _owner;
        private float _timer;
        private string _poolKey;

        public void Setup(GameObject owner, float damage, string poolKey)
        {
            _owner = owner;
            _damage = damage;
            _poolKey = poolKey;
            _timer = 0;
        }

        private void Update()
        {
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
            _timer += Time.deltaTime;

            if (_timer >= lifetime)
            {
                ReturnToPool();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject == _owner) return;

            // Simple damage logic for now
            if (other.TryGetComponent<Dylanng.Core.IDamageable>(out var damageable))
            {
                damageable.TakeDamage(_damage);
            }

            ReturnToPool();
        }

        private void ReturnToPool()
        {
            var poolManager = Dylanng.Core.ServiceLocator.Get<PoolManager>();
            if (poolManager != null)
            {
                poolManager.Despawn(_poolKey, this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
