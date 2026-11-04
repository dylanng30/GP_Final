using UnityEngine;
using Game.Navigation.Core;
using Game.Navigation.HexGrid.Data;
using System.Collections.Generic;
using System;

namespace Game.Navigation.HexGrid.Pathfinding
{
    public class HexLocalPathfinder : MonoBehaviour, ILocalPathfinder
    {
        public event Action OnDestinationReached;
        
        [SerializeField] private HexGrid hexGrid;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float stopDistance = 0.1f;

        private bool isMovingToTarget = false;
        private Queue<HexNode> currentPath;
        private HexNode currentTargetNode;

        public bool IsMoving => isMovingToTarget;

        private void Awake()
        {
            // Try finding it on Awake, but don't error out if it's missing (e.g., in a Bootstrapper scene)
            if (hexGrid == null)
            {
                hexGrid = FindObjectOfType<HexGrid>();
            }
        }

        public void SetDestination(Vector3 target)
        {
            // If we still don't have a grid (or moved to a new scene), try finding it again
            if (hexGrid == null)
            {
                hexGrid = FindObjectOfType<HexGrid>();
                if (hexGrid == null)
                {
                    Debug.LogWarning("HexLocalPathfinder: Cannot SetDestination because no HexGrid is present in the current scene.");
                    return;
                }
            }

            HexNode startNode = hexGrid.GetNodeAt(transform.position);
            HexNode targetNode = hexGrid.GetNodeAt(target);

            if (startNode != null && targetNode != null)
            {
                List<HexNode> path = HexPathfinder.FindPath(hexGrid, startNode, targetNode);
                if (path != null && path.Count > 0)
                {
                    currentPath = new Queue<HexNode>(path);
                    currentTargetNode = currentPath.Dequeue();
                    isMovingToTarget = true;
                }
                else
                {
                    Debug.LogWarning("HexLocalPathfinder: No path found or already at destination.");
                }
            }
        }

        public void Stop()
        {
            isMovingToTarget = false;
            currentPath?.Clear();
        }

        private void Update()
        {
            if (isMovingToTarget && currentTargetNode != null)
            {
                Vector3 targetPos = currentTargetNode.worldPosition;
                // Move towards center of hex
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

                if (Vector3.Distance(transform.position, targetPos) <= stopDistance)
                {
                    if (currentPath.Count > 0)
                    {
                        currentTargetNode = currentPath.Dequeue();
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
