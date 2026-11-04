using Dylanng.Gameplay.RPG.Abilities.Projectiles;
using UnityEngine;

namespace Dylanng.Gameplay.RPG.Abilities.Effects
{
    /// <summary>Tường đá: collider thường chặn di chuyển, trigger chặn đạn của kẻ địch.</summary>
    public class StoneWallStructure : MonoBehaviour
    {
        private GameObject _owner;

        public void Initialize(GameObject owner, float lifetime)
        {
            _owner = owner;
            Destroy(gameObject, lifetime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent<IBlockableProjectile>(out var projectile)) return;
            if (IsFriendly(projectile.Caster)) return;

            projectile.Block();
        }

        private bool IsFriendly(GameObject caster)
        {
            return _owner != null && caster != null && caster.layer == _owner.layer;
        }
    }
}
