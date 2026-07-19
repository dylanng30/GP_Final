using Game.Gameplay.Entities;
using UnityEngine;

namespace Game.Gameplay.Controllers
{
    public class PlayerTargetingSystem : MonoBehaviour
    {
        [Header("--- Targeting Config ---")]
        [SerializeField] private float scanRadius = 10f;
        [SerializeField] private float loseTargetRadius = 13f;
        [SerializeField] private LayerMask enemyLayerMask;
        [SerializeField] private float scanInterval = 0.3f;

        private ITargetable _currentTarget;
        private float _scanTimer;

        public ITargetable CurrentTarget => _currentTarget;

        private void Update()
        {
            _scanTimer -= Time.deltaTime;
            if (_scanTimer <= 0f)
            {
                _scanTimer = scanInterval;
                ScanForTarget();
            }

            // Validate current target
            if (_currentTarget != null)
            {
                if (_currentTarget.IsDead || _currentTarget.Transform == null || 
                    Vector3.Distance(transform.position, _currentTarget.Transform.position) > loseTargetRadius)
                {
                    ClearTarget();
                }
            }
        }

        private void ScanForTarget()
        {
            Collider[] colliders = enemyLayerMask.value != 0 
                ? Physics.OverlapSphere(transform.position, scanRadius, enemyLayerMask)
                : Physics.OverlapSphere(transform.position, scanRadius);

            ITargetable nearestTarget = null;
            float minSqrDist = float.MaxValue;

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null && colliders[i].TryGetComponent<ITargetable>(out var targetable))
                {
                    if (targetable.IsDead || targetable.Transform == transform) continue;

                    float sqrDist = (targetable.Transform.position - transform.position).sqrMagnitude;
                    if (sqrDist < minSqrDist && sqrDist <= scanRadius * scanRadius)
                    {
                        minSqrDist = sqrDist;
                        nearestTarget = targetable;
                    }
                }
            }

            if (nearestTarget != _currentTarget)
            {
                SetTarget(nearestTarget);
            }
        }

        public void SetTarget(ITargetable newTarget)
        {
            if (_currentTarget != null)
            {
                _currentTarget.OnTargeted(false);
            }

            _currentTarget = newTarget;

            if (_currentTarget != null)
            {
                _currentTarget.OnTargeted(true);
            }
        }

        public void ClearTarget()
        {
            SetTarget(null);
        }

        private void OnDisable()
        {
            ClearTarget();
        }

        private void OnDestroy()
        {
            ClearTarget();
        }
    }
}
