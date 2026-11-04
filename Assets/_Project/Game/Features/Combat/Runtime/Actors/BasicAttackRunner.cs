using Dylanng;
using Dylanng.Gameplay.RPG.Abilities.BasicAttack;
using Game.Animation.Hero;
using Game.Data.Hero;
using Game.Gameplay.Entities;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>Thực hiện đánh thường: bắt đầu animation, chờ điểm trúng rồi mới tạo đòn.</summary>
    public sealed class BasicAttackRunner
    {
        private const string FallbackPoolKey = "BasicAttack_Melee_Invisible";
        private const float PendingTimeout = 1f;
        private const float FallbackCooldown = 1f;
        private const float FallbackAttack = 50f;
        private static readonly Vector3 FallbackSpawnOffset = new Vector3(0f, 1.2f, 0.8f);
        private static readonly Vector3 AimHeightOffset = Vector3.up;

        private readonly HeroInstance _owner;
        private readonly BasicAttackSO _config;
        private readonly HeroAnimatorController _animator;
        private readonly AttachPoints _attachPoints;

        private float _lastAttackTime = float.NegativeInfinity;
        private bool _isPending;
        private ITargetable _pendingTarget;
        private float _pendingStartTime;
        private float _pendingDamage;

        public float Cooldown => _config != null ? _config.AttackCooldown : FallbackCooldown;
        public bool CanAttack => Time.time - _lastAttackTime >= Cooldown;

        public BasicAttackRunner(
            HeroInstance owner, BasicAttackSO config, HeroAnimatorController animator, AttachPoints attachPoints)
        {
            _owner = owner;
            _config = config;
            _animator = animator;
            _attachPoints = attachPoints;
        }

        public void PrewarmPool()
        {
            if (_config == null || _config.ProjectilePrefab == null) return;

            ServiceLocator.Get<PoolManager>()?.CreatePool(_config.ProjectilePoolKey, _config.ProjectilePrefab, 10);
        }

        public void Attack(ITargetable target)
        {
            if (target != null && (target.IsDead || target.Transform == null)) return;
            if (!CanAttack) return;

            _lastAttackTime = Time.time;

            if (target != null) _owner.FaceTowards(target.Transform.position);

            _animator?.PlayAttack();

            float attack = _owner.Stats != null ? _owner.Stats.ATK : FallbackAttack;
            float multiplier = _config != null ? _config.DamageMultiplier : 1f;

            _isPending = true;
            _pendingTarget = target;
            _pendingDamage = attack * multiplier;
            _pendingStartTime = Time.time;

            if (_animator == null) ExecutePending();
        }

        public void Tick()
        {
            if (_isPending && Time.time - _pendingStartTime > PendingTimeout) ExecutePending();
        }

        /// <summary>Gọi từ animation event tại thời điểm đòn đánh trúng.</summary>
        public void OnAnimationHit()
        {
            if (_isPending) ExecutePending();
        }

        private void ExecutePending()
        {
            _isPending = false;

            ResolveSpawnPose(_pendingTarget, out Vector3 position, out Quaternion rotation);

            string poolKey = _config != null ? _config.ProjectilePoolKey : FallbackPoolKey;
            var projectile = ServiceLocator.Get<PoolManager>()?.Spawn<BasicAttackProjectile>(poolKey, position, rotation);

            if (projectile != null)
            {
                projectile.Setup(_owner, _pendingTarget, _pendingDamage, _config);
            }
            else
            {
                DealDirectHit();
            }
        }

        private void DealDirectHit()
        {
            if (_pendingTarget == null || _pendingTarget.Transform == null) return;

            GameElement element = _config != null ? _config.Element : GameElement.Physical;
            GameObject targetObject = _pendingTarget.Transform.gameObject;

            ServiceLocator.Get<CombatResolver>()?.Deal(
                _owner.gameObject, targetObject, _pendingDamage, element, _pendingTarget.Transform.position);

            if (_config != null) _owner.GainEnergy(_config.EnergyPerHit);
        }

        private void ResolveSpawnPose(ITargetable target, out Vector3 position, out Quaternion rotation)
        {
            Transform owner = _owner.transform;
            position = owner.TransformPoint(_config != null ? _config.SpawnOffset : FallbackSpawnOffset);
            rotation = owner.rotation;

            Transform spawnPoint = _config != null ? _attachPoints.Get(_config.SpawnPointName) : null;
            if (spawnPoint != null)
            {
                position = spawnPoint.position;
                rotation = spawnPoint.rotation;
            }

            if (target == null || target.Transform == null) return;

            Vector3 direction = (target.Transform.position + AimHeightOffset - position).normalized;
            if (direction != Vector3.zero) rotation = Quaternion.LookRotation(direction);
        }
    }
}
