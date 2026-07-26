using UnityEngine;

namespace GP_Final.Navigation.HexGrid.Core
{
    public static class HexMetrics
    {
        public const float outerRadius = 1f;
        public const float innerRadius = outerRadius * 0.866025404f; // sqrt(3) / 2

        // Pointy-topped hexagon corners
        public static readonly Vector3[] corners = {
            new Vector3(0f, 0f, outerRadius),
            new Vector3(innerRadius, 0f, 0.5f * outerRadius),
            new Vector3(innerRadius, 0f, -0.5f * outerRadius),
            new Vector3(0f, 0f, -outerRadius),
            new Vector3(-innerRadius, 0f, -0.5f * outerRadius),
            new Vector3(-innerRadius, 0f, 0.5f * outerRadius),
            new Vector3(0f, 0f, outerRadius) // Duplicate the first corner to easily draw lines
        };
    }
}
