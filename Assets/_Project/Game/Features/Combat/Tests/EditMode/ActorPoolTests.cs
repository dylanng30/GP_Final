using NUnit.Framework;

namespace Game.Combat.Domain.Tests
{
    public class ActorPoolTests
    {
        private const float Tolerance = 0.001f;

        private static HealthPool CreateHealth(float max = 100f)
        {
            var health = new HealthPool();
            health.Restore(max);
            return health;
        }

        [Test]
        public void TakeDamage_ReducesHealth()
        {
            HealthPool health = CreateHealth();

            DamageOutcome outcome = health.TakeDamage(30f);

            Assert.That(health.Current, Is.EqualTo(70f).Within(Tolerance));
            Assert.That(outcome.HealthLost, Is.EqualTo(30f).Within(Tolerance));
            Assert.That(health.IsDead, Is.False);
        }

        [Test]
        public void LethalDamage_RaisesDiedOnce()
        {
            HealthPool health = CreateHealth();
            int diedCount = 0;
            health.Died += () => diedCount++;

            DamageOutcome outcome = health.TakeDamage(500f);
            health.TakeDamage(10f);

            Assert.That(outcome.IsLethal, Is.True);
            Assert.That(health.Current, Is.Zero);
            Assert.That(diedCount, Is.EqualTo(1));
        }

        [Test]
        public void Heal_CannotExceedMax()
        {
            HealthPool health = CreateHealth();
            health.TakeDamage(10f);

            Assert.That(health.Heal(50f), Is.EqualTo(10f).Within(Tolerance));
            Assert.That(health.Current, Is.EqualTo(100f).Within(Tolerance));
        }

        [Test]
        public void Heal_DoesNothingWhenDead()
        {
            HealthPool health = CreateHealth();
            health.TakeDamage(100f);

            Assert.That(health.Heal(50f), Is.Zero);
            Assert.That(health.IsDead, Is.True);
        }

        [Test]
        public void SetMax_ClampsCurrentHealth()
        {
            HealthPool health = CreateHealth();

            health.SetMax(60f);

            Assert.That(health.Current, Is.EqualTo(60f).Within(Tolerance));
        }

        [Test]
        public void Shield_AbsorbsAndIsConsumed()
        {
            HealthPool health = CreateHealth();
            health.Shields.Add(this, 40f);

            DamageOutcome first = health.TakeDamage(30f);
            DamageOutcome second = health.TakeDamage(30f);

            Assert.That(first.Absorbed, Is.EqualTo(30f).Within(Tolerance));
            Assert.That(first.HealthLost, Is.Zero);
            Assert.That(second.Absorbed, Is.EqualTo(10f).Within(Tolerance));
            Assert.That(second.HealthLost, Is.EqualTo(20f).Within(Tolerance));
            Assert.That(health.Shields.Total, Is.Zero);
        }

        [Test]
        public void Shield_RemovedBySourceOnly()
        {
            var shields = new ShieldPool();
            object sourceA = new object();
            object sourceB = new object();
            shields.Add(sourceA, 10f);
            shields.Add(sourceB, 20f);

            shields.RemoveSource(sourceA);

            Assert.That(shields.Total, Is.EqualTo(20f).Within(Tolerance));
        }

        [Test]
        public void Restore_ClearsDeathAndShields()
        {
            HealthPool health = CreateHealth();
            health.Shields.Add(this, 10f);
            health.TakeDamage(500f);

            health.Restore(100f);

            Assert.That(health.IsDead, Is.False);
            Assert.That(health.Current, Is.EqualTo(100f).Within(Tolerance));
            Assert.That(health.Shields.Total, Is.Zero);
        }

        [Test]
        public void Energy_IsClampedToMax()
        {
            var energy = new EnergyPool(100f);

            energy.Gain(150f);

            Assert.That(energy.Current, Is.EqualTo(100f).Within(Tolerance));
        }

        [Test]
        public void Energy_TrySpendFailsWhenInsufficient()
        {
            var energy = new EnergyPool(100f);
            energy.Gain(30f);

            Assert.That(energy.TrySpend(50f), Is.False);
            Assert.That(energy.Current, Is.EqualTo(30f).Within(Tolerance));
            Assert.That(energy.TrySpend(30f), Is.True);
            Assert.That(energy.Current, Is.Zero);
        }
    }
}
