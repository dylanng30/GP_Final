using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat.Stats
{
    public class Stat
    {
        public StatType Type { get; private set; }
        public float BaseValue { get; private set; }
        
        private float _cachedFinalValue;
        private bool _isDirty = true;
        
        private readonly List<StatModifier> _modifiers = new List<StatModifier>();
        public event Action<StatType, float, float> OnValueChanged; // (Type, OldValue, NewValue)

        public Stat(StatType type, float baseValue)
        {
            Type = type;
            BaseValue = baseValue;
            _cachedFinalValue = baseValue;
            _isDirty = false;
        }

        public float Value
        {
            get
            {
                if (_isDirty)
                {
                    float oldVal = _cachedFinalValue;
                    _cachedFinalValue = CalculateFinalValue();
                    _isDirty = false;
                    
                    if (Mathf.Abs(oldVal - _cachedFinalValue) > 0.0001f)
                    {
                        OnValueChanged?.Invoke(Type, oldVal, _cachedFinalValue);
                    }
                }
                return _cachedFinalValue;
            }
        }

        public void SetBaseValue(float newBaseValue)
        {
            if (Mathf.Abs(BaseValue - newBaseValue) <= 0.0001f) return;
            BaseValue = newBaseValue;
            _isDirty = true;
            _ = Value; // Trigger immediate recalculation & event dispatch
        }

        public void AddModifier(StatModifier mod)
        {
            if (mod == null) return;
            _modifiers.Add(mod);
            _modifiers.Sort((a, b) => a.Order.CompareTo(b.Order));
            _isDirty = true;
            _ = Value; // Trigger immediate recalculation & event dispatch
        }

        public bool RemoveModifier(StatModifier mod)
        {
            if (mod == null) return false;
            if (_modifiers.Remove(mod))
            {
                _isDirty = true;
                _ = Value; // Trigger immediate recalculation & event dispatch
                return true;
            }
            return false;
        }

        public bool RemoveAllModifiersFromSource(object source)
        {
            if (source == null) return false;
            int removed = _modifiers.RemoveAll(m => m.Source == source);
            if (removed > 0)
            {
                _isDirty = true;
                _ = Value; // Trigger immediate recalculation & event dispatch
                return true;
            }
            return false;
        }

        public IReadOnlyList<StatModifier> GetModifiers() => _modifiers;

        private float CalculateFinalValue()
        {
            float finalValue = BaseValue;
            float sumPercentAdd = 0f;

            for (int i = 0; i < _modifiers.Count; i++)
            {
                StatModifier mod = _modifiers[i];
                switch (mod.Type)
                {
                    case StatModType.Flat:
                        finalValue += mod.Value;
                        break;
                    case StatModType.PercentAdd:
                        sumPercentAdd += mod.Value;
                        if (i + 1 >= _modifiers.Count || _modifiers[i + 1].Type != StatModType.PercentAdd)
                        {
                            finalValue *= (1f + sumPercentAdd);
                            sumPercentAdd = 0f;
                        }
                        break;
                    case StatModType.PercentMult:
                        finalValue *= (1f + mod.Value);
                        break;
                    case StatModType.Override:
                        finalValue = mod.Value;
                        break;
                }
            }
            return (float)Math.Round(finalValue, 4);
        }
    }
}
