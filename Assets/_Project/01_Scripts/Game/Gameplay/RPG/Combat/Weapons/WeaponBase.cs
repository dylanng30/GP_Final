using UnityEngine;

namespace Dylanng.Gameplay.RPG.Combat.Weapons
{
    public abstract class WeaponBase : IWeapon
    {
        [Header("Weapon Stats")]
        [SerializeField] protected string weaponID;
        [SerializeField] protected float baseDamage = 10f;
        [SerializeField] protected float attackRange = 2f;
        [SerializeField] protected float cooldown = 0.5f;

        protected float lastAttackTime;
        protected GameObject owner;

        public virtual void Initialize(GameObject owner)
        {
            this.owner = owner;
        }

        public abstract void StartAttack();
        public virtual void ReleaseAttack() { }
        public virtual void CancelAttack() { }

        public virtual void UpdateWeapon(float deltaTime) { }

        protected bool IsOnCooldown()
        {
            return Time.time < lastAttackTime + cooldown;
        }
    }
}
