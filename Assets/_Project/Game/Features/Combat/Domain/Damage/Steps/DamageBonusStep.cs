namespace Game.Combat.Domain
{
    public sealed class DamageBonusStep : IDamageStep
    {
        public void Apply(ref DamageContext context)
        {
            context.Amount *= 1f + context.Request.DamageBonus;
        }
    }
}
