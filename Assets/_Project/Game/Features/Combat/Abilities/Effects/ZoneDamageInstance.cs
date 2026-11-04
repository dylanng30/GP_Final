using Game.Combat;
using Game.Data.Hero;
using UnityEngine;
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
        private Game.Combat.Domain.StatusDefinitionSO _appliedStatus;
        private LayerMask _enemyMask;
        private readonly System.Collections.Generic.List<ITargetable> _hitTargets = new System.Collections.Generic.List<ITargetable>();

        private float _elapsedTime;
        private float _tickTimer;

        public void Initialize(GameObject caster, float radius, float duration, float tickInterval, float damage, GameElement element, bool isAura, Game.Combat.Domain.StatusDefinitionSO appliedStatus = null)
        {
            _caster = caster;
            _radius = radius;
            _duration = duration;
            _tickInterval = tickInterval;
            _damage = damage;
            _element = element;
            _isAura = isAura;
            _appliedStatus = appliedStatus;
            _enemyMask = TargetQuery.GetEnemyMask(caster);

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

            TargetQuery.Collect(transform.position, _radius, _enemyMask, _caster != null ? _caster.transform : null, _hitTargets);

            foreach (ITargetable target in _hitTargets)
            {
                ServiceLocator.Get<CombatResolver>()?.Deal(_caster, target.Transform.gameObject, _damage, _element, target.Transform.position);
                if (_appliedStatus != null) ServiceLocator.Get<CombatResolver>()?.ApplyStatus(_caster, target.Transform.gameObject, _appliedStatus);
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
