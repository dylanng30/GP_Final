using Dylanng.Core;
using Game.Events;
using Game.Managers.Party;
using Game.Gameplay.Entities;
using UnityEngine;

namespace Game.Gameplay.Controllers
{
    public class CameraController : MonoBehaviour
    {
        [Header("--- CAMERA SETTINGS ---")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -10f);
        [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1f, 0f);
        [SerializeField] private float positionSmoothSpeed = 5f;
        [SerializeField] private float rotationSmoothSpeed = 5f;

        private Transform _target;

        private void OnEnable()
        {
            EventBus.Subscribe<ActiveHeroChangedEvent>(OnActiveHeroChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ActiveHeroChangedEvent>(OnActiveHeroChanged);
        }
        
        /*private void Start()
        {
            UpdateTarget();
        }*/

        private void OnActiveHeroChanged(ActiveHeroChangedEvent evt)
        {
            UpdateTarget();
        }

        private void UpdateTarget()
        {
            var partyCombatManager = ServiceLocator.Get<PartyCombatManager>();
            if (partyCombatManager != null)
            {
                HeroInstance activeHero = partyCombatManager.GetActiveHero();
                if (activeHero != null)
                {
                    _target = activeHero.transform;
                }
                else
                {
                    _target = null;
                }
            }
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            // Smooth Position
            Vector3 desiredPosition = _target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, positionSmoothSpeed * Time.deltaTime);

            // Smooth Rotation
            Vector3 lookTarget = _target.position + lookOffset;
            Vector3 directionToTarget = lookTarget - transform.position;
            if (directionToTarget != Vector3.zero)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(directionToTarget);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSmoothSpeed * Time.deltaTime);
            }
        }
    }
}
