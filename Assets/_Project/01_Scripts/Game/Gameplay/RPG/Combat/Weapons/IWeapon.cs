using UnityEngine;

namespace Dylanng.Gameplay.RPG.Combat.Weapons
{
    public interface IWeapon
    {
        void StartAttack();
        void ReleaseAttack();
        void CancelAttack();
        void UpdateWeapon(float deltaTime);
    }
}
