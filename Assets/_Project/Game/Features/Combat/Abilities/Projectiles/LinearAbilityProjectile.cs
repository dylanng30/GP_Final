using System.Collections.Generic;
using Dylanng;
using Game.Combat.Abilities.Events;
using Game.Gameplay.Controllers;
using Game.Gameplay.Entities;
using Game.Managers.Party;
using UnityEngine;

namespace Game.Combat.Abilities.Projectiles
{
    /// <summary>
    /// Đạn skill bay thẳng. Trúng kẻ địch đầu tiên trên đường bay thì nổ, chạy CollisionEffects tại điểm trúng.
    /// Hết tầm bay thì nổ tại điểm cuối nếu ExplodeAtEnd, không thì tắt dần.
    /// </summary>
    public class LinearAbilityProjectile : PoolableObject, IAbilityProjectile, IUpdatable, IBlockableProjectile
    {
        [Tooltip("Phải trùng ProjectilePoolKey của ProjectileEffect để trả đúng pool")]
        [SerializeField] private string poolKey = "";
        [SerializeField] private float speed = 25f;
        [Tooltip("Tầm bay tối đa. 0 nghĩa là dùng Range của skill")]
        [SerializeField] private float maxDistance = 0f;
        [Tooltip("Bán kính va chạm khi quét đường bay")]
        [SerializeField] private float hitRadius = 0.4f;
        [Tooltip("Độ cao nhắm vào thân mục tiêu tính từ chân")]
        [SerializeField] private float aimHeight = 1f;
        [SerializeField] private bool explodeAtEnd = false;

        [Header("--- Presentation ---")]
        [Tooltip("VFX lúc đạn rời tay/nòng, đặt tại điểm bắn theo hướng bay")]
        [SerializeField] private GameObject launchVfx;
        [SerializeField] private float launchVfxScale = 1f;
        [SerializeField] private float impactShake = 0f;
        [SerializeField] private string launchSoundKey = "";
        [SerializeField] private string impactSoundKey = "";

        private AbilityContext _context;
        private List<IAbilityEffect> _collisionEffects;
        private LayerMask _enemyMask;
        private Vector3 _direction;
        private float _range;
        private float _traveled;
        private bool _isDone;

        public GameObject Caster => _context.Caster;

        public void InitializeProjectile(in AbilityContext context, List<IAbilityEffect> collisionEffects = null)
        {
            _context = context;
            _collisionEffects = collisionEffects;
            _enemyMask = context.Caster != null ? TargetQuery.GetEnemyMask(context.Caster) : (LayerMask)0;
            _range = maxDistance > 0f ? maxDistance : (context.Ability != null && context.Ability.Range > 0f ? context.Ability.Range : 10f);
            _traveled = 0f;
            _isDone = false;

            _direction = ResolveDirection(context);
            transform.rotation = Quaternion.LookRotation(_direction);

            if (launchVfx != null)
            {
                GameObject vfx = VfxSpawner.Spawn(launchVfx, transform.position, transform.rotation, null);
                vfx.transform.localScale *= launchVfxScale;
                Destroy(vfx, 1.5f);
            }
            PlaySound(launchSoundKey, transform.position);
        }

        /// <summary>Nhắm thẳng vào thân mục tiêu nếu có, không thì bay ngang theo hướng skill.</summary>
        private Vector3 ResolveDirection(in AbilityContext context)
        {
            if (context.Target != null)
            {
                Vector3 toTarget = context.Target.transform.position + Vector3.up * aimHeight - transform.position;
                if (toTarget.sqrMagnitude > 0.01f) return toTarget.normalized;
            }

            Vector3 direction = context.Direction;
            return direction.sqrMagnitude > 0.001f ? direction : transform.forward;
        }

        public override void OnSpawn()
        {
            base.OnSpawn();
            _isDone = false;

            // Lấy lại từ pool: xoá vệt lửa còn sót ở vị trí cũ.
            foreach (ParticleSystem particles in GetComponentsInChildren<ParticleSystem>(true))
            {
                particles.Clear(false);
                particles.Play(false);
            }
            foreach (TrailRenderer trail in GetComponentsInChildren<TrailRenderer>(true)) trail.Clear();

            ServiceLocator.Get<ITickSystem>()?.Register(this);
        }

        public override void OnDespawn()
        {
            ServiceLocator.Get<ITickSystem>()?.Unregister(this);
            base.OnDespawn();
        }

        public void OnUpdate(float deltaTime)
        {
            if (_isDone) return;

            float step = Mathf.Min(speed * deltaTime, _range - _traveled);
            Vector3 start = transform.position;

            if (TryFindHit(start, step, out ITargetable target, out Vector3 hitPoint))
            {
                transform.position = hitPoint;
                Explode(target.Transform.gameObject, hitPoint);
                return;
            }

            transform.position = start + _direction * step;
            _traveled += step;

            if (_traveled >= _range - 0.001f)
            {
                if (explodeAtEnd) Explode(null, transform.position);
                else ReturnToPool();
            }
        }

        private bool TryFindHit(Vector3 start, float step, out ITargetable target, out Vector3 hitPoint)
        {
            target = null;
            hitPoint = start;
            if (_enemyMask.value == 0 || step <= 0f) return false;

            RaycastHit[] hits = Physics.SphereCastAll(start, hitRadius, _direction, step, _enemyMask, QueryTriggerInteraction.Collide);
            float best = float.MaxValue;
            foreach (RaycastHit hit in hits)
            {
                ITargetable candidate = hit.collider.GetComponentInParent<ITargetable>();
                if (candidate == null || candidate.IsDead || candidate.Transform.gameObject == _context.Caster) continue;
                if (hit.distance >= best) continue;

                best = hit.distance;
                target = candidate;
                hitPoint = hit.distance > 0f ? start + _direction * hit.distance : start;
            }
            return target != null;
        }

        public void Block()
        {
            Explode(null, transform.position);
        }

        private void Explode(GameObject hitTarget, Vector3 point)
        {
            if (_isDone) return;
            _isDone = true;

            if (_collisionEffects != null)
            {
                AbilityContext hit = _context.WithHit(hitTarget, point);
                foreach (IAbilityEffect effect in _collisionEffects) effect?.Execute(hit);
            }

            PlaySound(impactSoundKey, point);
            if (impactShake > 0f && IsActiveHero(_context.Caster) && CameraController.Instance != null)
            {
                CameraController.Instance.AddShake(impactShake);
            }

            ReturnToPool();
        }

        private static bool IsActiveHero(GameObject caster)
        {
            var party = ServiceLocator.Get<PartyCombatManager>();
            HeroInstance hero = party != null ? party.GetActiveHero() : null;
            return hero != null && hero.gameObject == caster;
        }

        private static void PlaySound(string key, Vector3 position)
        {
            if (!string.IsNullOrEmpty(key)) EventBus.Publish(new PlayAudioEvent(key, position));
        }

        private void ReturnToPool()
        {
            _isDone = true;
            var poolManager = ServiceLocator.Get<PoolManager>();
            if (poolManager != null && !string.IsNullOrEmpty(poolKey)) poolManager.Despawn(poolKey, this);
            else gameObject.SetActive(false);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ServiceLocator.Get<ITickSystem>()?.Unregister(this);
        }
    }
}
