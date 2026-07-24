namespace Game.Gameplay.AI.Core
{
    public enum AIWorldStateKey : byte
    {
        TargetExists = 0,
        TargetInMeleeRange = 1,
        TargetInSpellRange = 2,
        TargetCastingSpell = 3,
        TargetIsDead = 4,
        IsAtSpawn = 5,
        
        IsLowHealth = 10,
        IsStaggered = 11,
        HasShield = 12,
        IsCasting = 13,
        IsBossPhase2 = 14,
        IsBossPhase3 = 15,
        
        CanUseBossSkill1 = 20,
        CanUseBossSkill2 = 21,
        CanUseBossSkill3 = 22,
        
        HasSquadAttackToken = 30,
        IsFlanking = 31,
        IsRetreating = 32
    }
}
