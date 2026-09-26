using Game.Data.Hero;
using UnityEngine;
using Dylanng.Gameplay.RPG.Abilities.Events;
using Game.Gameplay.Entities;
using Dylanng.Gameplay.RPG.Abilities;
using Dylanng;

namespace Game.Gameplay.RPG.Abilities.Effects
{
    public class ZoneDamageInstance : PoolableObject, IUpdatable
    {
        public string PoolKey { get; set; }
        
        private GameObject _caster;
        private float _radius;
        private float _duration;
        private float _tickInterval;
        private float _damage;
        private GameElement _element;
        private bool _isAura;

        private float _elapsedTime;
        private float _tickTimer;

        public void Initialize(GameObject caster, float radius, float duration, float tickInterval, float damage, GameElement element, bool isAura)
        {
            _caster = caster;
            _radius = radius;
            _duration = duration;
            _tickInterval = tickInterval;
            _damage = damage;
            _element = element;
            _isAura = isAura;

            _elapsedTime = 0f;
            _tickTimer = tickInterval; 
            
            TickDamage();
        }

        public override void OnSpawn()
        {
            base.OnSpawn();
            ServiceLocator.Get<ITickSystem>()?.Register(this);
        }

        public override void OnDespawn()
        {
            base.OnDespawn();
            ServiceLocator.Get<ITickSystem>()?.Unregister(this);
        }

        public void OnUpdate(float deltaTime)
        {
            if (_duration > 0)
            {
                _elapsedTime += deltaTime;
                if (_elapsedTime >= _duration)
                {
                    DespawnZone();
                    return;
                }
            }

            // Follow caster if it's an aura
            if (_isAura && _caster != null)
            {
                transform.position = _caster.transform.position;
            }
            else if (_isAura && _caster == null)
            {
                // Caster died, aura disappears
                DespawnZone();
                return;
            }

            _tickTimer -= deltaTime;
            if (_tickTimer <= 0f)
            {
                _tickTimer = _tickInterval;
                TickDamage();
            }
        }

        private void TickDamage()
        {
            if (_caster == null && _isAura) return;

            Collider[] colliders = Physics.OverlapSphere(transform.position, _radius);
            foreach (var col in colliders)
            {
                if (col.TryGetComponent<ITargetable>(out var targetable))
                {
                    if (targetable.IsDead) continue;
                    if (_caster != null && targetable.Transform == _caster.transform) continue; // Don't damage self
                    
                    bool isCrit = UnityEngine.Random.value <= 0.1f;
                    EventBus.Publish(new DamageDealtEvent(_caster, targetable.Transform.gameObject, _damage, _element, targetable.Transform.position, isCrit));
                }
            }
        }

        private void DespawnZone()
        {
            if (!string.IsNullOrEmpty(PoolKey))
            {
                ServiceLocator.Get<PoolManager>()?.Despawn(PoolKey, this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawSphere(transform.position, _radius);
        }
    }
}
