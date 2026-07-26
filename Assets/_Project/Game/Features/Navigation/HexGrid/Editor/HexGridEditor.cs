#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using GP_Final.Navigation.HexGrid.Data;
using GP_Final.Navigation.HexGrid.Core;

namespace GP_Final.Navigation.HexGrid.Editor
{
    [CustomEditor(typeof(HexGrid))]
    public class HexGridEditor : UnityEditor.Editor
    {
        private enum BrushMode { View, ToggleWalkable, SetCost }
        private BrushMode currentMode = BrushMode.View;
        private int paintCost = 1;

        public override void OnInspectorGUI()
        {
            HexGrid hexGrid = (HexGrid)target;

            DrawDefaultInspector();

            if (GUILayout.Button("Generate/Reset Grid"))
            {
                hexGrid.GenerateGrid();
                EditorUtility.SetDirty(hexGrid);
                SceneView.RepaintAll();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Painter Tools", EditorStyles.boldLabel);

            currentMode = (BrushMode)GUILayout.Toolbar((int)currentMode, new string[] { "View Only", "Toggle Walkable", "Set Cost" });

            if (currentMode == BrushMode.SetCost)
            {
                paintCost = EditorGUILayout.IntSlider("Movement Cost", paintCost, 1, 10);
            }
            
            EditorGUILayout.HelpBox("Select a brush and click/drag in the Scene View to paint the grid.", MessageType.Info);
        }

        private void OnSceneGUI()
        {
            HexGrid hexGrid = (HexGrid)target;
            HexNode[] nodes = hexGrid.GetAllNodes();

            if (nodes == null || nodes.Length == 0) return;

            // Handle Mouse Input for Painting
            Event e = Event.current;
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            
            if (currentMode != BrushMode.View)
            {
                if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
                {
                    if (e.button == 0) // Left click
                    {
                        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                        Plane groundPlane = new Plane(Vector3.up, hexGrid.transform.position);
                        if (groundPlane.Raycast(ray, out float enter))
                        {
                            Vector3 hitPoint = ray.GetPoint(enter);
                            HexNode node = hexGrid.GetNodeAt(hitPoint);
                            
                            if (node != null)
                            {
                                if (currentMode == BrushMode.ToggleWalkable && e.type == EventType.MouseDown)
                                {
                                    node.isWalkable = !node.isWalkable;
                                    EditorUtility.SetDirty(hexGrid);
                                }
                                else if (currentMode == BrushMode.SetCost)
                                {
                                    node.movementCost = paintCost;
                                    EditorUtility.SetDirty(hexGrid);
                                }
                                e.Use(); // Consume the event so we don't deselect the object
                            }
                        }
                    }
                }
                
                // Add default control so clicking doesn't deselect the grid while painting
                if (e.type == EventType.Layout)
                {
                    HandleUtility.AddDefaultControl(controlID);
                }
            }

            // Draw the Grid
            Vector3[] localCorners = HexMetrics.corners;
            Vector3[] worldCorners = new Vector3[7];

            Handles.color = Color.white;

            foreach (var node in nodes)
            {
                if (node == null) continue;

                for (int i = 0; i < 7; i++)
                {
                    worldCorners[i] = node.worldPosition + localCorners[i];
                }

                // Color based on state
                if (!node.isWalkable)
                {
                    Handles.color = new Color(1f, 0f, 0f, 0.5f); // Red for unwalkable
                    Handles.DrawSolidDisc(node.worldPosition, Vector3.up, HexMetrics.innerRadius * 0.8f);
                }
                else if (node.movementCost > 1)
                {
                    Handles.color = new Color(1f, 1f, 0f, 0.5f); // Yellow for high cost
                    Handles.DrawSolidDisc(node.worldPosition, Vector3.up, HexMetrics.innerRadius * 0.8f);
                    
                    // Draw text for cost
                    GUIStyle style = new GUIStyle();
                    style.normal.textColor = Color.black;
                    style.alignment = TextAnchor.MiddleCenter;
                    Handles.Label(node.worldPosition, node.movementCost.ToString(), style);
                }

                // Draw hex outline
                Handles.color = Color.white;
                Handles.DrawPolyLine(worldCorners);
            }
        }
    }
}
#endif
