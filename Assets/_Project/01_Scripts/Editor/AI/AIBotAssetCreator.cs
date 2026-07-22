using System.IO;
using UnityEditor;
using UnityEngine;
using Game.Gameplay.AI.Controllers;
using Game.Gameplay.AI.Core;
using Game.Gameplay.AI.Gambit;
using Game.Gameplay.AI.Gambit.Actions;
using Game.Gameplay.AI.Gambit.Conditions;
using Game.Gameplay.AI.GOAP;
using Game.Gameplay.AI.GOAP.Actions;

namespace Dylanng.Editor.AI
{
    public static class AIBotAssetCreator
    {
        private const string BASE_PATH = "Assets/_Project/01_Scripts/Game/Gameplay/AI/";
        private const string GOALS_PATH = BASE_PATH + "_GOAPGoals/";
        private const string ACTIONS_PATH = BASE_PATH + "_Actions/";
        private const string RULES_PATH = BASE_PATH + "_GambitRules/";

        [MenuItem("Dylanng/AI/Auto-Create Default Bot Assets (Melee & Ranged)")]
        public static void CreateDefaultAssets()
        {
            EnsureDirectories();

            // 1. GOAP Goal chung cho cả Melee & Ranged (Kill Hero)
            CreateOrUpdateGOAPGoal(GOALS_PATH + "GOAPGoal_KillHero.asset", "Kill Hero Goal", 50,
                new[] { AIWorldStateKey.TargetIsDead }, new[] { true });

            // 2. GOAP Actions cho Melee Bot
            CreateOrUpdateMoveToTargetAction(ACTIONS_PATH + "Action_MoveToMeleeRange.asset", "Move To Melee Range", 1f, 1.8f);
            CreateOrUpdateMeleeAttackAction(ACTIONS_PATH + "Action_MeleeAttack.asset", "Melee Attack", 1f, 1.0f);

            // 3. GOAP Actions cho Ranged Bot
            CreateOrUpdateMoveToSpellRangeAction(ACTIONS_PATH + "Action_MoveToSpellRange.asset", "Move To Spell Range", 1f, 6.5f);
            CreateOrUpdateRangedAttackAction(ACTIONS_PATH + "Action_RangedAttack.asset", "Ranged Attack", 1f, 1.2f, -1);

            // 4. Gambit Rules chung & riêng
            CreateOrUpdateGambitRule(RULES_PATH + "Rule_SelfDefense_LowHP.asset", "Tự Vệ Khi Yếu Máu (Shield)", 100,
                new HealthBelowThresholdCondition(0.25f), new SelfDefenseImmunityAction(0));

            CreateOrUpdateGambitRule(RULES_PATH + "Rule_Ranged_FleeIfMelee.asset", "Phản Xạ Bị Áp Sát (Ranged Defense)", 90,
                new TargetInMeleeRangeCondition(2.5f), new SelfDefenseImmunityAction(0));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AIBotAssetCreator] Tạo & chuẩn hóa thành công tất cả bộ SO Assets cho Melee & Ranged Bot!");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(GOALS_PATH)) Directory.CreateDirectory(GOALS_PATH);
            if (!Directory.Exists(ACTIONS_PATH)) Directory.CreateDirectory(ACTIONS_PATH);
            if (!Directory.Exists(RULES_PATH)) Directory.CreateDirectory(RULES_PATH);
        }

        private static void CreateOrUpdateGOAPGoal(string path, string goalName, int priority, AIWorldStateKey[] keys, bool[] values)
        {
            var goal = AssetDatabase.LoadAssetAtPath<GOAPGoalSO>(path);
            if (goal == null)
            {
                goal = ScriptableObject.CreateInstance<GOAPGoalSO>();
                AssetDatabase.CreateAsset(goal, path);
            }

            SerializedObject so = new SerializedObject(goal);
            so.FindProperty("goalName").stringValue = goalName;
            so.FindProperty("basePriority").intValue = priority;
            
            SetEnumArray(so.FindProperty("desiredStateKeys"), keys);
            SetBoolArray(so.FindProperty("desiredStateValues"), values);
            
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(goal);
        }

        private static void CreateOrUpdateMoveToTargetAction(string path, string actionName, float cost, float stopDist)
        {
            var action = AssetDatabase.LoadAssetAtPath<MoveToMeleeAction>(path);
            if (action == null)
            {
                action = ScriptableObject.CreateInstance<MoveToMeleeAction>();
                AssetDatabase.CreateAsset(action, path);
            }

            SerializedObject so = new SerializedObject(action);
            so.FindProperty("actionName").stringValue = actionName;
            so.FindProperty("cost").floatValue = cost;
            so.FindProperty("stopDistance").floatValue = stopDist;
            
            SetEnumArray(so.FindProperty("preconditionKeys"), new[] { AIWorldStateKey.TargetExists });
            SetBoolArray(so.FindProperty("preconditionValues"), new[] { true });
            SetEnumArray(so.FindProperty("effectKeys"), new[] { AIWorldStateKey.TargetInMeleeRange });
            SetBoolArray(so.FindProperty("effectValues"), new[] { true });

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(action);
        }

        private static void CreateOrUpdateMeleeAttackAction(string path, string actionName, float cost, float duration)
        {
            var action = AssetDatabase.LoadAssetAtPath<MeleeAttackAction>(path);
            if (action == null)
            {
                action = ScriptableObject.CreateInstance<MeleeAttackAction>();
                AssetDatabase.CreateAsset(action, path);
            }

            SerializedObject so = new SerializedObject(action);
            so.FindProperty("actionName").stringValue = actionName;
            so.FindProperty("cost").floatValue = cost;
            so.FindProperty("attackDuration").floatValue = duration;
            
            SetEnumArray(so.FindProperty("preconditionKeys"), new[] { AIWorldStateKey.TargetInMeleeRange });
            SetBoolArray(so.FindProperty("preconditionValues"), new[] { true });
            SetEnumArray(so.FindProperty("effectKeys"), new[] { AIWorldStateKey.TargetIsDead });
            SetBoolArray(so.FindProperty("effectValues"), new[] { true });

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(action);
        }

        private static void CreateOrUpdateMoveToSpellRangeAction(string path, string actionName, float cost, float stopDist)
        {
            var action = AssetDatabase.LoadAssetAtPath<MoveToSpellRangeAction>(path);
            if (action == null)
            {
                action = ScriptableObject.CreateInstance<MoveToSpellRangeAction>();
                AssetDatabase.CreateAsset(action, path);
            }

            SerializedObject so = new SerializedObject(action);
            so.FindProperty("actionName").stringValue = actionName;
            so.FindProperty("cost").floatValue = cost;
            so.FindProperty("stopDistance").floatValue = stopDist;
            
            SetEnumArray(so.FindProperty("preconditionKeys"), new[] { AIWorldStateKey.TargetExists });
            SetBoolArray(so.FindProperty("preconditionValues"), new[] { true });
            SetEnumArray(so.FindProperty("effectKeys"), new[] { AIWorldStateKey.TargetInSpellRange });
            SetBoolArray(so.FindProperty("effectValues"), new[] { true });

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(action);
        }

        private static void CreateOrUpdateRangedAttackAction(string path, string actionName, float cost, float duration, int skillIndex)
        {
            var action = AssetDatabase.LoadAssetAtPath<RangedAttackAction>(path);
            if (action == null)
            {
                action = ScriptableObject.CreateInstance<RangedAttackAction>();
                AssetDatabase.CreateAsset(action, path);
            }

            SerializedObject so = new SerializedObject(action);
            so.FindProperty("actionName").stringValue = actionName;
            so.FindProperty("cost").floatValue = cost;
            so.FindProperty("attackDuration").floatValue = duration;
            so.FindProperty("skillIndex").intValue = skillIndex;
            
            SetEnumArray(so.FindProperty("preconditionKeys"), new[] { AIWorldStateKey.TargetInSpellRange });
            SetBoolArray(so.FindProperty("preconditionValues"), new[] { true });
            SetEnumArray(so.FindProperty("effectKeys"), new[] { AIWorldStateKey.TargetIsDead });
            SetBoolArray(so.FindProperty("effectValues"), new[] { true });

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(action);
        }

        private static void CreateOrUpdateGambitRule(string path, string ruleName, int priority, IGambitCondition cond, IGambitAction act)
        {
            var rule = AssetDatabase.LoadAssetAtPath<GambitRuleSO>(path);
            if (rule == null)
            {
                rule = ScriptableObject.CreateInstance<GambitRuleSO>();
                AssetDatabase.CreateAsset(rule, path);
            }

            SerializedObject so = new SerializedObject(rule);
            so.FindProperty("ruleName").stringValue = ruleName;
            so.FindProperty("priority").intValue = priority;
            so.ApplyModifiedProperties();

            rule.Conditions = new System.Collections.Generic.List<IGambitCondition> { cond };
            rule.Action = act;
            EditorUtility.SetDirty(rule);
        }

        private static void SetEnumArray(SerializedProperty prop, AIWorldStateKey[] values)
        {
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).intValue = (int)values[i];
            }
        }

        private static void SetBoolArray(SerializedProperty prop, bool[] values)
        {
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).boolValue = values[i];
            }
        }
    }
}
