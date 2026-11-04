using System.Collections.Generic;
using Game.Navigation.HexGrid.Data;
using UnityEngine;

namespace Game.Navigation.HexGrid.Pathfinding
{
    public class HexPathfinder
    {
        public static List<HexNode> FindPath(HexGrid grid, HexNode startNode, HexNode targetNode)
        {
            if (startNode == null || targetNode == null || !targetNode.isWalkable)
            {
                return null;
            }

            List<HexNode> openSet = new List<HexNode>();
            HashSet<HexNode> closedSet = new HashSet<HexNode>();

            // Reset pathfinding data for all nodes just in case (ideally should only reset used ones for performance, but this is safer for prototype)
            HexNode[] allNodes = grid.GetAllNodes();
            foreach (var node in allNodes)
            {
                if (node != null) node.ResetPathfinding();
            }

            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                HexNode currentNode = openSet[0];
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (openSet[i].FCost < currentNode.FCost || 
                        (openSet[i].FCost == currentNode.FCost && openSet[i].hCost < currentNode.hCost))
                    {
                        currentNode = openSet[i];
                    }
                }

                openSet.Remove(currentNode);
                closedSet.Add(currentNode);

                if (currentNode == targetNode)
                {
                    return RetracePath(startNode, targetNode);
                }

                foreach (HexNode neighbor in grid.GetNeighbors(currentNode))
                {
                    if (!neighbor.isWalkable || closedSet.Contains(neighbor))
                    {
                        continue;
                    }

                    int newMovementCostToNeighbor = currentNode.gCost + neighbor.movementCost;
                    if (newMovementCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                    {
                        neighbor.gCost = newMovementCostToNeighbor;
                        neighbor.hCost = neighbor.coordinates.DistanceTo(targetNode.coordinates);
                        neighbor.parentNode = currentNode;

                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                        }
                    }
                }
            }

            return null; // Path not found
        }

        private static List<HexNode> RetracePath(HexNode startNode, HexNode endNode)
        {
            List<HexNode> path = new List<HexNode>();
            HexNode currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode);
                currentNode = currentNode.parentNode;
            }
            // Optional: include start node or not. Usually path doesn't need start node.
            
            path.Reverse();
            return path;
        }
    }
}
