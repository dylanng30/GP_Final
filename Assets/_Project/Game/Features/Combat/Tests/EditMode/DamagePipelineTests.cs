using Game.Data.Hero;
using NUnit.Framework;

namespace Game.Combat.Domain.Tests
{
    public class DamagePipelineTests
    {
        private const float Tolerance = 0.001f;
        private const float DefaultCritMultiplier = 1.5f;
        private const float DefenseConstant = 100f;
        private const float MinimumDamage = 1f;

        private sealed class FixedRandom : IRandomSource
        {
            private readonly float _value;

            public FixedRandom(float value)
            {
                _value = value;
            }

            public float NextFloat() => _value;
        }

        private static DamagePipeline CreatePipeline(float randomValue = 0.99f)
        {
            var chart = new ElementChart(
                new[] { GameElement.Wood, GameElement.Fire, GameElement.Earth, GameElement.Metal, GameElement.Water },
                new[] { GameElement.Wood, GameElement.Earth, GameElement.Water, GameElement.Fire, GameElement.Metal },
                new[] { new ElementPair(GameElement.Light, GameElement.Darkness) },
                1.2f,
                0.8f);

            return new DamagePipeline(new IDamageStep[]
            {
                new ElementRelationStep(chart),
                new DamageBonusStep(),
                new CriticalStep(new FixedRandom(randomValue), DefaultCritMultiplier),
                new DefenseStep(DefenseConstant),
                new MinimumDamageStep(MinimumDamage)
            });
        }

        [Test]
        public void NeutralHit_KeepsAmount()
        {
            DamageResult result = CreatePipeline().Run(new DamageRequest(100f, GameElement.Physical, GameElement.Fire));

            Assert.That(result.Amount, Is.EqualTo(100f).Within(Tolerance));
            Assert.That(result.IsCritical, Is.False);
            Assert.That(result.Relation, Is.EqualTo(ElementRelation.Neutral));
        }

        [TestCase(GameElement.Fire, GameElement.Metal, 120f, ElementRelation.Overcomes)]
        [TestCase(GameElement.Metal, GameElement.Fire, 80f, ElementRelation.OvercomedBy)]
        [TestCase(GameElement.Wood, GameElement.Fire, 100f, ElementRelation.Generates)]
        public void ElementRelation_ChangesDamage(
            GameElement element, GameElement targetElement, float expected, ElementRelation relation)
        {
            DamageResult result = CreatePipeline().Run(new DamageRequest(100f, element, targetElement));

            Assert.That(result.Amount, Is.EqualTo(expected).Within(Tolerance));
            Assert.That(result.Relation, Is.EqualTo(relation));
        }

        [Test]
        public void DamageBonus_IncreasesDamage()
        {
            var request = new DamageRequest(100f, GameElement.Physical, GameElement.Physical, damageBonus: 0.25f);

            Assert.That(CreatePipeline().Run(request).Amount, Is.EqualTo(125f).Within(Tolerance));
        }

        [Test]
        public void Critical_UsesSourceCritDamage()
        {
            var request = new DamageRequest(100f, GameElement.Physical, GameElement.Physical, critRate: 0.5f, critDamage: 2f);

            DamageResult result = CreatePipeline(randomValue: 0.1f).Run(request);

            Assert.That(result.IsCritical, Is.True);
            Assert.That(result.Amount, Is.EqualTo(200f).Within(Tolerance));
        }

        [Test]
        public void Critical_FallsBackToDefaultMultiplier()
        {
            var request = new DamageRequest(100f, GameElement.Physical, GameElement.Physical, critRate: 1f);

            Assert.That(CreatePipeline(randomValue: 0f).Run(request).Amount, Is.EqualTo(150f).Within(Tolerance));
        }

        [Test]
        public void Critical_DoesNotTriggerWhenRollFails()
        {
            var request = new DamageRequest(100f, GameElement.Physical, GameElement.Physical, critRate: 0.5f, critDamage: 2f);

            Assert.That(CreatePipeline(randomValue: 0.5f).Run(request).IsCritical, Is.False);
        }

        [Test]
        public void Critical_IsSkippedWhenCanCritIsFalse()
        {
            var request = new DamageRequest(100f, GameElement.Physical, GameElement.Physical, critRate: 1f, canCrit: false);

            DamageResult result = CreatePipeline(randomValue: 0f).Run(request);

            Assert.That(result.IsCritical, Is.False);
            Assert.That(result.Amount, Is.EqualTo(100f).Within(Tolerance));
        }

        [TestCase(0f, 100f)]
        [TestCase(100f, 50f)]
        [TestCase(300f, 25f)]
        [TestCase(-50f, 100f)]
        public void Defense_ReducesDamage(float defense, float expected)
        {
            var request = new DamageRequest(100f, GameElement.Physical, GameElement.Physical, targetDefense: defense);

            Assert.That(CreatePipeline().Run(request).Amount, Is.EqualTo(expected).Within(Tolerance));
        }

        [Test]
        public void MinimumDamage_IsApplied()
        {
            var request = new DamageRequest(0.1f, GameElement.Physical, GameElement.Physical, targetDefense: 900f);

            Assert.That(CreatePipeline().Run(request).Amount, Is.EqualTo(MinimumDamage).Within(Tolerance));
        }

        [Test]
        public void AllModifiers_AreCombined()
        {
            var request = new DamageRequest(
                100f, GameElement.Fire, GameElement.Metal,
                damageBonus: 0.5f, critRate: 1f, critDamage: 2f, targetDefense: 100f);

            // 100 x 1.2 (khắc) x 1.5 (bonus) x 2 (chí mạng) x 0.5 (DEF)
            Assert.That(CreatePipeline(randomValue: 0f).Run(request).Amount, Is.EqualTo(180f).Within(Tolerance));
        }
    }
}
