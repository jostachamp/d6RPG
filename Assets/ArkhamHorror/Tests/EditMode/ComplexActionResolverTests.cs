using ArkhamHorror.Mechanics.DynamicPool;
using NUnit.Framework;

namespace ArkhamHorror.Mechanics.Tests
{
    public sealed class ComplexActionResolverTests
    {
        [TestCase(SkillLevel.Bad, 6)]
        [TestCase(SkillLevel.Normal, 5)]
        [TestCase(SkillLevel.Good, 4)]
        [TestCase(SkillLevel.Amazing, 3)]
        [TestCase(SkillLevel.Phenomenal, 2)]
        public void SkillLevel_UsesItsMinimumSuccessfulResult(SkillLevel skillLevel, int successfulResult)
        {
            ComplexActionResult success = ComplexActionResolver.Evaluate(new[] { successfulResult }, skillLevel);

            Assert.That(success.Succeeded, Is.True);

            if (successfulResult > 2)
            {
                ComplexActionResult failure = ComplexActionResolver.Evaluate(new[] { successfulResult - 1 }, skillLevel);
                Assert.That(failure.Succeeded, Is.False);
            }
        }

        [Test]
        public void NaturalOne_AlwaysFailsDespitePositiveModifier()
        {
            ComplexActionResult result = ComplexActionResolver.Evaluate(
                new[] { 1 },
                SkillLevel.Phenomenal,
                dieResultModifier: 100);

            Assert.That(result.Succeeded, Is.False);
        }

        [Test]
        public void NaturalSix_AlwaysSucceedsDespiteNegativeModifier()
        {
            ComplexActionResult result = ComplexActionResolver.Evaluate(
                new[] { 6 },
                SkillLevel.Bad,
                dieResultModifier: -100);

            Assert.That(result.Succeeded, Is.True);
        }

        [TestCase(ActionDifficulty.Standard, 1, true)]
        [TestCase(ActionDifficulty.Difficult, 1, false)]
        [TestCase(ActionDifficulty.Difficult, 2, true)]
        [TestCase(ActionDifficulty.VeryDifficult, 2, false)]
        [TestCase(ActionDifficulty.VeryDifficult, 3, true)]
        public void Difficulty_RequiresTheCorrectNumberOfSuccesses(
            ActionDifficulty difficulty,
            int successCount,
            bool expected)
        {
            var rolls = new int[successCount];
            for (int index = 0; index < rolls.Length; index++)
            {
                rolls[index] = 6;
            }

            ComplexActionResult result = ComplexActionResolver.Evaluate(rolls, SkillLevel.Bad, difficulty);

            Assert.That(result.Succeeded, Is.EqualTo(expected));
        }

        [Test]
        public void Perform_SpendsOnlyPoolDiceAndRollsAdditionalHandDice()
        {
            var pool = new DicePool(6);
            var resolver = new ComplexActionResolver(new SequenceDieRoller(2, 4, 6));

            ComplexActionResult result = resolver.Perform(
                pool,
                diceFromPool: 2,
                skillLevel: SkillLevel.Good,
                additionalHandDice: 1);

            Assert.That(pool.AvailableDice, Is.EqualTo(4));
            Assert.That(result.DieResults, Is.EqualTo(new[] { 2, 4, 6 }));
            Assert.That(result.SuccessCount, Is.EqualTo(2));
            Assert.That(result.Succeeded, Is.True);
        }

        [Test]
        public void Perform_WithInsufficientPoolDice_DoesNotMutatePoolOrRoll()
        {
            var pool = new DicePool(2);
            var resolver = new ComplexActionResolver(new SequenceDieRoller());

            Assert.That(
                () => resolver.Perform(pool, 3, SkillLevel.Normal),
                Throws.InvalidOperationException);
            Assert.That(pool.AvailableDice, Is.EqualTo(2));
        }
    }
}
