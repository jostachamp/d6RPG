using ArkhamHorror.Mechanics.DynamicPool;
using NUnit.Framework;

namespace ArkhamHorror.Mechanics.Tests
{
    public sealed class RollConditionTests
    {
        [Test]
        public void Advantage_AddsDieAndRemovesLowestResult()
        {
            var pool = new DicePool(1);
            var resolver = new ComplexActionResolver(new SequenceDieRoller(2, 6));

            ComplexActionResult result = resolver.Perform(
                pool,
                new DiceSelection(1, 0),
                SkillLevel.Bad,
                rollCondition: RollCondition.Advantage);

            Assert.That(result.AllRolls.Count, Is.EqualTo(2));
            Assert.That(result.DieResults, Is.EqualTo(new[] { 6 }));
            Assert.That(result.Succeeded, Is.True);
        }

        [Test]
        public void Disadvantage_AddsDieAndRemovesHighestResult()
        {
            var pool = new DicePool(1);
            var resolver = new ComplexActionResolver(new SequenceDieRoller(6, 2));

            ComplexActionResult result = resolver.Perform(
                pool,
                new DiceSelection(1, 0),
                SkillLevel.Bad,
                rollCondition: RollCondition.Disadvantage);

            Assert.That(result.DieResults, Is.EqualTo(new[] { 2 }));
            Assert.That(result.Succeeded, Is.False);
        }

        [Test]
        public void AdvantageAndDisadvantage_RemoveLowestAndHighest()
        {
            var pool = new DicePool(1);
            var resolver = new ComplexActionResolver(new SequenceDieRoller(4, 1, 6));

            ComplexActionResult result = resolver.Perform(
                pool,
                new DiceSelection(1, 0),
                SkillLevel.Good,
                rollCondition: RollCondition.Advantage | RollCondition.Disadvantage);

            Assert.That(result.AllRolls.Count, Is.EqualTo(3));
            Assert.That(result.DieResults, Is.EqualTo(new[] { 4 }));
            Assert.That(result.Succeeded, Is.True);
        }
    }
}
