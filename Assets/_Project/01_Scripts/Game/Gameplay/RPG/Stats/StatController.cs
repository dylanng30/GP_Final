using System;
using System.Collections.Generic;
using Dylanng.Core.Base;
using UnityEngine;

namespace Dylanng.Gameplay.RPG.Stats
{
    public class StatController : ComponentBase
    {


        private readonly Dictionary<StatType, Stat> _stats = new Dictionary<StatType, Stat>();

        public event Action<StatType, float, float> OnAnyStatChanged;

        public float MaxHP => GetStatValue(StatType.MaxHP);
        public float ATK => GetStatValue(StatType.ATK);
        public float DEF => GetStatValue(StatType.DEF);
        public float Speed => GetStatValue(StatType.Speed);
        public float CritRate => GetStatValue(StatType.CritRate);
        public float CritDamage => GetStatValue(StatType.CritDamage);

        protected override void Awake()
        {
            base.Awake();
        }

        public void Initialize(string characterId, int level, int starLevel)
        {
            var statRepo = Dylanng.Core.ServiceLocator.Get<IStatRepository>();
            if (statRepo == null)
            {
                Debug.LogError("[StatController] IStatRepository not found!");
                return;
            }

            var statData = statRepo.GetHeroStatData(characterId);
            if (statData == null)
            {
                Debug.LogWarning($"[StatController] No stat data found for character ID: {characterId}");
                return;
            }

            float maxHp = StatGrowthCalculator.CalculateStat(statData.BaseHp, statData.HpGrowthPerLevel, level, starLevel, statData.StarUpMultiplier);
            float atk = StatGrowthCalculator.CalculateStat(statData.BaseAtk, statData.AtkGrowthPerLevel, level, starLevel, statData.StarUpMultiplier);
            float def = StatGrowthCalculator.CalculateStat(statData.BaseDef, statData.DefGrowthPerLevel, level, starLevel, statData.StarUpMultiplier);
            float speed = statData.BaseSpeed;
            float critRate = statData.BaseCritRate;
            float critDamage = statData.BaseCritDamage;
            float maxEnergy = statData.MaxEnergy;

            InitializeStat(StatType.MaxHP, maxHp);
            InitializeStat(StatType.ATK, atk);
            InitializeStat(StatType.DEF, def);
            InitializeStat(StatType.Speed, speed);
            InitializeStat(StatType.CritRate, critRate);
            InitializeStat(StatType.CritDamage, critDamage);
            InitializeStat(StatType.MaxEnergy, maxEnergy);
        }

        public void InitializeStat(StatType type, float baseValue)
        {
            if (!_stats.TryGetValue(type, out Stat stat))
            {
                stat = new Stat(type, baseValue);
                stat.OnValueChanged += HandleStatChanged;
                _stats[type] = stat;
            }
            else
            {
                stat.SetBaseValue(baseValue);
            }
        }

        public float GetStatValue(StatType type)
        {
            return _stats.TryGetValue(type, out Stat stat) ? stat.Value : 0f;
        }

        public Stat GetStat(StatType type)
        {
            _stats.TryGetValue(type, out Stat stat);
            return stat;
        }

        public void AddModifier(StatType type, StatModifier mod)
        {
            if (mod == null) return;
            if (!_stats.TryGetValue(type, out Stat stat))
            {
                InitializeStat(type, 0f);
                stat = _stats[type];
            }
            stat.AddModifier(mod);
        }

        public void AddModifier(StatModifier mod, StatField field)
        {
            AddModifier((StatType)field, mod);
        }

        public bool RemoveModifier(StatType type, StatModifier mod)
        {
            if (_stats.TryGetValue(type, out Stat stat))
            {
                return stat.RemoveModifier(mod);
            }
            return false;
        }

        public bool RemoveModifier(StatModifier mod, StatField field)
        {
            return RemoveModifier((StatType)field, mod);
        }

        public bool RemoveAllModifiersFromSource(object source)
        {
            if (source == null) return false;
            bool removed = false;
            foreach (var kvp in _stats)
            {
                removed |= kvp.Value.RemoveAllModifiersFromSource(source);
            }
            return removed;
        }

        private void HandleStatChanged(StatType type, float oldVal, float newVal)
        {
            OnAnyStatChanged?.Invoke(type, oldVal, newVal);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            foreach (var kvp in _stats)
            {
                kvp.Value.OnValueChanged -= HandleStatChanged;
            }
            _stats.Clear();
        }
    }

    // Complete replacement alias ensuring 100% backward compatibility with CharacterStats
    public class CharacterStats : StatController
    {
    }
}
