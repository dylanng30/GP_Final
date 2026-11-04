using System.Collections.Generic;

namespace Game.Combat.Domain
{
    /// <summary>Tính sát thương bằng cách chạy lần lượt các bước độc lập.</summary>
    public sealed class DamagePipeline
    {
        private readonly IReadOnlyList<IDamageStep> _steps;

        public DamagePipeline(IReadOnlyList<IDamageStep> steps)
        {
            _steps = steps;
        }

        public DamageResult Run(in DamageRequest request)
        {
            var context = new DamageContext(request);
            for (int i = 0; i < _steps.Count; i++)
            {
                _steps[i].Apply(ref context);
            }
            return new DamageResult(context.Amount, context.IsCritical, context.Relation);
        }
    }
}
