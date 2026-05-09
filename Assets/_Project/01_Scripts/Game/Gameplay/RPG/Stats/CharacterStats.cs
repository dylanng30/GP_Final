using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dylanng.Core.Base;
using UnityEngine;

namespace Dylanng.Gameplay.RPG.Stats
{
    public class CharacterStats : ComponentBase
    {
        [SerializeField] private CharacterBaseStatsSO _baseStats;
        
        private readonly List<StatModifier> _hpModifiers = new List<StatModifier>();
        private readonly List<StatModifier> _atkModifiers = new List<StatModifier>();
        private readonly List<StatModifier> _defModifiers = new List<StatModifier>();
        private readonly List<StatModifier> _speedModifiers = new List<StatModifier>();

        public float MaxHP => CalculateFinalValue(_baseStats.BaseHP, _hpModifiers);
        public float ATK => CalculateFinalValue(_baseStats.BaseATK, _atkModifiers);
        public float DEF => CalculateFinalValue(_baseStats.BaseDEF, _defModifiers);
        public float Speed => CalculateFinalValue(_baseStats.BaseSpeed, _speedModifiers);

        public void AddModifier(StatModifier mod, StatField field)
        {
            GetTargetList(field).Add(mod);
            GetTargetList(field).Sort(CompareModifierOrder);
        }

        public bool RemoveModifier(StatModifier mod, StatField field)
        {
            return GetTargetList(field).Remove(mod);
        }

        public bool RemoveAllModifiersFromSource(object source)
        {
            bool removed = false;
            removed |= RemoveFromList(_hpModifiers, source);
            removed |= RemoveFromList(_atkModifiers, source);
            removed |= RemoveFromList(_defModifiers, source);
            removed |= RemoveFromList(_speedModifiers, source);
            return removed;
        }

        private bool RemoveFromList(List<StatModifier> list, object source)
        {
            int count = list.RemoveAll(m => m.Source == source);
            return count > 0;
        }

        private int CompareModifierOrder(StatModifier a, StatModifier b)
        {
            if (a.Order < b.Order) return -1;
            if (a.Order > b.Order) return 1;
            return 0;
        }

        private float CalculateFinalValue(float baseValue, List<StatModifier> modifiers)
        {
            float finalValue = baseValue;
            float sumPercentAdd = 0;

            for (int i = 0; i < modifiers.Count; i++)
            {
                StatModifier mod = modifiers[i];

                if (mod.Type == StatModType.Flat)
                {
                    finalValue += mod.Value;
                }
                else if (mod.Type == StatModType.PercentAdd)
                {
                    sumPercentAdd += mod.Value;
                    if (i + 1 >= modifiers.Count || modifiers[i + 1].Type != StatModType.PercentAdd)
                    {
                        finalValue *= 1 + sumPercentAdd;
                        sumPercentAdd = 0;
                    }
                }
                else if (mod.Type == StatModType.PercentMult)
                {
                    finalValue *= 1 + mod.Value;
                }
            }

            return (float)Math.Round(finalValue, 4);
        }

        private List<StatModifier> GetTargetList(StatField field)
        {
            switch (field)
            {
                case StatField.HP: return _hpModifiers;
                case StatField.ATK: return _atkModifiers;
                case StatField.DEF: return _defModifiers;
                case StatField.Speed: return _speedModifiers;
                default: return null;
            }
        }
    }

    public enum StatField
    {
        HP, ATK, DEF, Speed
    }
}
