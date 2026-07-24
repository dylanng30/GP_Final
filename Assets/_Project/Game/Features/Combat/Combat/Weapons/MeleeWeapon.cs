using System.Collections.Generic;
using UnityEngine;

namespace Dylanng.Gameplay.RPG.Combat.Weapons
{
    public class MeleeWeapon : WeaponBase
    {
        [System.Serializable]
        public struct MeleeAttack
        {
            public string animationTrigger;
            public float damageMultiplier;
            public float comboWindow; // Time until combo resets
        }

        [SerializeField] private List<MeleeAttack> comboAttacks;
        
        private int _currentComboIndex = 0;
        private float _lastAttackTimestamp;

        public override void StartAttack()
        {
            if (IsOnCooldown()) return;

            UpdateCombo();
            ExecuteAttack();
        }

        private void UpdateCombo()
        {
            if (Time.time - _lastAttackTimestamp > comboAttacks[_currentComboIndex].comboWindow)
            {
                _currentComboIndex = 0;
            }
            else
            {
                _currentComboIndex = (_currentComboIndex + 1) % comboAttacks.Count;
            }
        }

        private void ExecuteAttack()
        {
            var attack = comboAttacks[_currentComboIndex];
            _lastAttackTimestamp = Time.time;
            lastAttackTime = Time.time;

            Debug.Log($"[Melee] Executing {attack.animationTrigger} (Combo Index: {_currentComboIndex}) - Damage: {baseDamage * attack.damageMultiplier}");

            // Trigger animation if animator exists
            if (owner.TryGetComponent<Animator>(out var animator))
            {
                animator.SetTrigger(attack.animationTrigger);
            }

            // Real damage would be handled by animation events or a temporary hit trigger
            // For this mechanical demo, we log the intent.
        }

        public override void UpdateWeapon(float deltaTime)
        {
            // Optional: Reset combo if window exceeded
            if (_currentComboIndex > 0 && Time.time - _lastAttackTimestamp > comboAttacks[_currentComboIndex].comboWindow)
            {
                _currentComboIndex = 0;
                Debug.Log("[Melee] Combo Reset");
            }
        }
    }
}
