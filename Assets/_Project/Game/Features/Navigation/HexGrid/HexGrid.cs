using System.Collections.Generic;
using UnityEngine;
using Game.Navigation.HexGrid.Core;
using Game.Navigation.HexGrid.Data;

namespace Game.Navigation.HexGrid
{
    public class HexGrid : MonoBehaviour
    {
        public int width = 10;
        public int height = 10;

        [SerializeField, HideInInspector]
        private HexNode[] nodes;

        private void Awake()
        {
            if (nodes == null || nodes.Length == 0)
            {
                GenerateGrid();
            }
        }

        public void GenerateGrid()
        {
            nodes = new HexNode[width * height];

            for (int z = 0, i = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    CreateNode(x, z, i++);
                }
            }
        }

        private void CreateNode(int x, int z, int i)
        {
            Vector3 position;
            // Pointy topped layout
            position.x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);
            position.y = 0f;
            position.z = z * (HexMetrics.outerRadius * 1.5f);

            HexCoordinates coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
            
            // Adjust position relative to grid's transform
            position += transform.position;

            nodes[i] = new HexNode(coordinates, position);
        }

        public HexNode GetNodeAt(Vector3 position)
        {
            // Convert world pos relative to this grid back to coordinates
            Vector3 localPos = position - transform.position;
            HexCoordinates coordinates = HexCoordinates.FromPosition(localPos);
            return GetNodeFromCoordinates(coordinates);
        }

        public HexNode GetNodeFromCoordinates(HexCoordinates coordinates)
        {
            // Convert cube coords to offset coords to find index
            int z = coordinates.Z;
            int x = coordinates.X + z / 2;

            if (z < 0 || z >= height || x < 0 || x >= width)
            {
                return null;
            }

            int index = x + z * width;
            if (index >= 0 && index < nodes.Length)
            {
                return nodes[index];
            }
            return null;
        }

        public List<HexNode> GetNeighbors(HexNode node)
        {
            List<HexNode> neighbors = new List<HexNode>();
            
            // 6 directions for pointy-topped
            Vector2Int[] directions = {
                new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(0, -1),
                new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1)
            };

            foreach (var dir in directions)
            {
                HexCoordinates neighborCoords = new HexCoordinates(node.coordinates.X + dir.x, node.coordinates.Z + dir.y);
                HexNode neighborNode = GetNodeFromCoordinates(neighborCoords);
                if (neighborNode != null)
                {
                    neighbors.Add(neighborNode);
                }
            }

            return neighbors;
        }

        // Editor specific: Get all nodes for drawing
        public HexNode[] GetAllNodes()
        {
            return nodes;
        }
    }
}
