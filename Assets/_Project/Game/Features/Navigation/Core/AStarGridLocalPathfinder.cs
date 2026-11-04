using UnityEngine;
using System;
using System.Collections.Generic;

namespace Game.Navigation.Core
{
    /// <summary>
    /// Placeholder structure for A* Grid Pathfinding. 
    /// To be fully implemented later as decided by the user.
    /// </summary>
    public class AStarGridLocalPathfinder : MonoBehaviour, ILocalPathfinder
    {
        public event Action OnDestinationReached;
        
        private bool isMovingToTarget = false;
        public float moveSpeed = 5f;
        
        private Queue<Vector3> currentPath;
        private Vector3 currentTargetWaypoint;

        public bool IsMoving => isMovingToTarget;

        public void SetDestination(Vector3 target)
        {
            // TODO: Implement grid based A* calculation here.
            // For now, we simulate a straight line path to the target.
            
            currentPath = new Queue<Vector3>();
            currentPath.Enqueue(target); 
            
            if (currentPath.Count > 0)
            {
                currentTargetWaypoint = currentPath.Dequeue();
                isMovingToTarget = true;
            }
        }

        public void Stop()
        {
            isMovingToTarget = false;
            currentPath?.Clear();
        }

        private void Update()
        {
            if (isMovingToTarget)
            {
                // Simple movement logic towards the waypoint
                transform.position = Vector3.MoveTowards(transform.position, currentTargetWaypoint, moveSpeed * Time.deltaTime);

                if (Vector3.Distance(transform.position, currentTargetWaypoint) < 0.1f)
                {
                    if (currentPath != null && currentPath.Count > 0)
                    {
                        currentTargetWaypoint = currentPath.Dequeue();
                    }
                    else
                    {
                        isMovingToTarget = false;
                        OnDestinationReached?.Invoke();
                    }
                }
            }
        }
    }
}
