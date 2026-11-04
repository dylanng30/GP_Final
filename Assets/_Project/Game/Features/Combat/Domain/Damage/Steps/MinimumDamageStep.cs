using System;

namespace Game.Combat.Domain
{
    public sealed class MinimumDamageStep : IDamageStep
    {
        private readonly float _minimumDamage;

        public MinimumDamageStep(float minimumDamage)
        {
            _minimumDamage = minimumDamage;
        }

        public void Apply(ref DamageContext context)
        {
            context.Amount = Math.Max(_minimumDamage, context.Amount);
        }
    }
}
