using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Game.Gameplay.AI.Gambit;

namespace Dylanng.Editor.AI
{
    [CustomPropertyDrawer(typeof(IGambitCondition), true)]
    public class GambitConditionDrawer : PropertyDrawer
    {
        private static List<Type> _conditionTypes;
        private static string[] _conditionNames;

        private static void InitTypes()
        {
            if (_conditionTypes != null) return;
            _conditionTypes = TypeCache.GetTypesDerivedFrom<IGambitCondition>()
                .Where(t => !t.IsInterface && !t.IsAbstract)
                .ToList();
            _conditionNames = new string[] { "None" }.Concat(_conditionTypes.Select(t => t.Name)).ToArray();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            InitTypes();
            EditorGUI.BeginProperty(position, label, property);

            Rect headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            
            int currentIndex = 0;
            string managedRefType = property.managedReferenceFullTypename;
            if (!string.IsNullOrEmpty(managedRefType))
            {
                string[] parts = managedRefType.Split(' ');
                if (parts.Length > 1)
                {
                    Type curType = _conditionTypes.FirstOrDefault(t => t.FullName == parts[1]);
                    if (curType != null) currentIndex = _conditionTypes.IndexOf(curType) + 1;
                }
            }

            int selectedIndex = EditorGUI.Popup(headerRect, label.text, currentIndex, _conditionNames);
            if (selectedIndex != currentIndex)
            {
                if (selectedIndex == 0)
                {
                    property.managedReferenceValue = null;
                }
                else
                {
                    Type targetType = _conditionTypes[selectedIndex - 1];
                    property.managedReferenceValue = Activator.CreateInstance(targetType, true);
                }
                property.serializedObject.ApplyModifiedProperties();
            }

            if (property.hasVisibleChildren && property.managedReferenceValue != null)
            {
                EditorGUI.indentLevel++;
                Rect contentRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, position.width, position.height - EditorGUIUtility.singleLineHeight - 2);
                EditorGUI.PropertyField(contentRect, property, GUIContent.none, true);
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.hasVisibleChildren && property.managedReferenceValue != null)
            {
                return EditorGUI.GetPropertyHeight(property, true) + EditorGUIUtility.singleLineHeight + 2;
            }
            return EditorGUIUtility.singleLineHeight;
        }
    }

    [CustomPropertyDrawer(typeof(IGambitAction), true)]
    public class GambitActionDrawer : PropertyDrawer
    {
        private static List<Type> _actionTypes;
        private static string[] _actionNames;

        private static void InitTypes()
        {
            if (_actionTypes != null) return;
            _actionTypes = TypeCache.GetTypesDerivedFrom<IGambitAction>()
                .Where(t => !t.IsInterface && !t.IsAbstract)
                .ToList();
            _actionNames = new string[] { "None" }.Concat(_actionTypes.Select(t => t.Name)).ToArray();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            InitTypes();
            EditorGUI.BeginProperty(position, label, property);

            Rect headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            
            int currentIndex = 0;
            string managedRefType = property.managedReferenceFullTypename;
            if (!string.IsNullOrEmpty(managedRefType))
            {
                string[] parts = managedRefType.Split(' ');
                if (parts.Length > 1)
                {
                    Type curType = _actionTypes.FirstOrDefault(t => t.FullName == parts[1]);
                    if (curType != null) currentIndex = _actionTypes.IndexOf(curType) + 1;
                }
            }

            int selectedIndex = EditorGUI.Popup(headerRect, label.text, currentIndex, _actionNames);
            if (selectedIndex != currentIndex)
            {
                if (selectedIndex == 0)
                {
                    property.managedReferenceValue = null;
                }
                else
                {
                    Type targetType = _actionTypes[selectedIndex - 1];
                    property.managedReferenceValue = Activator.CreateInstance(targetType, true);
                }
                property.serializedObject.ApplyModifiedProperties();
            }

            if (property.hasVisibleChildren && property.managedReferenceValue != null)
            {
                EditorGUI.indentLevel++;
                Rect contentRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, position.width, position.height - EditorGUIUtility.singleLineHeight - 2);
                EditorGUI.PropertyField(contentRect, property, GUIContent.none, true);
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.hasVisibleChildren && property.managedReferenceValue != null)
            {
                return EditorGUI.GetPropertyHeight(property, true) + EditorGUIUtility.singleLineHeight + 2;
            }
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
