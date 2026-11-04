using System.Collections.Generic;
using Game.Data.Hero;
using NUnit.Framework;
using UnityEditor;

namespace Game.Combat.Domain.Tests
{
    public class ElementChartTests
    {
        private const string ChartAssetPath = "Assets/_Project/Game/Features/Combat/SOs/Rules/ElementChart.asset";

        private const GameElement Kim = GameElement.Metal;
        private const GameElement Thuy = GameElement.Water;
        private const GameElement Moc = GameElement.Wood;
        private const GameElement Hoa = GameElement.Fire;
        private const GameElement Tho = GameElement.Earth;

        private const ElementRelation N = ElementRelation.Neutral;
        private const ElementRelation Sinh = ElementRelation.Generates;
        private const ElementRelation DuocSinh = ElementRelation.GeneratedBy;
        private const ElementRelation Khac = ElementRelation.Overcomes;
        private const ElementRelation BiKhac = ElementRelation.OvercomedBy;

        private ElementChartSO _chart;

        [SetUp]
        public void SetUp()
        {
            _chart = AssetDatabase.LoadAssetAtPath<ElementChartSO>(ChartAssetPath);
            Assert.That(_chart, Is.Not.Null, "Thiếu asset ElementChart");
        }

        // Tương sinh: Mộc > Hỏa > Thổ > Kim > Thủy > Mộc
        // Tương khắc: Mộc > Thổ > Thủy > Hỏa > Kim > Mộc
        private static IEnumerable<TestCaseData> FiveElementPairs()
        {
            GameElement[] elements = { Kim, Thuy, Moc, Hoa, Tho };
            ElementRelation[,] expected =
            {
                //          Kim       Thủy      Mộc       Hỏa       Thổ
                /* Kim  */ { N,        Sinh,     Khac,     BiKhac,   DuocSinh },
                /* Thủy */ { DuocSinh, N,        Sinh,     Khac,     BiKhac },
                /* Mộc  */ { BiKhac,   DuocSinh, N,        Sinh,     Khac },
                /* Hỏa  */ { Khac,     BiKhac,   DuocSinh, N,        Sinh },
                /* Thổ  */ { Sinh,     Khac,     BiKhac,   DuocSinh, N }
            };

            for (int attacker = 0; attacker < elements.Length; attacker++)
            {
                for (int defender = 0; defender < elements.Length; defender++)
                {
                    yield return new TestCaseData(elements[attacker], elements[defender], expected[attacker, defender])
                        .SetName($"{elements[attacker]} đánh {elements[defender]} là {expected[attacker, defender]}");
                }
            }
        }

        [TestCaseSource(nameof(FiveElementPairs))]
        public void FiveElements_HaveExpectedRelation(GameElement attacker, GameElement defender, ElementRelation expected)
        {
            Assert.That(_chart.GetRelation(attacker, defender), Is.EqualTo(expected));
        }

        [TestCase(ElementRelation.Overcomes, 1.2f)]
        [TestCase(ElementRelation.OvercomedBy, 0.8f)]
        [TestCase(ElementRelation.Generates, 1f)]
        [TestCase(ElementRelation.GeneratedBy, 1f)]
        [TestCase(ElementRelation.Neutral, 1f)]
        public void DamageMultiplier_MatchesRelation(ElementRelation relation, float expected)
        {
            Assert.That(_chart.GetDamageMultiplier(relation), Is.EqualTo(expected).Within(0.0001f));
        }

        [TestCase(GameElement.Physical, GameElement.Fire)]
        [TestCase(GameElement.Fire, GameElement.Physical)]
        [TestCase(GameElement.Light, GameElement.Fire)]
        [TestCase(GameElement.Darkness, GameElement.Water)]
        public void ElementsOutsideCycles_AreNeutral(GameElement attacker, GameElement defender)
        {
            Assert.That(_chart.GetRelation(attacker, defender), Is.EqualTo(ElementRelation.Neutral));
        }

        [Test]
        public void LightAndDarkness_OvercomeEachOther()
        {
            Assert.That(_chart.GetRelation(GameElement.Light, GameElement.Darkness), Is.EqualTo(ElementRelation.Overcomes));
            Assert.That(_chart.GetRelation(GameElement.Darkness, GameElement.Light), Is.EqualTo(ElementRelation.Overcomes));
        }
    }
}
