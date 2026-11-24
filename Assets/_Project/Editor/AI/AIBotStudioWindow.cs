using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Game.Gameplay.AI.Controllers;
using Game.Gameplay.AI.Gambit;
using Game.Gameplay.AI.GOAP;
using Game.Gameplay.AI.GOAP.Actions;

namespace Dylanng.Editor.AI
{
    public class AIBotStudioWindow : EditorWindow
    {
        private Vector2 _leftScroll;
        private Vector2 _rightScroll;
        
        private List<GambitRuleSO> _allRules = new List<GambitRuleSO>();
        private List<GOAPGoalSO> _allGoals = new List<GOAPGoalSO>();
        private List<GOAPActionBase> _allActions = new List<GOAPActionBase>();
        
        private EnemyBotController _selectedController;

        [MenuItem("Dylanng/AI/Gambit & GOAP Studio")]
        public static void ShowWindow()
        {
            AIBotStudioWindow window = GetWindow<AIBotStudioWindow>("AI Studio");
            window.minSize = new Vector2(750, 500);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshAssets();
        }

        private void OnFocus()
        {
            RefreshAssets();
        }

        private void RefreshAssets()
        {
            _allRules.Clear();
            _allGoals.Clear();
            _allActions.Clear();

            string[] ruleGuids = AssetDatabase.FindAssets("t:GambitRuleSO");
            foreach (string g in ruleGuids)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GambitRuleSO>(AssetDatabase.GUIDToAssetPath(g));
                if (asset != null) _allRules.Add(asset);
            }

            string[] goalGuids = AssetDatabase.FindAssets("t:GOAPGoalSO");
            foreach (string g in goalGuids)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GOAPGoalSO>(AssetDatabase.GUIDToAssetPath(g));
                if (asset != null) _allGoals.Add(asset);
            }

            string[] actionGuids = AssetDatabase.FindAssets("t:GOAPActionBase");
            foreach (string g in actionGuids)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GOAPActionBase>(AssetDatabase.GUIDToAssetPath(g));
                if (asset != null) _allActions.Add(asset);
            }
        }

        private void OnGUI()
        {
            DrawToolbar();

            EditorGUILayout.BeginHorizontal();

            // LEFT PANEL: PROJECT ASSETS
            EditorGUILayout.BeginVertical("box", GUILayout.Width(350));
            DrawLeftPanel();
            EditorGUILayout.EndVertical();

            // RIGHT PANEL: SCENE CONTROLLERS OR SELECTED DETAILS
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
            DrawRightPanel();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            if (Application.isPlaying)
            {
                Repaint();
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("🔄 Refresh Assets", EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                RefreshAssets();
            }
            if (GUILayout.Button("+ Create Gambit Rule", EditorStyles.toolbarButton, GUILayout.Width(130)))
            {
                CreateAsset<GambitRuleSO>("NewGambitRule.asset");
            }
            if (GUILayout.Button("+ Create GOAP Goal", EditorStyles.toolbarButton, GUILayout.Width(130)))
            {
                CreateAsset<GOAPGoalSO>("NewGOAPGoal.asset");
            }
            if (GUILayout.Button("+ Create Action ▼", EditorStyles.toolbarDropDown, GUILayout.Width(130)))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Move To Target Action"), false, () => CreateAsset<MoveToMeleeAction>("Action_MoveToTarget.asset"));
                menu.AddItem(new GUIContent("Melee Attack Action"), false, () => CreateAsset<MeleeAttackAction>("Action_MeleeAttack.asset"));
                menu.AddItem(new GUIContent("Move To Spell Range Action"), false, () => CreateAsset<MoveToSpellRangeAction>("Action_MoveToSpellRange.asset"));
                menu.AddItem(new GUIContent("Ranged Attack Action"), false, () => CreateAsset<RangedAttackAction>("Action_RangedAttack.asset"));
                menu.AddItem(new GUIContent("Cast Boss Skill Action"), false, () => CreateAsset<CastBossSkillAction>("Action_CastBossSkill.asset"));
                menu.ShowAsContext();
            }
            if (GUILayout.Button("⚡ Auto-Create Melee/Ranged Assets", EditorStyles.toolbarButton, GUILayout.Width(210)))
            {
                AIBotAssetCreator.CreateDefaultAssets();
                RefreshAssets();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("🤖 Dylanng AI Studio", EditorStyles.boldLabel, GUILayout.Width(130));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLeftPanel()
        {
            EditorGUILayout.LabelField("📁 PROJECT ASSETS", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);

            // RULES
            EditorGUILayout.LabelField($"⚡ Gambit Rules ({_allRules.Count})", EditorStyles.miniBoldLabel);
            for (int i = 0; i < _allRules.Count; i++)
            {
                DrawAssetRow(_allRules[i]);
            }
            EditorGUILayout.Space(10);

            // GOALS
            EditorGUILayout.LabelField($"🏆 GOAP Goals ({_allGoals.Count})", EditorStyles.miniBoldLabel);
            for (int i = 0; i < _allGoals.Count; i++)
            {
                DrawAssetRow(_allGoals[i]);
            }
            EditorGUILayout.Space(10);

            // ACTIONS
            EditorGUILayout.LabelField($"⚔️ GOAP Actions ({_allActions.Count})", EditorStyles.miniBoldLabel);
            for (int i = 0; i < _allActions.Count; i++)
            {
                DrawAssetRow(_allActions[i]);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawAssetRow(ScriptableObject asset)
        {
            if (asset == null) return;
            EditorGUILayout.BeginHorizontal("helpBox");
            
            // Icon / Name
            EditorGUILayout.ObjectField(asset, asset.GetType(), false);

            // Assign Button if controller selected
            if (_selectedController != null)
            {
                if (GUILayout.Button("➡️ Assign", GUILayout.Width(45), GUILayout.Height(18)))
                {
                    AssignAssetToSelectedController(asset);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawRightPanel()
        {
            EditorGUILayout.LabelField("🎮 SCENE AI CONTROLLERS (DRAG & DROP / QUICK SELECT)", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);

            // Find all controllers in active scene
            EnemyBotController[] controllers = Object.FindObjectsByType<EnemyBotController>();
            if (controllers.Length == 0)
            {
                EditorGUILayout.HelpBox("No EnemyBotController or BossBotController found in the current Scene.", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < controllers.Length; i++)
                {
                    EnemyBotController controller = controllers[i];
                    bool isSelected = (_selectedController == controller);
                    
                    GUIStyle boxStyle = new GUIStyle("helpBox");
                    if (isSelected)
                    {
                        boxStyle.normal.background = EditorGUIUtility.Load("builtin_skins/darkskin/images/node3 on.png") as Texture2D;
                    }

                    EditorGUILayout.BeginVertical(boxStyle);
                    EditorGUILayout.BeginHorizontal();
                    
                    string icon = controller is BossBotController ? "👑" : "🤖";
            EditorGUILayout.LabelField($"{icon} {controller.gameObject.name}", EditorStyles.boldLabel);

                    if (GUILayout.Button(isSelected ? "Selected ✔" : "Select", GUILayout.Width(90)))
                    {
                        _selectedController = controller;
                        Selection.activeGameObject = controller.gameObject;
                    }

                    EditorGUILayout.EndHorizontal();

                    // Drag and Drop dropzone specifically for this controller
                    Rect dropRect = GUILayoutUtility.GetRect(0f, 25f, GUILayout.ExpandWidth(true));
                    GUIStyle dropStyle = new GUIStyle("box") { alignment = TextAnchor.MiddleCenter, fontSize = 10 };
                    GUI.Box(dropRect, "📥 Drag & drop Gambit Rule / GOAP Goal / GOAP Action here to assign to this Bot", dropStyle);

                    Event evt = Event.current;
                    if (dropRect.Contains(evt.mousePosition) && (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        if (evt.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();
                            Undo.RecordObject(controller, "Assign AI Asset via Studio");
                            foreach (Object obj in DragAndDrop.objectReferences)
                            {
                                if (obj is ScriptableObject so) AssignAssetToController(controller, so);
                            }
                            EditorUtility.SetDirty(controller);
                            evt.Use();
                        }
                    }

                    // Live Play Mode mini-status
                    if (Application.isPlaying)
                    {
                        EditorGUILayout.LabelField($"HP: {(controller.HealthPercentage * 100):F1}% | Target: {(controller.CurrentTarget != null ? controller.CurrentTarget.Transform.name : "None")}", EditorStyles.miniLabel);
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(5);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void AssignAssetToSelectedController(ScriptableObject asset)
        {
            if (_selectedController == null || asset == null) return;
            Undo.RecordObject(_selectedController, "Assign AI Asset via Studio Button");
            AssignAssetToController(_selectedController, asset);
            EditorUtility.SetDirty(_selectedController);
        }

        private void AssignAssetToController(EnemyBotController controller, ScriptableObject asset)
        {
            if (controller == null || asset == null) return;

            SerializedObject so = new SerializedObject(controller);
            so.Update();

            SerializedProperty prop = null;
            if (asset is GambitRuleSO) prop = so.FindProperty("gambitRules");
            else if (asset is GOAPGoalSO) prop = so.FindProperty("goapGoals");
            else if (asset is GOAPActionBase) prop = so.FindProperty("availableActions");

            if (prop != null)
            {
                // Check duplicate
                for (int i = 0; i < prop.arraySize; i++)
                {
                    if (prop.GetArrayElementAtIndex(i).objectReferenceValue == asset) return;
                }
                prop.arraySize++;
                prop.GetArrayElementAtIndex(prop.arraySize - 1).objectReferenceValue = asset;
                so.ApplyModifiedProperties();
                Debug.Log($"[AI Studio] Assigned '{asset.name}' to '{controller.gameObject.name}'.");
            }
        }

        private void CreateAsset<T>(string defaultName) where T : ScriptableObject
        {
            string path = EditorUtility.SaveFilePanelInProject("Create AI Asset", defaultName, "asset", "Select folder to save");
            if (!string.IsNullOrEmpty(path))
            {
                T asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                RefreshAssets();
                Selection.activeObject = asset;
            }
        }
    }
}
