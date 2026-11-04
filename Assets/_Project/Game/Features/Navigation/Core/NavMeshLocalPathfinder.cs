using UnityEngine;
using UnityEngine.AI;
using System;

namespace Game.Navigation.Core
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class NavMeshLocalPathfinder : MonoBehaviour, ILocalPathfinder
    {
        private NavMeshAgent navAgent;
        public event Action OnDestinationReached;
        
        private bool isMovingToTarget = false;
        private float stoppingDistanceOffset = 0.1f;

        public bool IsMoving => isMovingToTarget;

        private void Awake()
        {
            navAgent = GetComponent<NavMeshAgent>();
        }

        public void SetDestination(Vector3 target)
        {
            isMovingToTarget = true;
            navAgent.isStopped = false;
            navAgent.SetDestination(target);
        }

        public void Stop()
        {
            isMovingToTarget = false;
            if (navAgent.isActiveAndEnabled)
            {
                navAgent.isStopped = true;
            }
        }

        private void Update()
        {
            if (isMovingToTarget)
            {
                // Check if we've reached the destination
                if (!navAgent.pathPending)
                {
                    if (navAgent.remainingDistance <= navAgent.stoppingDistance + stoppingDistanceOffset)
                    {
                        if (!navAgent.hasPath || navAgent.velocity.sqrMagnitude == 0f)
                        {
                            isMovingToTarget = false;
                            OnDestinationReached?.Invoke();
                        }
                    }
                }
            }
        }
    }
}
