using ArkhamHorror.Mechanics.Consequences;
using ArkhamHorror.Mechanics.DynamicPool;
using NUnit.Framework;

namespace ArkhamHorror.Mechanics.Tests
{
    public sealed class TraumaTests
    {
        [TestCase(1, TraumaKind.SubtleStrangeness)]
        [TestCase(2, TraumaKind.SubtleStrangeness)]
        [TestCase(3, TraumaKind.Shocked)]
        [TestCase(4, TraumaKind.Stunned)]
        [TestCase(5, TraumaKind.OvercomeByHorror)]
        [TestCase(7, TraumaKind.OvercomeByHorror)]
        [TestCase(8, TraumaKind.MindUndone)]
        [TestCase(10, TraumaKind.MindUndone)]
        [TestCase(11, TraumaKind.LostForever)]
        [TestCase(20, TraumaKind.LostForever)]
        public void TableResult_MapsEveryBoundary(int total, TraumaKind expected)
        {
            TraumaResolution result = TraumaGenerator.FromRoll(
                naturalRoll: 1,
                horrorOnesModifier: total - 1,
                sessionModifier: 0);

            Assert.That(result.Total, Is.EqualTo(total));
            Assert.That(result.Kind, Is.EqualTo(expected));
        }

        [Test]
        public void Shocked_DiscardsOneRegularDieBeforeHorrorDice()
        {
            var state = new TraumaState();
            var pool = new DicePool(3, 3, regularDice: 1, horrorDice: 2, horrorLimit: 2);
            var insight = new InsightPool(1, 1);
            TraumaResolution result = TraumaGenerator.FromRoll(3, 0, 0);

            TraumaApplication applied = state.Apply(result, pool, insight);

            Assert.That(applied.PoolDiceDiscarded, Is.EqualTo(1));
            Assert.That(pool.RegularDice, Is.Zero);
            Assert.That(pool.HorrorDice, Is.EqualTo(2));
            Assert.That(state.SessionRollModifier, Is.Zero);
        }

        [Test]
        public void ShockedWithEmptyPool_IncreasesFutureSessionRolls()
        {
            var state = new TraumaState();
            var pool = new DicePool(1, 1, 0);
            var insight = new InsightPool(1, 1);

            state.Apply(TraumaGenerator.FromRoll(3, 0, 0), pool, insight);

            Assert.That(state.SessionRollModifier, Is.EqualTo(1));
        }

        [Test]
        public void Stunned_DiscardsTheEntirePool()
        {
            var state = new TraumaState();
            var pool = new DicePool(3, 3, regularDice: 1, horrorDice: 2, horrorLimit: 2);
            var insight = new InsightPool(1, 1);

            TraumaApplication applied = state.Apply(TraumaGenerator.FromRoll(4, 0, 0), pool, insight);

            Assert.That(applied.PoolDiceDiscarded, Is.EqualTo(3));
            Assert.That(pool.AvailableDice, Is.Zero);
            Assert.That(state.SessionRollModifier, Is.Zero);
        }

        [Test]
        public void OvercomeByHorror_InsightPreventsPersonalityTrigger()
        {
            var state = new TraumaState();
            var pool = new DicePool(1);
            var insight = new InsightPool(2, 2);

            TraumaApplication applied = state.Apply(
                TraumaGenerator.FromRoll(5, 0, 0),
                pool,
                insight,
                TraumaChoice.SpendInsight);

            Assert.That(applied.RequestedChoiceHonored, Is.True);
            Assert.That(applied.InsightSpent, Is.EqualTo(1));
            Assert.That(applied.TriggersNegativePersonality, Is.False);
            Assert.That(insight.Current, Is.EqualTo(1));
        }

        [Test]
        public void OvercomeByHorror_InsufficientInsightTriggersPersonality()
        {
            var state = new TraumaState();
            var pool = new DicePool(1);
            var insight = new InsightPool(1, 0);

            TraumaApplication applied = state.Apply(
                TraumaGenerator.FromRoll(5, 0, 0),
                pool,
                insight,
                TraumaChoice.SpendInsight);

            Assert.That(applied.RequestedChoiceHonored, Is.False);
            Assert.That(applied.TriggersNegativePersonality, Is.True);
        }

        [Test]
        public void MindUndone_AlwaysEscalatesSessionAndExtendsAcceptedPersonalityEffect()
        {
            var state = new TraumaState();
            var pool = new DicePool(1);
            var insight = new InsightPool(2, 2);

            TraumaApplication applied = state.Apply(
                TraumaGenerator.FromRoll(6, horrorOnesModifier: 2, sessionModifier: 0),
                pool,
                insight);

            Assert.That(applied.TriggersNegativePersonality, Is.True);
            Assert.That(applied.ExtendsNegativePersonalityDuration, Is.True);
            Assert.That(state.SessionRollModifier, Is.EqualTo(1));
            Assert.That(insight.Current, Is.EqualTo(2));
        }

        [Test]
        public void MindUndone_InsightPreventsPersonalityButNotSessionEscalation()
        {
            var state = new TraumaState();
            var pool = new DicePool(1);
            var insight = new InsightPool(2, 2);

            TraumaApplication applied = state.Apply(
                TraumaGenerator.FromRoll(6, horrorOnesModifier: 2, sessionModifier: 0),
                pool,
                insight,
                TraumaChoice.SpendInsight);

            Assert.That(applied.TriggersNegativePersonality, Is.False);
            Assert.That(applied.ExtendsNegativePersonalityDuration, Is.False);
            Assert.That(applied.InsightSpent, Is.EqualTo(2));
            Assert.That(state.SessionRollModifier, Is.EqualTo(1));
        }

        [Test]
        public void LostForever_CanBeAvoidedByPermanentlyReducingInsightLimit()
        {
            var state = new TraumaState();
            var pool = new DicePool(1);
            var insight = new InsightPool(3, 3);

            TraumaApplication applied = state.Apply(
                TraumaGenerator.FromRoll(6, horrorOnesModifier: 5, sessionModifier: 0),
                pool,
                insight,
                TraumaChoice.SacrificeInsightLimit);

            Assert.That(applied.RemovalAvoided, Is.True);
            Assert.That(state.IsLostForever, Is.False);
            Assert.That(insight.Limit, Is.EqualTo(1));
            Assert.That(insight.Current, Is.EqualTo(1));
        }

        [Test]
        public void LostForever_RemovesCharacterWhenInsightLimitCannotBeSacrificed()
        {
            var state = new TraumaState();
            var pool = new DicePool(1);
            var insight = new InsightPool(1, 1);

            TraumaApplication applied = state.Apply(
                TraumaGenerator.FromRoll(6, horrorOnesModifier: 5, sessionModifier: 0),
                pool,
                insight,
                TraumaChoice.SacrificeInsightLimit);

            Assert.That(applied.RequestedChoiceHonored, Is.False);
            Assert.That(state.IsLostForever, Is.True);
        }

        [Test]
        public void SessionModifierMustMatchAndResetsAtSessionEnd()
        {
            var state = new TraumaState();
            var emptyPool = new DicePool(1, 1, 0);
            var insight = new InsightPool(1, 1);
            state.Apply(TraumaGenerator.FromRoll(3, 0, 0), emptyPool, insight);

            TraumaResolution stale = TraumaGenerator.FromRoll(2, 0, 0);
            Assert.That(() => state.Apply(stale, emptyPool, insight), Throws.InvalidOperationException);

            state.EndSession();
            Assert.That(state.SessionRollModifier, Is.Zero);
        }

        [TestCase(PersonalityEffectDuration.UntilEndOfNextTurn, PersonalityEffectDuration.UntilEndOfScene)]
        [TestCase(PersonalityEffectDuration.UntilEndOfScene, PersonalityEffectDuration.UntilEndOfSession)]
        [TestCase(PersonalityEffectDuration.UntilEndOfSession, PersonalityEffectDuration.UntilEndOfSession)]
        public void MindUndone_ExtendsPersonalityEffectDuration(
            PersonalityEffectDuration normal,
            PersonalityEffectDuration expected)
        {
            Assert.That(TraumaDurationRules.ExtendForMindUndone(normal), Is.EqualTo(expected));
        }
    }
}
