namespace Game.Combat.Domain
{
    public interface IDamageStep
    {
        void Apply(ref DamageContext context);
    }
}
