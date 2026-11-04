namespace Game.Combat.Domain
{
    /// <summary>Quan hệ của hệ tấn công đối với hệ phòng thủ.</summary>
    public enum ElementRelation
    {
        Neutral,
        Generates,
        GeneratedBy,
        Overcomes,
        OvercomedBy
    }
}
