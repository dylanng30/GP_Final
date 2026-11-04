using System.Collections.Generic;
using Game.Data.Hero;

namespace Game.Combat.Domain
{
    /// <summary>
    /// Bảng quan hệ giữa các hệ, dựng từ vòng tương sinh, vòng tương khắc và các cặp khắc lẫn nhau.
    /// Cặp không được khai báo là Neutral.
    /// </summary>
    public sealed class ElementChart : IElementChart
    {
        private const int KeyStride = 256;

        private readonly Dictionary<int, ElementRelation> _relations = new Dictionary<int, ElementRelation>();
        private readonly float _overcomesMultiplier;
        private readonly float _overcomedByMultiplier;

        public ElementChart(
            IReadOnlyList<GameElement> generationCycle,
            IReadOnlyList<GameElement> overcomingCycle,
            IReadOnlyList<ElementPair> mutualOvercomes,
            float overcomesMultiplier,
            float overcomedByMultiplier)
        {
            _overcomesMultiplier = overcomesMultiplier;
            _overcomedByMultiplier = overcomedByMultiplier;

            RegisterCycle(generationCycle, ElementRelation.Generates, ElementRelation.GeneratedBy);
            RegisterCycle(overcomingCycle, ElementRelation.Overcomes, ElementRelation.OvercomedBy);

            foreach (ElementPair pair in mutualOvercomes)
            {
                _relations[ToKey(pair.First, pair.Second)] = ElementRelation.Overcomes;
                _relations[ToKey(pair.Second, pair.First)] = ElementRelation.Overcomes;
            }
        }

        public ElementRelation GetRelation(GameElement attacker, GameElement defender)
        {
            return _relations.TryGetValue(ToKey(attacker, defender), out ElementRelation relation)
                ? relation
                : ElementRelation.Neutral;
        }

        public float GetDamageMultiplier(ElementRelation relation)
        {
            switch (relation)
            {
                case ElementRelation.Overcomes: return _overcomesMultiplier;
                case ElementRelation.OvercomedBy: return _overcomedByMultiplier;
                default: return 1f;
            }
        }

        private void RegisterCycle(IReadOnlyList<GameElement> cycle, ElementRelation forward, ElementRelation backward)
        {
            for (int i = 0; i < cycle.Count; i++)
            {
                GameElement current = cycle[i];
                GameElement next = cycle[(i + 1) % cycle.Count];
                _relations[ToKey(current, next)] = forward;
                _relations[ToKey(next, current)] = backward;
            }
        }

        private static int ToKey(GameElement attacker, GameElement defender)
        {
            return (int)attacker * KeyStride + (int)defender;
        }
    }
}
