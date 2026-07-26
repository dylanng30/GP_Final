using System.Collections.Generic;
using UnityEngine;

namespace GP_Final.Navigation.Data
{
    [CreateAssetMenu(fileName = "WorldMapGraph", menuName = "Dylanng/Pathfinding/WorldMapGraph")]
    public class WorldMapGraphSO : ScriptableObject
    {
        public List<PortalConnection> interSceneConnections = new List<PortalConnection>();
        public List<IntraScenePath> intraScenePaths = new List<IntraScenePath>();

        // Will be used by GlobalPathfinder to calculate costs
        public float GetIntraSceneCost(string portalId1, string portalId2)
        {
            foreach (var path in intraScenePaths)
            {
                if ((path.portalId1 == portalId1 && path.portalId2 == portalId2) ||
                    (path.portalId1 == portalId2 && path.portalId2 == portalId1))
                {
                    return path.distanceCost;
                }
            }
            return float.MaxValue; // Or a very large cost if not found
        }
    }
}
