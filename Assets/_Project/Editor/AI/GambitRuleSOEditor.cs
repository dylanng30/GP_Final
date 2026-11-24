using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Game.Gameplay.AI.Gambit;

namespace Dylanng.Editor.AI
{
    [CustomEditor(typeof(GambitRuleSO))]
    public class GambitRuleSOEditor : UnityEditor.Editor
    {
        private SerializedProperty _ruleNameProp;
        private SerializedProperty _priorityProp;
        private SerializedProperty _conditionsProp;
        private SerializedProperty _actionProp;

        private List<Type> _availableConditionTypes;
        private List<Type> _availableActionTypes;

        private void OnEnable()
        {
            _ruleNameProp = serializedObject.FindProperty("ruleName");
            _priorityProp = serializedObject.FindProperty("priority");
            _conditionsProp = serializedObject.FindProperty("conditions");
            _actionProp = serializedObject.FindProperty("action");

            _availableConditionTypes = TypeCache.GetTypesDerivedFrom<IGambitCondition>()
                .Where(t => !t.IsInterface && !t.IsAbstract)
                .ToList();

            _availableActionTypes = TypeCache.GetTypesDerivedFrom<IGambitAction>()
                .Where(t => !t.IsInterface && !t.IsAbstract)
                .ToList();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Header box
            GUIStyle headerStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };
            EditorGUILayout.BeginVertical(headerStyle);
            EditorGUILayout.LabelField("Gambit Priority Override Rule", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_ruleNameProp);
            EditorGUILayout.PropertyField(_priorityProp);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Conditions section
            EditorGUILayout.LabelField("Conditions (AND logic - All must be true)", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            for (int i = 0; i < _conditionsProp.arraySize; i++)
            {
                SerializedProperty elem = _conditionsProp.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(elem, new GUIContent($"Condition {i + 1}"), true);
                if (GUILayout.Button("X", GUILayout.Width(24)))
                {
                    _conditionsProp.DeleteArrayElementAtIndex(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2);
            }
            EditorGUI.indentLevel--;

            // Add Condition Button
            if (GUILayout.Button("+ Add Condition", GUILayout.Height(25)))
            {
                GenericMenu menu = new GenericMenu();
                foreach (Type type in _availableConditionTypes)
                {
                    Type targetType = type;
                    menu.AddItem(new GUIContent(type.Name), false, () =>
                    {
                        serializedObject.Update();
                        _conditionsProp.arraySize++;
                        SerializedProperty newElem = _conditionsProp.GetArrayElementAtIndex(_conditionsProp.arraySize - 1);
                        newElem.managedReferenceValue = Activator.CreateInstance(targetType, true);
                        serializedObject.ApplyModifiedProperties();
                    });
                }
                menu.ShowAsContext();
            }

            EditorGUILayout.Space(10);

            // Action section
            EditorGUILayout.LabelField("Tactical Override Action (Immediate Execution)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_actionProp, new GUIContent("Action to Execute"), true);

            if (_actionProp.managedReferenceValue == null)
            {
                if (GUILayout.Button("Select Tactical Action...", GUILayout.Height(25)))
                {
                    GenericMenu menu = new GenericMenu();
                    foreach (Type type in _availableActionTypes)
                    {
                        Type targetType = type;
                        menu.AddItem(new GUIContent(type.Name), false, () =>
                        {
                            serializedObject.Update();
                            _actionProp.managedReferenceValue = Activator.CreateInstance(targetType, true);
                            serializedObject.ApplyModifiedProperties();
                        });
                    }
                    menu.ShowAsContext();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
