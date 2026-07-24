using UnityEditor;
using UnityEngine;

namespace ColorfulHierarchy
{
    [InitializeOnLoad]
    public static class ColorfulHierarchyDrawer
    {
        static ColorfulHierarchyDrawer()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        }

        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            if (Event.current.type != EventType.Repaint) return;

            GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (go == null) return;

            ColorfulHierarchyCategory category = go.GetComponent<ColorfulHierarchyCategory>();
            if (category == null) return;

            bool isSelected = Selection.Contains(go);

            Rect itemRect = selectionRect;
            itemRect.xMax += 50; 

            if (category.enableBackground)
            {
                Color bgColor = category.backgroundColor;
                if (isSelected)
                {
                    bgColor = Color.Lerp(bgColor, new Color(0.15f, 0.4f, 0.7f, 1f), 0.6f);
                }

                EditorGUI.DrawRect(itemRect, bgColor);

                if (category.leftBorder)
                {
                    Rect borderRect = new Rect(itemRect.x, itemRect.y, category.leftBorderWidth, itemRect.height);
                    EditorGUI.DrawRect(borderRect, category.leftBorderColor);
                }
            }

            Rect contentRect = itemRect;
            if (category.leftBorder && category.enableBackground)
            {
                contentRect.xMin += category.leftBorderWidth + 4;
            }
            else
            {
                contentRect.xMin += 4;
            }

            float iconSize = 16f;
            if (category.icon != null)
            {
                Rect iconRect = new Rect(contentRect.x, contentRect.y + (contentRect.height - iconSize) / 2f, iconSize, iconSize);
                
                if (category.textAlignment == TextAnchor.MiddleCenter || category.textAlignment == TextAnchor.UpperCenter || category.textAlignment == TextAnchor.LowerCenter)
                {
                    GUIContent tempContent = new GUIContent(go.name);
                    GUIStyle tempStyle = new GUIStyle(EditorStyles.label) { fontStyle = category.fontStyle };
                    Vector2 textSize = tempStyle.CalcSize(tempContent);
                    iconRect.x = itemRect.x + (itemRect.width - textSize.x) / 2f - iconSize - 4;
                }
                else if (category.textAlignment == TextAnchor.MiddleRight || category.textAlignment == TextAnchor.UpperRight || category.textAlignment == TextAnchor.LowerRight)
                {
                    GUIContent tempContent = new GUIContent(go.name);
                    GUIStyle tempStyle = new GUIStyle(EditorStyles.label) { fontStyle = category.fontStyle };
                    Vector2 textSize = tempStyle.CalcSize(tempContent);
                    iconRect.x = itemRect.xMax - textSize.x - iconSize - 8;
                }

                GUI.DrawTexture(iconRect, category.icon, ScaleMode.ScaleToFit, true);
                
                if (category.textAlignment == TextAnchor.MiddleLeft || category.textAlignment == TextAnchor.UpperLeft || category.textAlignment == TextAnchor.LowerLeft)
                {
                    contentRect.xMin += iconSize + 4;
                }
            }

            GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
            labelStyle.normal.textColor = category.textColor;
            labelStyle.fontStyle = category.fontStyle;
            labelStyle.alignment = category.textAlignment;
            
            if (isSelected && category.enableBackground)
            {
                labelStyle.normal.textColor = Color.white;
            }

            if (category.textDropShadow && !isSelected) 
            {
                Rect shadowRect = contentRect;
                shadowRect.x += 1;
                shadowRect.y += 1;
                
                GUIStyle shadowStyle = new GUIStyle(labelStyle);
                shadowStyle.normal.textColor = new Color(0, 0, 0, 0.5f);
                GUI.Label(shadowRect, go.name, shadowStyle);
            }

            GUI.Label(contentRect, go.name, labelStyle);
        }
    }

    public static class ColorfulHierarchyMenu
    {
        [MenuItem("GameObject/Colorful Category", false, 0)]
        public static void CreateCategory(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("--- New Category ---");
            go.AddComponent<ColorfulHierarchyCategory>();
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create Colorful Category");
            Selection.activeObject = go;
        }
    }

    [CustomEditor(typeof(ColorfulHierarchyCategory))]
    [CanEditMultipleObjects]
    public class ColorfulHierarchyCategoryEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(10);
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 14;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            EditorGUILayout.LabelField("✨ Colorful Hierarchy Category ✨", titleStyle);
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Text Design", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("textColor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fontStyle"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("textAlignment"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("textDropShadow"));
            EditorGUI.indentLevel--;
            
            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Background Design", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableBackground"));
            if (serializedObject.FindProperty("enableBackground").boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("backgroundColor"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("leftBorder"));
                if (serializedObject.FindProperty("leftBorder").boolValue)
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("leftBorderColor"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("leftBorderWidth"));
                }
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Icon Design", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("icon"));
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(15);
            
            GUIStyle hintStyle = new GUIStyle(EditorStyles.helpBox);
            hintStyle.fontSize = 12;
            hintStyle.alignment = TextAnchor.MiddleLeft;
            EditorGUILayout.TextArea("💡 This component is Editor-Only.\nIt will be automatically removed when building the game so it has ZERO impact on game performance.", hintStyle);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
