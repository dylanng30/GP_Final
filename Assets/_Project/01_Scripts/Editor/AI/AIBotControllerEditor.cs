using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Game.Gameplay.AI.Controllers;
using Game.Gameplay.AI.Gambit;
using Game.Gameplay.AI.GOAP;
using Game.Gameplay.AI.GOAP.Actions;
using Game.Gameplay.AI.GOAP.Goals;

namespace Dylanng.Editor.AI
{
    [CustomEditor(typeof(EnemyBotController), true)]
    public class AIBotControllerEditor : UnityEditor.Editor
    {
        private SerializedProperty _gambitRulesProp;
        private SerializedProperty _goapGoalsProp;
        private SerializedProperty _availableActionsProp;
        private SerializedProperty _detectionRadiusProp;
        private SerializedProperty _spellRangeProp;
        private SerializedProperty _maxDistanceFromSpawnProp;
        private SerializedProperty _targetLayerMaskProp;

        // Boss props
        private SerializedProperty _phase2GambitRulesProp;
        private SerializedProperty _phase2GoapGoalsProp;
        private SerializedProperty _phase2AvailableActionsProp;
        private SerializedProperty _phase2HealthThresholdProp;

        private void OnEnable()
        {
            _gambitRulesProp = serializedObject.FindProperty("gambitRules");
            _goapGoalsProp = serializedObject.FindProperty("goapGoals");
            _availableActionsProp = serializedObject.FindProperty("availableActions");
            _detectionRadiusProp = serializedObject.FindProperty("detectionRadius");
            _spellRangeProp = serializedObject.FindProperty("spellRange");
            _maxDistanceFromSpawnProp = serializedObject.FindProperty("maxDistanceFromSpawn");
            _targetLayerMaskProp = serializedObject.FindProperty("targetLayerMask");

            // Boss specific fields
            _phase2GambitRulesProp = serializedObject.FindProperty("phase2GambitRules");
            _phase2GoapGoalsProp = serializedObject.FindProperty("phase2GoapGoals");
            _phase2AvailableActionsProp = serializedObject.FindProperty("phase2AvailableActions");
            _phase2HealthThresholdProp = serializedObject.FindProperty("phase2HealthThreshold");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EnemyBotController controller = (EnemyBotController)target;
            bool isBoss = controller is BossBotController;

            // Title Banner
            GUIStyle bannerStyle = new GUIStyle(EditorStyles.helpBox)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 13
            };
            EditorGUILayout.BeginVertical(bannerStyle);
            EditorGUILayout.LabelField(isBoss ? "👑 BOSS AI CONTROLLER (GAMBIT + GOAP)" : "🤖 ENEMY AI CONTROLLER (GAMBIT + GOAP)", bannerStyle);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // 1. DRAG & DROP ZONE
            DrawDragAndDropZone();

            EditorGUILayout.Space(10);

            // 2. TARGETING & LEASH CONFIG
            EditorGUILayout.LabelField("🎯 Targeting & Leash Configuration", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            if (_detectionRadiusProp != null) EditorGUILayout.PropertyField(_detectionRadiusProp);
            if (_spellRangeProp != null) EditorGUILayout.PropertyField(_spellRangeProp);
            if (_maxDistanceFromSpawnProp != null) EditorGUILayout.PropertyField(_maxDistanceFromSpawnProp);
            if (_targetLayerMaskProp != null) EditorGUILayout.PropertyField(_targetLayerMaskProp);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(10);

            // 3. GAMBIT RULES
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("⚡ Gambit Priority Rules (Immediate Reflexes)", EditorStyles.boldLabel);
            if (GUILayout.Button("+ Create Rule", GUILayout.Width(110)))
            {
                CreateAssetAndAssign<GambitRuleSO>("NewGambitRule.asset", _gambitRulesProp);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(_gambitRulesProp, true);

            EditorGUILayout.Space(5);

            // 4. GOAP GOALS
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🏆 GOAP Goals (Strategic Objectives)", EditorStyles.boldLabel);
            if (GUILayout.Button("+ Create Goal ▼", GUILayout.Width(120)))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Kill Hero Goal"), false, () => CreateAssetAndAssign<GOAPGoalSO>("Goal_KillHero.asset", _goapGoalsProp));
                menu.AddItem(new GUIContent("Return To Spawn Goal"), false, () => CreateAssetAndAssign<ReturnToSpawnGoalSO>("Goal_ReturnToSpawn.asset", _goapGoalsProp));
                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(_goapGoalsProp, true);

            EditorGUILayout.Space(5);

            // 5. AVAILABLE ACTIONS
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("⚔️ GOAP Actions (Available Behaviors)", EditorStyles.boldLabel);
            if (GUILayout.Button("+ Create Action ▼", GUILayout.Width(130)))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Move To Target Action"), false, () => CreateAssetAndAssign<MoveToMeleeAction>("Action_MoveToTarget.asset", _availableActionsProp));
                menu.AddItem(new GUIContent("Melee Attack Action"), false, () => CreateAssetAndAssign<MeleeAttackAction>("Action_MeleeAttack.asset", _availableActionsProp));
                menu.AddItem(new GUIContent("Ranged Attack Action"), false, () => CreateAssetAndAssign<RangedAttackAction>("Action_RangedAttack.asset", _availableActionsProp));
                menu.AddItem(new GUIContent("Return To Spawn Action"), false, () => CreateAssetAndAssign<ReturnToSpawnAction>("Action_ReturnToSpawn.asset", _availableActionsProp));
                menu.AddItem(new GUIContent("Cast Boss Skill Action"), false, () => CreateAssetAndAssign<CastBossSkillAction>("Action_CastBossSkill.asset", _availableActionsProp));
                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.PropertyField(_availableActionsProp, true);

            // 6. BOSS MULTI-PHASE (if applicable)
            if (isBoss && _phase2HealthThresholdProp != null)
            {
                EditorGUILayout.Space(15);
                GUIStyle phaseStyle = new GUIStyle(EditorStyles.helpBox);
                EditorGUILayout.BeginVertical(phaseStyle);
                EditorGUILayout.LabelField("🔥 Boss Multi-Phase Engine (Phase 2 Settings)", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_phase2HealthThresholdProp, new GUIContent("Phase 2 Health Threshold"));
                EditorGUILayout.PropertyField(_phase2GambitRulesProp, new GUIContent("Phase 2 Gambit Rules"), true);
                EditorGUILayout.PropertyField(_phase2GoapGoalsProp, new GUIContent("Phase 2 GOAP Goals"), true);
                EditorGUILayout.PropertyField(_phase2AvailableActionsProp, new GUIContent("Phase 2 Actions"), true);
                EditorGUILayout.EndVertical();
            }

            // 7. REAL-TIME PLAY MODE DEBUGGER
            if (Application.isPlaying)
            {
                DrawPlayModeDebugger(controller);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDragAndDropZone()
        {
            Rect dropRect = GUILayoutUtility.GetRect(0f, 60f, GUILayout.ExpandWidth(true));
            GUIStyle dropStyle = new GUIStyle(EditorStyles.helpBox)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 11
            };

            GUI.Box(dropRect, "📥 DRAG & DROP SCRIPTABLE OBJECT HERE\n(Auto-detects & categorizes Gambit Rule / GOAP Goal / GOAP Action)", dropStyle);

            Event evt = Event.current;
            if (dropRect.Contains(evt.mousePosition))
            {
                if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (UnityEngine.Object obj in DragAndDrop.objectReferences)
                        {
                            if (obj is GambitRuleSO rule)
                            {
                                AddToObjectList(_gambitRulesProp, rule);
                            }
                            else if (obj is GOAPGoalSO goal)
                            {
                                AddToObjectList(_goapGoalsProp, goal);
                            }
                            else if (obj is GOAPActionBase action)
                            {
                                AddToObjectList(_availableActionsProp, action);
                            }
                        }
                        serializedObject.ApplyModifiedProperties();
                        evt.Use();
                    }
                }
            }
        }

        private void AddToObjectList(SerializedProperty listProp, UnityEngine.Object asset)
        {
            if (listProp == null || asset == null) return;
            for (int i = 0; i < listProp.arraySize; i++)
            {
                if (listProp.GetArrayElementAtIndex(i).objectReferenceValue == asset)
                {
                    return; // Already in list -> do not add duplicate
                }
            }
            listProp.arraySize++;
            listProp.GetArrayElementAtIndex(listProp.arraySize - 1).objectReferenceValue = asset;
        }

        private void CreateAssetAndAssign<T>(string defaultFileName, SerializedProperty assignToListProp) where T : ScriptableObject
        {
            string path = EditorUtility.SaveFilePanelInProject("Quick Create AI Asset", defaultFileName, "asset", "Select folder to save AI ScriptableObject asset");
            if (!string.IsNullOrEmpty(path))
            {
                T asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                if (assignToListProp != null)
                {
                    serializedObject.Update();
                    AddToObjectList(assignToListProp, asset);
                    serializedObject.ApplyModifiedProperties();
                }

                EditorUtility.FocusProjectWindow();
                Selection.activeObject = asset;
            }
        }

        private void DrawPlayModeDebugger(EnemyBotController controller)
        {
            EditorGUILayout.Space(15);
            GUIStyle debugStyle = new GUIStyle(EditorStyles.helpBox);
            EditorGUILayout.BeginVertical(debugStyle);
            EditorGUILayout.LabelField("🛠 LIVE DEBUGGER (PLAY MODE)", EditorStyles.boldLabel);

            var aiBrainField = typeof(EnemyBotController).GetField("_aiBrain", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            AIBrain brain = aiBrainField?.GetValue(controller) as AIBrain;

            EditorGUILayout.LabelField("❤️ HP Percentage:", $"{(controller.HealthPercentage * 100):F1}%");
            EditorGUILayout.LabelField("🎯 Current Target:", controller.CurrentTarget != null ? controller.CurrentTarget.Transform.name : "None");
            EditorGUILayout.LabelField("📏 Distance:", controller.DistanceToTarget < float.MaxValue ? $"{controller.DistanceToTarget:F2}m" : "N/A");

            EditorGUILayout.Space(3);
            if (brain != null)
            {
                EditorGUILayout.LabelField("⚡ Active Gambit Rule:", brain.ActiveGambitRule != null ? brain.ActiveGambitRule.RuleName : "None");
                EditorGUILayout.LabelField("🏆 Active GOAP Goal:", brain.ActiveGoal != null ? brain.ActiveGoal.GoalName : "None");
                EditorGUILayout.LabelField("⚔️ Current Action Chain:", brain.CurrentGOAPAction != null ? brain.CurrentGOAPAction.ActionName : "None");
            }

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("🌐 World State Bitmask:", controller.WorldState.ToString());

            EditorGUILayout.EndVertical();
            
            // Repaint Inspector continuously in Play Mode for real-time observation
            Repaint();
        }
    }
}
