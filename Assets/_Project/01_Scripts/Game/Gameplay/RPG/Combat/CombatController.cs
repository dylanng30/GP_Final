using UnityEngine;
using Dylanng.Core;
using Dylanng.Core.Base;
using Dylanng.Services;
using Dylanng.Gameplay.RPG.Combat.Weapons;

namespace Dylanng.Gameplay.RPG.Combat
{
    public class CombatController : MonoBehaviourBase
    {
        [Header("Settings")]
        [SerializeField] private WeaponBase currentWeapon;
        [SerializeField] private KeyCode modeSwitchKey = KeyCode.R;

        private InputManager _inputManager;
        private bool _wasAttackPressed;

        protected override void Awake()
        {
            base.Awake();
            if (currentWeapon != null)
            {
                currentWeapon.Initialize(gameObject);
            }
        }

        private void Start()
        {
            _inputManager = ServiceLocator.Get<InputManager>();
        }

        private void Update()
        {
            if (_inputManager == null) return;

            HandleWeaponInput();
            HandleModeSwitch();

            currentWeapon?.UpdateWeapon(Time.deltaTime);
        }

        private void HandleWeaponInput()
        {
            bool isAttackPressed = _inputManager.AttackInput;

            // Start Attack (On Pressed)
            if (isAttackPressed && !_wasAttackPressed)
            {
                currentWeapon?.StartAttack();
            }
            // Release Attack (On Released)
            else if (!isAttackPressed && _wasAttackPressed)
            {
                currentWeapon?.ReleaseAttack();
            }

            _wasAttackPressed = isAttackPressed;
        }

        private void HandleModeSwitch()
        {
            if (Input.GetKeyDown(modeSwitchKey))
            {
                if (currentWeapon is HybridWeapon hybrid)
                {
                    hybrid.SwitchMode();
                }
            }
        }

        public void SetWeapon(WeaponBase newWeapon)
        {
            currentWeapon = newWeapon;
            currentWeapon.Initialize(gameObject);
        }
    }
}
