using System;
using Game.Combat.Stats;
using Game.Data.Hero;

namespace Game.Combat.Domain
{
    [Flags]
    public enum ControlFlags
    {
        None = 0,
        CannotMove = 1,
        CannotAct = 2
    }

    public enum StatusStackPolicy
    {
        Refresh,
        Stack,
        KeepExisting
    }

    [Serializable]
    public class StatBuff
    {
        public StatType StatType;
        public float Value;
        public StatModType ModifierType = StatModType.PercentAdd;

        public StatBuff()
        {
        }

        public StatBuff(StatType statType, float value, StatModType modifierType)
        {
            StatType = statType;
            Value = value;
            ModifierType = modifierType;
        }
    }

    [Serializable]
    public struct ElementDamageModifier
    {
        public GameElement Element;
        public float Multiplier;

        public ElementDamageModifier(GameElement element, float multiplier)
        {
            Element = element;
            Multiplier = multiplier;
        }
    }
}
