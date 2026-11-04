namespace Game.Combat.Domain
{
    /// <summary>Trạng thái được các bước trong pipeline lần lượt cập nhật.</summary>
    public struct DamageContext
    {
        public readonly DamageRequest Request;
        public float Amount;
        public bool IsCritical;
        public ElementRelation Relation;

        public DamageContext(in DamageRequest request)
        {
            Request = request;
            Amount = request.Amount;
            IsCritical = false;
            Relation = ElementRelation.Neutral;
        }
    }
}
