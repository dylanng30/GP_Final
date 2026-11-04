namespace Game.Combat.Stats
{
    public enum StatType
    {
        // Core Combat Stats
        MaxHP = 0,
        ATK = 1,
        DEF = 2,
        Speed = 3,
        
        // Advanced Combat Stats
        CritRate = 4,
        CritDamage = 5,
        Lifesteal = 6,
        ArmorPenetration = 7,
        EnergyRegen = 8,
        MaxEnergy = 9,
        
        // Defensive Buffs
        Shield = 10,
        DamageImmunity = 11,

        // Element Bonuses
        DamageBonus = 12,
        HealingBonus = 13,
        ControlDurationBonus = 14
    }

}
