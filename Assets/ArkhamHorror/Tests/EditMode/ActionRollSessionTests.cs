using ArkhamHorror.Mechanics.DynamicPool;
using NUnit.Framework;

namespace ArkhamHorror.Mechanics.Tests
{
    public sealed class ActionRollSessionTests
    {
        [Test]
        public void Reroll_ReplacesCurrentResultAndPreservesAuditHistory()
        {
            var pool = new DicePool(1);
            var resolver = new ComplexActionResolver(new SequenceDieRoller(2, 6));
            ActionRollSession session = resolver.Begin(pool, new DiceSelection(1, 0), SkillLevel.Bad, rerollsAllowed: 1);

            session.Reroll(0);
            ComplexActionResult result = session.Complete();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.AllRolls[0].NaturalResult, Is.EqualTo(6));
            Assert.That(result.RollEvents.Count, Is.EqualTo(2));
            Assert.That(result.RollEvents[0].NaturalResult, Is.EqualTo(2));
            Assert.That(result.RollEvents[0].IsReroll, Is.False);
            Assert.That(result.RollEvents[1].NaturalResult, Is.EqualTo(6));
            Assert.That(result.RollEvents[1].IsReroll, Is.True);
        }

        [Test]
        public void HorrorOne_CannotBeRerolled()
        {
            var pool = new DicePool(1, 1, regularDice: 0, horrorDice: 1, horrorLimit: 1);
            var resolver = new ComplexActionResolver(new SequenceDieRoller(1));
            ActionRollSession session = resolver.Begin(pool, new DiceSelection(0, 1), SkillLevel.Bad, rerollsAllowed: 1);

            Assert.That(() => session.Reroll(0), Throws.InvalidOperationException);

            ComplexActionResult result = session.Complete();
            Assert.That(result.TriggersTrauma, Is.True);
            Assert.That(result.HorrorOnesRolled, Is.EqualTo(1));
        }

        [Test]
        public void HorrorDie_RerolledIntoOneTriggersTraumaAndBecomesLocked()
        {
            var pool = new DicePool(1, 1, regularDice: 0, horrorDice: 1, horrorLimit: 1);
            var resolver = new ComplexActionResolver(new SequenceDieRoller(2, 1));
            ActionRollSession session = resolver.Begin(pool, new DiceSelection(0, 1), SkillLevel.Bad, rerollsAllowed: 2);

            session.Reroll(0);

            Assert.That(() => session.Reroll(0), Throws.InvalidOperationException);
            ComplexActionResult result = session.Complete();
            Assert.That(result.HorrorOnesRolled, Is.EqualTo(1));
            Assert.That(result.RollEvents.Count, Is.EqualTo(2));
        }

        [Test]
        public void Advantage_RemovesLowestResultAfterRerolls()
        {
            var pool = new DicePool(1);
            var resolver = new ComplexActionResolver(new SequenceDieRoller(1, 4, 6));
            ActionRollSession session = resolver.Begin(
                pool,
                new DiceSelection(1, 0),
                SkillLevel.Bad,
                rollCondition: RollCondition.Advantage,
                rerollsAllowed: 1);

            session.Reroll(0);
            ComplexActionResult result = session.Complete();

            Assert.That(result.DieResults, Is.EqualTo(new[] { 6 }));
            Assert.That(result.Succeeded, Is.True);
        }

        [Test]
        public void SpecialResultRemoval_HappensAfterRollConditions()
        {
            var pool = new DicePool(2);
            var resolver = new ComplexActionResolver(new SequenceDieRoller(6, 5));
            ActionRollSession session = resolver.Begin(
                pool,
                new DiceSelection(2, 0),
                SkillLevel.Normal,
                specialResultRemovalsAllowed: 1);

            session.ApplyRollCondition();
            session.RemoveResult(0);
            ComplexActionResult result = session.Complete();

            Assert.That(result.DieResults, Is.EqualTo(new[] { 5 }));
            Assert.That(result.SuccessCount, Is.EqualTo(1));
        }

        [Test]
        public void Reroll_AfterResultRemovalBeginsIsRejected()
        {
            var pool = new DicePool(1);
            var resolver = new ComplexActionResolver(new SequenceDieRoller(4));
            ActionRollSession session = resolver.Begin(pool, new DiceSelection(1, 0), SkillLevel.Good, rerollsAllowed: 1);

            session.ApplyRollCondition();

            Assert.That(() => session.Reroll(0), Throws.InvalidOperationException);
        }

        [Test]
        public void Reroll_RequiresAnExplicitGrant()
        {
            var pool = new DicePool(1);
            var resolver = new ComplexActionResolver(new SequenceDieRoller(4));
            ActionRollSession session = resolver.Begin(pool, new DiceSelection(1, 0), SkillLevel.Good);

            Assert.That(() => session.Reroll(0), Throws.InvalidOperationException);
            Assert.That(session.RemainingRerolls, Is.Zero);
            Assert.That(session.CurrentRolls[0].NaturalResult, Is.EqualTo(4));
        }

        [Test]
        public void PreRollRemoval_RemovesFromHandButStillSpendsCommittedPoolDice()
        {
            var pool = new DicePool(2);
            var resolver = new ComplexActionResolver(new SequenceDieRoller(5));

            ActionRollSession session = resolver.Begin(
                pool,
                new DiceSelection(2, 0),
                SkillLevel.Normal,
                preRollDiceRemoved: new DiceSelection(1, 0));
            ComplexActionResult result = session.Complete();

            Assert.That(pool.AvailableDice, Is.Zero);
            Assert.That(result.AllRolls.Count, Is.EqualTo(1));
            Assert.That(result.Succeeded, Is.True);
        }

        [Test]
        public void HorrorDieRemovedBeforeRoll_DoesNotGenerateTrauma()
        {
            var pool = new DicePool(2, 2, regularDice: 1, horrorDice: 1, horrorLimit: 1);
            var resolver = new ComplexActionResolver(new SequenceDieRoller(5));

            ActionRollSession session = resolver.Begin(
                pool,
                new DiceSelection(1, 1),
                SkillLevel.Normal,
                preRollDiceRemoved: new DiceSelection(0, 1));
            ComplexActionResult result = session.Complete();

            Assert.That(pool.AvailableDice, Is.Zero);
            Assert.That(result.AllRolls.Count, Is.EqualTo(1));
            Assert.That(result.AllRolls[0].Kind, Is.EqualTo(ArkhamHorror.Mechanics.Dice.DieKind.Regular));
            Assert.That(result.TriggersTrauma, Is.False);
        }
    }
}
