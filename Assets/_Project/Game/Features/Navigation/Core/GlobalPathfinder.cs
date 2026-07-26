using System.Collections.Generic;
using UnityEngine;
using GP_Final.Navigation.Data;

namespace GP_Final.Navigation.Core
{
    public class GlobalPathfinder
    {
        private class Node
        {
            public string PortalId;
            public float GCost;
            public float HCost;
            public float FCost => GCost + HCost;
            public Node Parent;
        }

        /// <summary>
        /// Finds a path of portals from the start scene to the target scene.
        /// </summary>
        public static List<string> FindGlobalPath(WorldMapGraphSO graph, string startScene, Vector3 startPos, string targetScene, Vector3 targetPos)
        {
            List<string> path = new List<string>();

            // If same scene, no portals needed globally
            if (startScene == targetScene)
            {
                return path; 
            }

            // Find all portals in the start scene to act as entry points
            List<PortalConnection> startPortals = GetPortalsByScene(graph, startScene);

            if (startPortals.Count == 0)
            {
                Debug.LogWarning($"GlobalPathfinder: Cannot find path. No portals in start scene.");
                return path;
            }

            // A* Implementation on the Portal Graph
            List<Node> openList = new List<Node>();
            HashSet<string> closedSet = new HashSet<string>();

            // Initialize open list with all portals in the start scene.
            // (Assuming we are starting at startPos, we should theoretically add the cost from startPos to the portal, 
            // but for simplicity in graph traversal, we just treat all portals in start scene as starting nodes with 0 initial cost).
            foreach (var portal in startPortals)
            {
                openList.Add(new Node 
                { 
                    PortalId = portal.fromPortalId, 
                    GCost = 0, 
                    HCost = 1 // Heuristic could be improved based on world positions if known
                });
            }

            Node targetNode = null;

            while (openList.Count > 0)
            {
                // Get node with lowest FCost
                Node currentNode = openList[0];
                for (int i = 1; i < openList.Count; i++)
                {
                    if (openList[i].FCost < currentNode.FCost || 
                       (openList[i].FCost == currentNode.FCost && openList[i].HCost < currentNode.HCost))
                    {
                        currentNode = openList[i];
                    }
                }

                openList.Remove(currentNode);
                closedSet.Add(currentNode.PortalId);

                // Check if we reached any portal in the target scene
                if (IsPortalInScene(graph, currentNode.PortalId, targetScene))
                {
                    targetNode = currentNode;
                    break;
                }

                // Get Neighbors
                List<string> neighbors = GetNeighbors(graph, currentNode.PortalId);

                foreach (string neighborId in neighbors)
                {
                    if (closedSet.Contains(neighborId)) continue;

                    float movementCostToNeighbor = currentNode.GCost + GetCostBetweenPortals(graph, currentNode.PortalId, neighborId);
                    
                    Node neighborNode = openList.Find(n => n.PortalId == neighborId);

                    if (neighborNode == null || movementCostToNeighbor < neighborNode.GCost)
                    {
                        if (neighborNode == null)
                        {
                            neighborNode = new Node { PortalId = neighborId };
                            openList.Add(neighborNode);
                        }

                        neighborNode.GCost = movementCostToNeighbor;
                        neighborNode.HCost = 1; // Simplified heuristic
                        neighborNode.Parent = currentNode;
                    }
                }
            }

            // Retrace path
            if (targetNode != null)
            {
                Node current = targetNode;
                while (current != null)
                {
                    path.Add(current.PortalId);
                    current = current.Parent;
                }
                path.Reverse(); // Since we traced backwards from target
            }
            else
            {
                string visitedNodes = string.Join(", ", closedSet);
                Debug.LogWarning($"GlobalPathfinder: Path not found from {startScene} to {targetScene}. Visited portals: [{visitedNodes}]. Please check if intraScenePaths are connected and scene names match exactly.");
            }

            return path;
        }

        private static List<PortalConnection> GetPortalsByScene(WorldMapGraphSO graph, string sceneName)
        {
            List<PortalConnection> portalsInScene = new List<PortalConnection>();
            foreach(var conn in graph.interSceneConnections)
            {
                if (conn.fromSceneName == sceneName)
                {
                    portalsInScene.Add(conn); 
                }
            }
            return portalsInScene;
        }

        private static bool IsPortalInScene(WorldMapGraphSO graph, string portalId, string sceneName)
        {
            foreach (var conn in graph.interSceneConnections)
            {
                if (conn.toPortalId == portalId && conn.toSceneName == sceneName)
                    return true;
                    
                if (conn.fromPortalId == portalId && conn.fromSceneName == sceneName)
                    return true;
            }
            return false;
        }

        private static List<string> GetNeighbors(WorldMapGraphSO graph, string portalId)
        {
            List<string> neighbors = new List<string>();

            // 1. Neighbors via Inter-Scene connections (Portal to Portal in different scenes)
            foreach (var conn in graph.interSceneConnections)
            {
                if (conn.fromPortalId == portalId)
                {
                    neighbors.Add(conn.toPortalId);
                }
            }

            // 2. Neighbors via Intra-Scene paths (Portals in the same scene)
            foreach (var path in graph.intraScenePaths)
            {
                if (path.portalId1 == portalId) neighbors.Add(path.portalId2);
                if (path.portalId2 == portalId) neighbors.Add(path.portalId1);
            }

            return neighbors;
        }

        private static float GetCostBetweenPortals(WorldMapGraphSO graph, string portalA, string portalB)
        {
            // Check inter-scene cost
            foreach (var conn in graph.interSceneConnections)
            {
                if (conn.fromPortalId == portalA && conn.toPortalId == portalB) return conn.transitionCost;
            }

            // Check intra-scene cost
            float intraCost = graph.GetIntraSceneCost(portalA, portalB);
            if (intraCost != float.MaxValue) return intraCost;

            return 9999f; // Fallback high cost
        }
    }
}
