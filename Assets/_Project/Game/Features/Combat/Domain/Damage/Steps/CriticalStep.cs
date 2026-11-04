namespace Game.Combat.Domain
{
    public sealed class CriticalStep : IDamageStep
    {
        private readonly IRandomSource _random;
        private readonly float _defaultMultiplier;

        public CriticalStep(IRandomSource random, float defaultMultiplier)
        {
            _random = random;
            _defaultMultiplier = defaultMultiplier;
        }

        public void Apply(ref DamageContext context)
        {
            DamageRequest request = context.Request;
            if (!request.CanCrit || _random.NextFloat() >= request.CritRate) return;

            context.IsCritical = true;
            context.Amount *= request.CritDamage >= 1f ? request.CritDamage : _defaultMultiplier;
        }
    }
}
