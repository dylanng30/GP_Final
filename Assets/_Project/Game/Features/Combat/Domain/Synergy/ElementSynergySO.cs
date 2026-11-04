using System.Collections.Generic;
using Game.Data.Hero;
using UnityEngine;

namespace Game.Combat.Domain
{
    /// <summary>Liên kết tương sinh: khi đội có Source và Target, hero hệ Target nhận Buffs.</summary>
    [CreateAssetMenu(fileName = "NewSynergy", menuName = "Dylanng/RPG/Combat/Element Synergy")]
    public class ElementSynergySO : ScriptableObject
    {
        public string DisplayName;
        [TextArea(2, 3)] public string Description;
        public GameElement Source;
        public GameElement Target;
        public List<StatBuff> Buffs = new List<StatBuff>();
    }

    public static class SynergyEvaluator
    {
        /// <summary>Lấy các liên kết đang kích hoạt. maxLinks bằng 0 nghĩa là không giới hạn.</summary>
        public static void FindActive(
            IReadOnlyList<GameElement> partyElements,
            IReadOnlyList<ElementSynergySO> synergies,
            int maxLinks,
            List<ElementSynergySO> results)
        {
            results.Clear();
            for (int i = 0; i < synergies.Count; i++)
            {
                if (maxLinks > 0 && results.Count >= maxLinks) return;

                ElementSynergySO synergy = synergies[i];
                if (Contains(partyElements, synergy.Source) && Contains(partyElements, synergy.Target))
                {
                    results.Add(synergy);
                }
            }
        }

        private static bool Contains(IReadOnlyList<GameElement> elements, GameElement element)
        {
            for (int i = 0; i < elements.Count; i++)
            {
                if (elements[i] == element) return true;
            }
            return false;
        }
    }
}
