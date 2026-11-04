namespace Game.Combat.Domain
{
    public sealed class ElementRelationStep : IDamageStep
    {
        private readonly IElementChart _chart;

        public ElementRelationStep(IElementChart chart)
        {
            _chart = chart;
        }

        public void Apply(ref DamageContext context)
        {
            context.Relation = _chart.GetRelation(context.Request.Element, context.Request.TargetElement);
            context.Amount *= _chart.GetDamageMultiplier(context.Relation);
        }
    }
}
