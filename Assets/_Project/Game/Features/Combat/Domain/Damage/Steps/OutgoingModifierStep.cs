namespace Game.Combat.Domain
{
    /// <summary>Áp hệ số từ trạng thái của người gây sát thương, ví dụ Thiêu Đốt giảm sát thương hệ Kim.</summary>
    public sealed class OutgoingModifierStep : IDamageStep
    {
        public void Apply(ref DamageContext context)
        {
            context.Amount *= context.Request.OutgoingMultiplier;
        }
    }
}
