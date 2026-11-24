using Dylanng;
using Game.Combat;
using Game.Data.Hero;
using Game.Combat.Abilities.Projectiles;
using Game.Gameplay.Entities;
using UnityEngine;

namespace Game.Combat.Abilities.BasicAttack
{
    [RequireComponent(typeof(Collider))]
    public class BasicAttackProjectile : PoolableObject, IUpdatable, IBlockableProjectile
    {
        private HeroInstance _caster;
        private ITargetable _target;
        private float _damage;
        private GameElement _element;
        private float _energyPerHit;
        private string _poolKey;

        private float _speed;
        private float _maxRange;
        private float _maxLifetime;
        private bool _isInvisibleVisual;
        [SerializeField] private LayerMask targetLayerMask;

        private float _timer;
        private float _distanceTraveled;

        public GameObject Caster => _caster != null ? _caster.gameObject : null;

        public void Setup(HeroInstance caster, ITargetable target, float damage, BasicAttackSO config)
        {
            _caster = caster;
            _target = target;
            _damage = damage;

            if (config != null)
            {
                _poolKey = config.ProjectilePoolKey;
                _speed = config.ProjectileSpeed;
                _maxRange = config.MaxRange;
                _maxLifetime = config.MaxLifetime;
                _isInvisibleVisual = config.IsInvisibleVisual;
                _element = config.Element;
                _energyPerHit = config.EnergyPerHit;
            }
            else
            {
                _poolKey = "BasicAttack_Melee_Invisible";
                _speed = 30f;
                _maxRange = 5f;
                _maxLifetime = 2f;
                _isInvisibleVisual = false;
                _element = GameElement.Physical;
                _energyPerHit = 0f;
            }

            if (config != null && config.TargetLayerMask.value != 0)
            {
                targetLayerMask = config.TargetLayerMask;
            }
            else if (caster != null)
            {
                LayerMask casterMask = caster.GetTargetLayerMask();
                if (casterMask.value != 0)
                {
                    targetLayerMask = casterMask;
                }
            }

            _timer = 0f;
            _distanceTraveled = 0f;

            ApplyVisualVisibility(!_isInvisibleVisual);

            // Orient towards target
            if (_target != null && _target.Transform != null)
            {
                Vector3 targetAimPos = _target.Transform.position + Vector3.up * 1f; // Aim at body center
                Vector3 dir = (targetAimPos - transform.position).normalized;
                if (dir != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(dir);
                }
            }
        }

        public override void OnSpawn()
        {
            base.OnSpawn();
            _timer = 0f;
            _distanceTraveled = 0f;

            // Đạn lấy lại từ pool: xoá vệt lửa còn sót ở chỗ cũ (particle mô phỏng trong World).
            foreach (ParticleSystem particles in GetComponentsInChildren<ParticleSystem>(true))
            {
                particles.Clear(false);
                particles.Play(false);
            }
            foreach (TrailRenderer trail in GetComponentsInChildren<TrailRenderer>(true)) trail.Clear();

            var tickSystem = ServiceLocator.Get<ITickSystem>();
            tickSystem?.Register(this);
        }

        public override void OnDespawn()
        {
            base.OnDespawn();
            var tickSystem = ServiceLocator.Get<ITickSystem>();
            tickSystem?.Unregister(this);
        }

        private void ApplyVisualVisibility(bool isVisible)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = isVisible;
                }
            }
        }

        public void OnUpdate(float deltaTime)
        {
            // Kiểm tra xem viên đạn có mục tiêu để đuổi theo hay không
            bool hasTarget = _target != null && !_target.IsDead && _target.Transform != null;

            // Nếu ban đầu có mục tiêu nhưng giờ mục tiêu đã chết/biến mất -> tự hủy đạn
            if (_target != null && (_target.IsDead || _target.Transform == null))
            {
                ReturnToPool();
                return;
            }

            if (hasTarget)
            {
                Vector3 targetAimPos = _target.Transform.position + Vector3.up * 1f;
                Vector3 dirToTarget = (targetAimPos - transform.position).normalized;
                if (dirToTarget != Vector3.zero)
                {
                    transform.forward = dirToTarget;
                }

                float moveStep = _speed * deltaTime;
                float distanceToTarget = Vector3.Distance(transform.position, targetAimPos);

                if (moveStep >= distanceToTarget)
                {
                    transform.position = targetAimPos;
                    HandleHitTarget();
                }
                else
                {
                    transform.position += dirToTarget * moveStep;
                }

                _distanceTraveled += moveStep;
            }
            else
            {
                // Bay thẳng tự do (cho đòn đánh thường thủ công không khóa mục tiêu)
                float moveStep = _speed * deltaTime;
                transform.Translate(Vector3.forward * moveStep, Space.Self);
                _distanceTraveled += moveStep;
            }

            _timer += deltaTime;

            if (_distanceTraveled >= _maxRange || _timer >= _maxLifetime)
            {
                ReturnToPool();
            }
        }

        private void HandleHitTarget()
        {
            if (_target != null && !_target.IsDead)
            {
                GameObject casterGo = _caster != null ? _caster.gameObject : null;
                ServiceLocator.Get<CombatResolver>()?.Deal(casterGo, _target.Transform.gameObject, _damage, _element, transform.position);
                if (_caster != null) _caster.GainEnergy(_energyPerHit);
            }
            ReturnToPool();
        }

        public void Block()
        {
            // Optional: Spawn a block VFX here if needed
            ReturnToPool();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_caster != null && other.gameObject == _caster.gameObject) return;

            if (targetLayerMask.value != 0 && ((1 << other.gameObject.layer) & targetLayerMask.value) == 0)
            {
                return;
            }

            // Check collision with targetable entities
            if (other.TryGetComponent<ITargetable>(out var targetable))
            {
                if (targetable.IsDead || (_caster != null && targetable == (ITargetable)_caster)) return;

                GameObject casterGo = _caster != null ? _caster.gameObject : null;
                ServiceLocator.Get<CombatResolver>()?.Deal(casterGo, other.gameObject, _damage, _element, transform.position);
                if (_caster != null) _caster.GainEnergy(_energyPerHit);

                ReturnToPool();
            }
        }

        private void ReturnToPool()
        {
            var poolManager = ServiceLocator.Get<PoolManager>();
            if (poolManager != null && !string.IsNullOrEmpty(_poolKey))
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
