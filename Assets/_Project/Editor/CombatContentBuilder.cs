using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dylanng;
using Game.Combat.Abilities;
using Game.Combat.Abilities.Effects;
using Game.Combat.Stats;
using Game.Combat.Domain;
using Game.Data.Hero;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Combat
{
    /// <summary>
    /// Dựng dữ liệu combat mặc định từ code. Chạy lại an toàn: asset đã có sẽ được ghi đè theo giá trị dưới đây.
    /// Menu: Tools > Combat > Build Combat Content
    /// </summary>
    public static partial class CombatContentBuilder
    {
        private const string SoRoot = "Assets/_Project/Game/Features/Combat/SOs";
        private const string StatusFolder = SoRoot + "/Status";
        private const string ReactionFolder = SoRoot + "/Reactions";
        private const string SynergyFolder = SoRoot + "/Synergies";
        private const string RulesPath = SoRoot + "/Rules/CombatRules.asset";

        [MenuItem("Tools/Combat/Build Combat Content")]
        public static void Build()
        {
            StatusCatalog statuses = BuildStatuses();
            List<ReactionRuleSO> reactions = BuildReactions(statuses);
            List<ElementSynergySO> synergies = BuildSynergies();
            UpdateCombatRules(reactions, synergies);
            BuildAbilities(statuses);
            BuildHud();
            BuildHealthBar();

            AssetDatabase.SaveAssets();
            Debug.Log("[Content] Đã dựng xong dữ liệu combat.");
        }

        private sealed class StatusCatalog
        {
            public StatusDefinitionSO Root;
            public StatusDefinitionSO Burn;
            public StatusDefinitionSO Petrify;
            public StatusDefinitionSO ArmorBreak;
            public StatusDefinitionSO Wet;
            public StatusDefinitionSO Stun;
            public StatusDefinitionSO TitanForm;
        }

        private static StatusCatalog BuildStatuses()
        {
            var set = new StatusCatalog
            {
                Root = CreateStatus("Status_Root", "Trói", GameElement.Wood, 2.5f, ControlFlags.CannotMove),
                Burn = CreateStatus("Status_Burn", "Thiêu Đốt", GameElement.Fire, 5f, ControlFlags.None),
                Petrify = CreateStatus("Status_Petrify", "Hóa Đá", GameElement.Earth, 2f,
                    ControlFlags.CannotMove | ControlFlags.CannotAct),
                ArmorBreak = CreateStatus("Status_ArmorBreak", "Phá Giáp", GameElement.Metal, 5f, ControlFlags.None),
                Wet = CreateStatus("Status_Wet", "Ướt", GameElement.Water, 4f, ControlFlags.None),
                TitanForm = CreateStatus("Status_TitanForm", "Cự Nhân", GameElement.Metal, 8f, ControlFlags.None),
                Stun = CreateStatus("Status_Stun", "Choáng", GameElement.Physical, 1.5f,
                    ControlFlags.CannotMove | ControlFlags.CannotAct)
            };

            set.Burn.TickInterval = 1f;
            set.Burn.TickAttackRatio = 0.2f;
            set.Burn.OutgoingDamageModifiers = new List<ElementDamageModifier>
            {
                new ElementDamageModifier(GameElement.Metal, 0.8f)
            };

            set.ArmorBreak.StatBuffs = new List<StatBuff>
            {
                new StatBuff(StatType.DEF, -0.3f, StatModType.PercentAdd)
            };

            set.Wet.StatBuffs = new List<StatBuff>
            {
                new StatBuff(StatType.Speed, -0.3f, StatModType.PercentAdd)
            };

            set.TitanForm.ScaleMultiplier = 1.5f;
            set.TitanForm.StatBuffs = new List<StatBuff>
            {
                new StatBuff(StatType.ATK, 0.4f, StatModType.PercentAdd),
                new StatBuff(StatType.DEF, 0.3f, StatModType.PercentAdd)
            };

            set.Root.LoopVfxPrefab = LoadVfx("MagicAuraGreen");
            set.Burn.LoopVfxPrefab = LoadVfx("TorchFire", "CampFire", "MagicAuraYellow");
            set.Wet.LoopVfxPrefab = LoadVfx("WaterRainRipples");

            foreach (StatusDefinitionSO status in new[]
                     {
                         set.Root, set.Burn, set.Petrify, set.ArmorBreak, set.Wet, set.Stun, set.TitanForm
                     })
            {
                EditorUtility.SetDirty(status);
            }

            return set;
        }

        private static StatusDefinitionSO CreateStatus(
            string assetName, string displayName, GameElement element, float duration, ControlFlags flags)
        {
            var status = GetOrCreate<StatusDefinitionSO>($"{StatusFolder}/{assetName}.asset");
            status.DisplayName = displayName;
            status.Element = element;
            status.Duration = duration;
            status.ControlFlags = flags;
            status.StackPolicy = StatusStackPolicy.Refresh;
            status.MaxStacks = 1;
            status.StatBuffs = new List<StatBuff>();
            status.OutgoingDamageModifiers = new List<ElementDamageModifier>();
            status.TickInterval = 0f;
            status.TickDamage = 0f;
            status.TickAttackRatio = 0f;
            status.ScaleMultiplier = 1f;
            return status;
        }

        private static List<ReactionRuleSO> BuildReactions(StatusCatalog statuses)
        {
            return new List<ReactionRuleSO>
            {
                CreateReaction("Reaction_Wood_Earth", "Phá Hóa Đá", statuses.Root, statuses.Petrify),
                CreateReaction("Reaction_Earth_Water", "Khô Cạn", statuses.Petrify, statuses.Wet),
                CreateReaction("Reaction_Metal_Wood", "Phá Dây Trói", statuses.ArmorBreak, statuses.Root),
                CreateReaction("Reaction_Water_Fire", "Dập Lửa", statuses.Wet, statuses.Burn)
            };
        }

        private static ReactionRuleSO CreateReaction(
            string assetName, string displayName, StatusDefinitionSO incoming, StatusDefinitionSO required)
        {
            var rule = GetOrCreate<ReactionRuleSO>($"{ReactionFolder}/{assetName}.asset");
            rule.DisplayName = displayName;
            rule.Incoming = incoming;
            rule.RequiredOnTarget = required;
            rule.RemovesRequiredStatus = true;
            rule.BonusAttackRatio = 0.15f;
            EditorUtility.SetDirty(rule);
            return rule;
        }

        private static List<ElementSynergySO> BuildSynergies()
        {
            return new List<ElementSynergySO>
            {
                CreateSynergy("Synergy_Wood_Fire", "Mộc sinh Hỏa", "Hỏa tăng sát thương",
                    GameElement.Wood, GameElement.Fire, new StatBuff(StatType.DamageBonus, 0.15f, StatModType.Flat)),
                CreateSynergy("Synergy_Fire_Earth", "Hỏa sinh Thổ", "Thổ tăng phòng thủ",
                    GameElement.Fire, GameElement.Earth, new StatBuff(StatType.DEF, 0.2f, StatModType.PercentAdd)),
                CreateSynergy("Synergy_Earth_Metal", "Thổ sinh Kim", "Kim tăng tỉ lệ chí mạng",
                    GameElement.Earth, GameElement.Metal, new StatBuff(StatType.CritRate, 0.15f, StatModType.Flat)),
                CreateSynergy("Synergy_Metal_Water", "Kim sinh Thủy", "Thủy tăng hồi máu",
                    GameElement.Metal, GameElement.Water, new StatBuff(StatType.HealingBonus, 0.25f, StatModType.Flat)),
                CreateSynergy("Synergy_Water_Wood", "Thủy sinh Mộc", "Mộc tăng thời gian khống chế",
                    GameElement.Water, GameElement.Wood, new StatBuff(StatType.ControlDurationBonus, 0.3f, StatModType.Flat))
            };
        }

        private static ElementSynergySO CreateSynergy(
            string assetName, string displayName, string description, GameElement source, GameElement target, StatBuff buff)
        {
            var synergy = GetOrCreate<ElementSynergySO>($"{SynergyFolder}/{assetName}.asset");
            synergy.DisplayName = displayName;
            synergy.Description = description;
            synergy.Source = source;
            synergy.Target = target;
            synergy.Buffs = new List<StatBuff> { buff };
            EditorUtility.SetDirty(synergy);
            return synergy;
        }

        private static void UpdateCombatRules(List<ReactionRuleSO> reactions, List<ElementSynergySO> synergies)
        {
            var rules = AssetDatabase.LoadAssetAtPath<CombatRulesSO>(RulesPath);
            var serialized = new SerializedObject(rules);

            AssignList(serialized.FindProperty("reactionRules"), reactions);
            AssignList(serialized.FindProperty("synergies"), synergies);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rules);
        }

        private static void AssignList<T>(SerializedProperty property, IReadOnlyList<T> items) where T : Object
        {
            property.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
        }

        private static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder)) return;

            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        /// <summary>Tìm prefab VFX theo tên, thử lần lượt từng tên. Trả về null nếu không có.</summary>
        private static GameObject LoadVfx(params string[] prefabNames)
        {
            foreach (string prefabName in prefabNames)
            {
                string path = AssetDatabase.FindAssets($"{prefabName} t:Prefab", new[] { "Assets/Epic Toon FX/Prefabs" })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .FirstOrDefault(candidate => Path.GetFileNameWithoutExtension(candidate) == prefabName);

                if (path != null) return AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }

            Debug.LogWarning($"[Content] Không tìm thấy VFX: {string.Join(", ", prefabNames)}");
            return null;
        }
    }
}
