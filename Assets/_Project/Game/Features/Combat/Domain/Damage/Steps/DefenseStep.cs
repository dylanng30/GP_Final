using System;

namespace Game.Combat.Domain
{
    /// <summary>Giảm sát thương theo công thức hằng số / (hằng số + DEF).</summary>
    public sealed class DefenseStep : IDamageStep
    {
        private readonly float _defenseConstant;

        public DefenseStep(float defenseConstant)
        {
            _defenseConstant = defenseConstant;
        }

        public void Apply(ref DamageContext context)
        {
            float defense = Math.Max(0f, context.Request.TargetDefense);
            context.Amount *= _defenseConstant / (_defenseConstant + defense);
        }
    }
}
