using ArkhamHorror.Mechanics.Consequences;
using NUnit.Framework;

namespace ArkhamHorror.Mechanics.Tests
{
    public sealed class InsightPoolTests
    {
        [Test]
        public void SessionRefill_RestoresCurrentToLimit()
        {
            var insight = new InsightPool(3, 1);

            insight.RefillForSession();

            Assert.That(insight.Current, Is.EqualTo(3));
        }

        [Test]
        public void SpendingAndGaining_AreBoundedByCurrentAndLimit()
        {
            var insight = new InsightPool(3, 2);

            Assert.That(insight.TrySpend(1), Is.True);
            Assert.That(insight.TrySpend(2), Is.False);
            insight.Gain(100);

            Assert.That(insight.Current, Is.EqualTo(3));
        }

        [Test]
        public void LimitIncrease_IsCappedAtTen()
        {
            var insight = new InsightPool(9, 0);

            insight.IncreaseLimit(5);

            Assert.That(insight.Limit, Is.EqualTo(InsightPool.MaximumLimit));
            Assert.That(insight.Current, Is.Zero);
        }

        [Test]
        public void DeathAvoidance_ReducesLimitByTwoAndCapsCurrent()
        {
            var insight = new InsightPool(4, 4);

            Assert.That(insight.TrySacrificeLimitToAvoidRemoval(), Is.True);

            Assert.That(insight.Limit, Is.EqualTo(2));
            Assert.That(insight.Current, Is.EqualTo(2));
        }

        [Test]
        public void DeathAvoidance_FailsWhenLimitIsBelowTwo()
        {
            var insight = new InsightPool(1, 1);

            Assert.That(insight.TrySacrificeLimitToAvoidRemoval(), Is.False);
            Assert.That(insight.Limit, Is.EqualTo(1));
            Assert.That(insight.Current, Is.EqualTo(1));
        }
    }
}
