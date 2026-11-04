namespace Game.Combat.Domain
{
    public readonly struct DamageResult
    {
        public static readonly DamageResult None = new DamageResult(0f, false, ElementRelation.Neutral);

        public readonly float Amount;
        public readonly bool IsCritical;
        public readonly ElementRelation Relation;

        public DamageResult(float amount, bool isCritical, ElementRelation relation)
        {
            Amount = amount;
            IsCritical = isCritical;
            Relation = relation;
        }
    }
}
