using UnityEngine;
using Dylanng.Core.Pooling;
using Dylanng.Gameplay.RPG.Abilities.Projectiles;
using UnityEngine.VFX;

namespace Dylanng.Gameplay.RPG.Abilities.Effects
{
    [RequireComponent(typeof(SphereCollider), typeof(Rigidbody))]
    public class ScienceFictionShield : PoolableObject
    {
        [Header("--- REFERENCES ---")] 
        [SerializeField] private SphereCollider col;
        [SerializeField] private Rigidbody rb;
        
        [Header("VFX Settings")]
        public GameObject ShieldRipplesPrefab;

        private GameObject _shieldOwner;

        public void Initialize(GameObject owner)
        {
            _shieldOwner = owner;
        }

        protected override void Awake()
        {
            base.Awake();
            
            if (col != null)
            {
                col.isTrigger = true;
            }
            
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Check if the colliding object is a projectile
            if (other.TryGetComponent<IBlockableProjectile>(out var projectile))
            {
                // We don't block projectiles shot by the shield owner
                if (_shieldOwner != null && projectile.Caster == _shieldOwner)
                {
                    return;
                }

                // Block the projectile
                projectile.Block();
                
                // Spawn ripple VFX at the hit point
                if (ShieldRipplesPrefab != null)
                {
                    var ripples = Instantiate(ShieldRipplesPrefab, transform);
                    var vfx = ripples.GetComponent<VisualEffect>();
                    if (vfx != null)
                    {
                        // Get the closest point on the projectile's collider to this shield's center
                        Vector3 hitPoint = other.ClosestPoint(transform.position);
                        vfx.SetVector3("SphereCenter", hitPoint);
                    }
                    Destroy(ripples, 2f);
                }

                Debug.Log($"[ScienceFictionShield] Đã chặn một viên đạn từ {projectile.Caster?.name}!");
            }
        }
    }
}
