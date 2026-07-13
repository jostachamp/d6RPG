using ArkhamHorror.Mechanics.DynamicPool;
using NUnit.Framework;

namespace ArkhamHorror.Mechanics.Tests
{
    public sealed class ReactionResolverTests
    {
        [Test]
        public void Reaction_SpendsExactlyOnePoolDie()
        {
            var pool = new DicePool(3);
            var resolver = new ComplexActionResolver(new SequenceDieRoller(5));

            ComplexActionResult result = resolver.PerformReaction(
                pool,
                new DiceSelection(1, 0),
                SkillLevel.Normal);

            Assert.That(pool.AvailableDice, Is.EqualTo(2));
            Assert.That(result.ActionKind, Is.EqualTo(ActionKind.Reaction));
            Assert.That(result.Succeeded, Is.True);
        }

        [Test]
        public void Reaction_CannotSpendMoreThanOnePoolDieAndDoesNotMutatePool()
        {
            var pool = new DicePool(3);
            var resolver = new ComplexActionResolver(new SequenceDieRoller());

            Assert.That(
                () => resolver.PerformReaction(pool, new DiceSelection(2, 0), SkillLevel.Normal),
                Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(pool.AvailableDice, Is.EqualTo(3));
        }

        [Test]
        public void Reaction_MayUseAdvantageWhileSpendingOnePoolDie()
        {
            var pool = new DicePool(2);
            var resolver = new ComplexActionResolver(new SequenceDieRoller(2, 6));

            ComplexActionResult result = resolver.PerformReaction(
                pool,
                new DiceSelection(1, 0),
                SkillLevel.Bad,
                rollCondition: RollCondition.Advantage);

            Assert.That(pool.AvailableDice, Is.EqualTo(1));
            Assert.That(result.AllRolls.Count, Is.EqualTo(2));
            Assert.That(result.DieResults, Is.EqualTo(new[] { 6 }));
        }

        [Test]
        public void HorrorDieReaction_CanTriggerTrauma()
        {
            var pool = new DicePool(1, 1, regularDice: 0, horrorDice: 1, horrorLimit: 1);
            var resolver = new ComplexActionResolver(new SequenceDieRoller(1));

            ComplexActionResult result = resolver.PerformReaction(
                pool,
                new DiceSelection(0, 1),
                SkillLevel.Bad);

            Assert.That(result.ActionKind, Is.EqualTo(ActionKind.Reaction));
            Assert.That(result.TriggersTrauma, Is.True);
        }
    }
}
