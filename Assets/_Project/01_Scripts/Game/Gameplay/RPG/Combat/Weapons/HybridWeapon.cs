using UnityEngine;

namespace Dylanng.Gameplay.RPG.Combat.Weapons
{
    public class HybridWeapon : WeaponBase
    {
        public enum WeaponMode { Melee, Ranged }

        [Header("Hybrid Modes")]
        [SerializeField] private MeleeWeapon meleeStrategy;
        [SerializeField] private BowWeapon rangedStrategy;
        
        [SerializeField] private WeaponMode currentMode = WeaponMode.Melee;

        public override void Initialize(GameObject owner)
        {
            base.Initialize(owner);
            meleeStrategy?.Initialize(owner);
            rangedStrategy?.Initialize(owner);
        }

        public void SwitchMode()
        {
            currentMode = (currentMode == WeaponMode.Melee) ? WeaponMode.Ranged : WeaponMode.Melee;
            Debug.Log($"[Spear] Switched to {currentMode} mode.");
            
            // Optional: Trigger "Switch" animation
            if (owner.TryGetComponent<Animator>(out var animator))
            {
                animator.SetTrigger("BaseSwitch");
            }
        }

        public override void StartAttack()
        {
            GetActiveWeapon()?.StartAttack();
        }

        public override void ReleaseAttack()
        {
            GetActiveWeapon()?.ReleaseAttack();
        }

        public override void UpdateWeapon(float deltaTime)
        {
            meleeStrategy?.UpdateWeapon(deltaTime);
            rangedStrategy?.UpdateWeapon(deltaTime);
        }

        private IWeapon GetActiveWeapon()
        {
            return currentMode == WeaponMode.Melee ? (IWeapon)meleeStrategy : (IWeapon)rangedStrategy;
        }
    }
}
