using NUnit.Framework;

namespace Game.Combat.Domain.Tests
{
    public class AbilityScalingTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void ChainPotency_FirstHit_IsFull()
        {
            Assert.AreEqual(1f, AbilityScaling.ChainPotency(0, 0.15f, 0.4f), Tolerance);
        }

        [Test]
        public void ChainPotency_DropsByFalloffEachBounce()
        {
            Assert.AreEqual(0.85f, AbilityScaling.ChainPotency(1, 0.15f, 0.4f), Tolerance);
            Assert.AreEqual(0.55f, AbilityScaling.ChainPotency(3, 0.15f, 0.4f), Tolerance);
        }

        [Test]
        public void ChainPotency_NeverBelowMinimum()
        {
            Assert.AreEqual(0.4f, AbilityScaling.ChainPotency(10, 0.15f, 0.4f), Tolerance);
        }

        [Test]
        public void ChainPotency_NegativeFalloff_DoesNotIncreaseDamage()
        {
            Assert.AreEqual(1f, AbilityScaling.ChainPotency(4, -0.5f, 0f), Tolerance);
        }

        [Test]
        public void MissingHealthMultiplier_FullHealth_NoBonus()
        {
            Assert.AreEqual(1f, AbilityScaling.MissingHealthMultiplier(1f, 1f), Tolerance);
        }

        [Test]
        public void MissingHealthMultiplier_ScalesWithMissingHealth()
        {
            Assert.AreEqual(1.5f, AbilityScaling.MissingHealthMultiplier(0.5f, 1f), Tolerance);
            Assert.AreEqual(2f, AbilityScaling.MissingHealthMultiplier(0f, 1f), Tolerance);
        }

        [Test]
        public void MissingHealthMultiplier_ClampsOutOfRangeRatio()
        {
            Assert.AreEqual(1f, AbilityScaling.MissingHealthMultiplier(1.5f, 1f), Tolerance);
            Assert.AreEqual(2f, AbilityScaling.MissingHealthMultiplier(-1f, 1f), Tolerance);
        }
    }
}
