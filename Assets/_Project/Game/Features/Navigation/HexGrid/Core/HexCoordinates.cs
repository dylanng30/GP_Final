using UnityEngine;
using System;

namespace GP_Final.Navigation.HexGrid.Core
{
    [Serializable]
    public struct HexCoordinates : IEquatable<HexCoordinates>
    {
        [SerializeField] private int x, z;

        public int X => x;
        public int Z => z;
        public int Y => -X - Z;

        public HexCoordinates(int x, int z)
        {
            this.x = x;
            this.z = z;
        }

        // Create from offset coordinates (commonly used for arrays)
        // Using "odd-q" vertical layout or "odd-r" horizontal layout?
        // Since we are pointy topped, horizontal rows makes sense. Let's use offset coordinates.
        public static HexCoordinates FromOffsetCoordinates(int x, int z)
        {
            // Pointy topped, odd-r (rows) offset
            return new HexCoordinates(x - z / 2, z);
        }

        // Convert world position back to hex coordinates
        public static HexCoordinates FromPosition(Vector3 position)
        {
            float x = position.x / (HexMetrics.innerRadius * 2f);
            float y = -x;
            
            float offset = position.z / (HexMetrics.outerRadius * 3f);
            x -= offset;
            y -= offset;

            int iX = Mathf.RoundToInt(x);
            int iY = Mathf.RoundToInt(y);
            int iZ = Mathf.RoundToInt(-x - y);

            if (iX + iY + iZ != 0)
            {
                float dX = Mathf.Abs(x - iX);
                float dY = Mathf.Abs(y - iY);
                float dZ = Mathf.Abs(-x - y - iZ);

                if (dX > dY && dX > dZ)
                {
                    iX = -iY - iZ;
                }
                else if (dZ > dY)
                {
                    iZ = -iX - iY;
                }
            }

            return new HexCoordinates(iX, iZ);
        }

        public int DistanceTo(HexCoordinates other)
        {
            return ((x < other.x ? other.x - x : x - other.x) +
                    (Y < other.Y ? other.Y - Y : Y - other.Y) +
                    (z < other.z ? other.z - z : z - other.z)) / 2;
        }

        public override string ToString() => $"({X}, {Y}, {Z})";

        public bool Equals(HexCoordinates other)
        {
            return x == other.x && z == other.z;
        }

        public override bool Equals(object obj)
        {
            return obj is HexCoordinates other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(x, z);
        }

        public static bool operator ==(HexCoordinates left, HexCoordinates right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(HexCoordinates left, HexCoordinates right)
        {
            return !left.Equals(right);
        }
    }
}
