using System.Collections.Generic;
using Dylanng;
using Game.Combat.Abilities.BasicAttack;
using Game.Animation.Hero;
using Game.Data.Hero;
using Game.Gameplay.Entities;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Đánh thường theo chuỗi combo: bắt đầu animation, chờ điểm trúng rồi tạo đòn.
    /// Cận chiến quét vùng trước mặt, tầm xa bắn đạn.
    /// </summary>
    public sealed class BasicAttackRunner
    {
        private const string FallbackPoolKey = "BasicAttack_Melee_Invisible";
        private const float PendingTimeout = 1f;
        private const float FallbackCooldown = 1f;
        private const float FallbackAttack = 50f;
        private const float BufferWindow = 0.3f;
        private const float LungeDuration = 0.12f;
        private static readonly Vector3 FallbackSpawnOffset = new Vector3(0f, 1.2f, 0.8f);
        private static readonly Vector3 AimHeightOffset = Vector3.up;

        private readonly HeroInstance _owner;
        private readonly BasicAttackSO _config;
        private readonly HeroAnimatorController _animator;
        private readonly AttachPoints _attachPoints;
        private readonly List<ITargetable> _meleeCandidates = new List<ITargetable>();

        private float _lastAttackTime = float.NegativeInfinity;
        private int _comboIndex;

        private bool _isPending;
        private ITargetable _pendingTarget;
        private ComboStep _pendingStep;
        private float _pendingStartTime;
        private float _pendingDamage;

        private bool _hasBufferedAttack;
        private ITargetable _bufferedTarget;
        private float _bufferExpireTime;

        public float Cooldown => _config != null ? _config.AttackCooldown : FallbackCooldown;
        public bool CanAttack => Time.time - _lastAttackTime >= Cooldown && !_isPending;
        public int ComboIndex => _comboIndex;

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

            if (!CanAttack)
            {
                BufferAttack(target);
                return;
            }

            StartAttack(target);
        }

        public void Tick()
        {
            if (_isPending && Time.time - _pendingStartTime > PendingTimeout) ExecutePending();

            if (!_hasBufferedAttack) return;

            if (Time.time > _bufferExpireTime)
            {
                _hasBufferedAttack = false;
            }
            else if (CanAttack)
            {
                _hasBufferedAttack = false;
                Attack(_bufferedTarget);
            }
        }

        /// <summary>Gọi từ animation event tại thời điểm đòn đánh trúng.</summary>
        public void OnAnimationHit()
        {
            if (_isPending) ExecutePending();
        }

        private void BufferAttack(ITargetable target)
        {
            _hasBufferedAttack = true;
            _bufferedTarget = target;
            _bufferExpireTime = Time.time + BufferWindow;
        }

        private void StartAttack(ITargetable target)
        {
            AdvanceCombo();
            _lastAttackTime = Time.time;

            if (target != null) _owner.FaceTowards(target.Transform.position);

            _animator?.PlayAttack();
            if (_config != null && _config.IsMelee)
            {
                EventBus.Publish(new Game.Combat.Abilities.Events.MeleeAttackStartedEvent(_owner.gameObject, _config.Element));
            }

            ComboStep step = _config != null ? _config.GetComboStep(_comboIndex) : ComboStep.Default;
            float attack = _owner.Stats != null ? _owner.Stats.ATK : FallbackAttack;
            float multiplier = _config != null ? _config.DamageMultiplier : 1f;

            _isPending = true;
            _pendingTarget = target;
            _pendingStep = step;
            _pendingDamage = attack * multiplier * step.DamageMultiplier;
            _pendingStartTime = Time.time;

            if (step.LungeDistance > 0f) _owner.Lunge(step.LungeDistance, LungeDuration);

            if (_animator == null) ExecutePending();
        }

        private void AdvanceCombo()
        {
            int stepCount = _config != null ? _config.Combo.Count : 0;
            bool isComboAlive = stepCount > 0 && Time.time - _lastAttackTime <= _config.ComboResetTime;

            _comboIndex = isComboAlive ? (_comboIndex + 1) % stepCount : 0;
        }

        private void ExecutePending()
        {
            _isPending = false;

            if (_config != null && _config.IsMelee)
            {
                ExecuteMeleeHit();
            }
            else
            {
                ExecuteProjectile();
            }
        }

        private void ExecuteMeleeHit()
        {
            Transform owner = _owner.transform;
            TargetQuery.Collect(
                owner.position, _pendingStep.Range, _owner.GetTargetLayerMask(), owner, _meleeCandidates);

            var resolver = ServiceLocator.Get<CombatResolver>();
            bool hasHit = false;

            foreach (ITargetable candidate in _meleeCandidates)
            {
                if (!IsInsideArc(owner, candidate.Transform.position)) continue;

                hasHit = true;
                resolver?.Deal(owner.gameObject, candidate.Transform.gameObject, _pendingDamage, _config.Element,
                    candidate.Transform.position);
            }

            if (hasHit) _owner.GainEnergy(_config.EnergyPerHit);

            EventBus.Publish(new Game.Combat.Abilities.Events.MeleeSwingEvent(owner.gameObject, _config.Element, _pendingStep, hasHit));
        }

        private bool IsInsideArc(Transform owner, Vector3 point)
        {
            Vector3 toPoint = point - owner.position;
            toPoint.y = 0f;
            return Vector3.Angle(owner.forward, toPoint) <= _pendingStep.ArcAngle * 0.5f;
        }

        private void ExecuteProjectile()
        {
            Transform muzzle = ResolveSpawnPose(_pendingTarget, out Vector3 position, out Quaternion rotation);

            string poolKey = _config != null ? _config.ProjectilePoolKey : FallbackPoolKey;
            var projectile = ServiceLocator.Get<PoolManager>()?.Spawn<BasicAttackProjectile>(poolKey, position, rotation);
            EventBus.Publish(new Game.Combat.Abilities.Events.RangedShotEvent(_owner.gameObject, _config, position, rotation, muzzle));

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

        /// <summary>Tính vị trí và hướng bắn. Trả về transform điểm bắn nếu có.</summary>
        private Transform ResolveSpawnPose(ITargetable target, out Vector3 position, out Quaternion rotation)
        {
            Transform owner = _owner.transform;
            position = owner.TransformPoint(_config != null ? _config.SpawnOffset : FallbackSpawnOffset);
            rotation = owner.rotation;

            Transform spawnPoint = _config != null ? _attachPoints.Get(_config.SpawnPointName) : null;
            if (spawnPoint != null)
            {
                position = spawnPoint.position;
                rotation = owner.rotation;
            }

            if (target == null || target.Transform == null) return spawnPoint;

            Vector3 direction = (target.Transform.position + AimHeightOffset - position).normalized;
            if (direction != Vector3.zero) rotation = Quaternion.LookRotation(direction);
            return spawnPoint;
        }
    }
}
