using System.Collections.Generic;
using Game.Data.Hero;
using UnityEngine;

namespace Game.Combat.Domain
{
    /// <summary>Dữ liệu tương sinh, tương khắc. Mỗi phần tử trong vòng sinh hoặc khắc phần tử kế tiếp.</summary>
    [CreateAssetMenu(fileName = "ElementChart", menuName = "Dylanng/RPG/Combat/Element Chart")]
    public class ElementChartSO : ScriptableObject, IElementChart
    {
        [Header("--- CYCLES ---")]
        [Tooltip("Vòng tương sinh: Mộc > Hỏa > Thổ > Kim > Thủy")]
        [SerializeField] private List<GameElement> generationCycle = new List<GameElement>();

        [Tooltip("Vòng tương khắc: Mộc > Thổ > Thủy > Hỏa > Kim")]
        [SerializeField] private List<GameElement> overcomingCycle = new List<GameElement>();

        [Tooltip("Các cặp hệ khắc lẫn nhau, ví dụ Light và Darkness")]
        [SerializeField] private List<ElementPair> mutualOvercomes = new List<ElementPair>();

        [Header("--- DAMAGE MULTIPLIERS ---")]
        [SerializeField] private float overcomesMultiplier = 1.2f;
        [SerializeField] private float overcomedByMultiplier = 0.8f;

        private ElementChart _chart;

        private ElementChart Chart => _chart ??= new ElementChart(
            generationCycle, overcomingCycle, mutualOvercomes, overcomesMultiplier, overcomedByMultiplier);

        public ElementRelation GetRelation(GameElement attacker, GameElement defender)
        {
            return Chart.GetRelation(attacker, defender);
        }

        public float GetDamageMultiplier(ElementRelation relation)
        {
            return Chart.GetDamageMultiplier(relation);
        }

        private void OnValidate()
        {
            _chart = null;
        }
    }
}
