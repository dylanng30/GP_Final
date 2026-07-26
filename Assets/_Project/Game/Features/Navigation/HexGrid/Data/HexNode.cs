using UnityEngine;
using GP_Final.Navigation.HexGrid.Core;
using System;

namespace GP_Final.Navigation.HexGrid.Data
{
    [Serializable]
    public class HexNode
    {
        public HexCoordinates coordinates;
        public Vector3 worldPosition;
        
        public bool isWalkable = true;
        public int movementCost = 1;

        // Pathfinding specific properties (not serialized)
        [NonSerialized] public int gCost;
        [NonSerialized] public int hCost;
        [NonSerialized] public HexNode parentNode;
        
        public int FCost => gCost + hCost;

        public HexNode(HexCoordinates coordinates, Vector3 worldPosition)
        {
            this.coordinates = coordinates;
            this.worldPosition = worldPosition;
        }

        public void ResetPathfinding()
        {
            gCost = 0;
            hCost = 0;
            parentNode = null;
        }
    }
}
