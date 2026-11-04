using Game.Data.Hero;

namespace Game.Combat.Domain
{
    /// <summary>Toàn bộ dữ liệu đầu vào của một lần tính sát thương.</summary>
    public readonly struct DamageRequest
    {
        public readonly float Amount;
        public readonly GameElement Element;
        public readonly GameElement TargetElement;
        public readonly float DamageBonus;
        public readonly float CritRate;
        public readonly float CritDamage;
        public readonly float TargetDefense;
        public readonly bool CanCrit;

        public DamageRequest(
            float amount,
            GameElement element,
            GameElement targetElement,
            float damageBonus = 0f,
            float critRate = 0f,
            float critDamage = 0f,
            float targetDefense = 0f,
            bool canCrit = true)
        {
            Amount = amount;
            Element = element;
            TargetElement = targetElement;
            DamageBonus = damageBonus;
            CritRate = critRate;
            CritDamage = critDamage;
            TargetDefense = targetDefense;
            CanCrit = canCrit;
        }
    }
}
