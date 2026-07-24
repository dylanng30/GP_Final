using UnityEngine;
using Dylanng.Gameplay.RPG.Combat.Projectiles;

namespace Dylanng.Gameplay.RPG.Combat.Weapons
{
    public class BowWeapon : WeaponBase
    {
        [Header("Bow Settings")]
        [SerializeField] private float drawTime = 1.0f;
        [SerializeField] private string projectilePoolKey = "Arrow";
        [SerializeField] private Transform firePoint;
        [SerializeField] private Projectile projectilePrefab;

        public float DrawTime => drawTime;
        public float CurrentPower { get; private set; }

        public BowState DrawState { get; private set; }
        public BowState HoldState { get; private set; }
        public BowState ShootState { get; private set; }

        private BowState _currentState;

        public override void Initialize(GameObject owner)
        {
            base.Initialize(owner);
            DrawState = new DrawState(this);
            HoldState = new HoldState(this);
            ShootState = new ShootState(this);

            // Ensure pool is initialized (if Manager exists)
            var poolManager = ServiceLocator.Get<PoolManager>();
            if (poolManager != null && projectilePrefab != null)
            {
                poolManager.CreatePool(projectilePoolKey, projectilePrefab, 10);
            }
        }

        public override void StartAttack()
        {
            if (IsOnCooldown()) return;
            TransitionTo(DrawState);
        }

        public override void ReleaseAttack()
        {
            _currentState?.HandleInputRelease();
        }

        public override void UpdateWeapon(float deltaTime)
        {
            _currentState?.Update(deltaTime);
        }

        public void TransitionTo(BowState newState)
        {
            _currentState?.Exit();
            _currentState = newState;
            _currentState?.Enter();
        }

        public void SetCurrentPower(float power) => CurrentPower = power;

        public void FireProjectile()
        {
            var poolManager = ServiceLocator.Get<PoolManager>();
            if (poolManager == null) return;

            var proj = poolManager.Spawn<Projectile>(projectilePoolKey, firePoint.position, firePoint.rotation);
            if (proj != null)
            {
                proj.Setup(owner, baseDamage * CurrentPower, projectilePoolKey);
                lastAttackTime = Time.time;
            }
        }

        public void SetAnimatorTrigger(string trigger)
        {
            if (owner.TryGetComponent<Animator>(out var animator))
            {
                animator.SetTrigger(trigger);
            }
        }
    }
}
