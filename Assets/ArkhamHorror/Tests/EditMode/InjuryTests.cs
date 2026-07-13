using ArkhamHorror.Mechanics.Consequences;
using ArkhamHorror.Mechanics.DynamicPool;
using NUnit.Framework;

namespace ArkhamHorror.Mechanics.Tests
{
    public sealed class InjuryTests
    {
        [TestCase(1, InjuryKind.HeavyBlow)]
        [TestCase(2, InjuryKind.Slowed)]
        [TestCase(3, InjuryKind.NastyCut)]
        [TestCase(4, InjuryKind.Concussed)]
        [TestCase(5, InjuryKind.InjuredArm)]
        [TestCase(6, InjuryKind.InjuredLeg)]
        [TestCase(7, InjuryKind.LossOfSense)]
        [TestCase(8, InjuryKind.SeverelyInjured)]
        [TestCase(9, InjuryKind.Comatose)]
        [TestCase(10, InjuryKind.Dire)]
        [TestCase(11, InjuryKind.Dead)]
        [TestCase(20, InjuryKind.Dead)]
        public void TableResult_MapsEveryBoundary(int total, InjuryKind expected)
        {
            Assert.That(InjuryCatalog.ResolveTableResult(total), Is.EqualTo(expected));
        }

        [Test]
        public void Generation_AddsEveryExistingInjuryAndExternalModifier()
        {
            InjuryResolution result = InjuryGenerator.FromBaseRoll(4, existingInjuries: 3, externalModifier: 1);

            Assert.That(result.Total, Is.EqualTo(8));
            Assert.That(result.Kind, Is.EqualTo(InjuryKind.SeverelyInjured));
            Assert.That(result.ExistingInjuryModifier, Is.EqualTo(3));
        }

        [Test]
        public void DuplicateEffectsDoNotStackButEveryInstanceMustBeHealed()
        {
            var state = new InjuryState();
            var pool = new DicePool(6);
            InjuryInstance first = state.Apply(InjuryGenerator.FromBaseRoll(4, 0), pool);
            InjuryInstance second = state.Apply(InjuryGenerator.FromBaseRoll(3, 1), pool);

            Assert.That(first.Kind, Is.EqualTo(InjuryKind.Concussed));
            Assert.That(second.Kind, Is.EqualTo(InjuryKind.Concussed));
            Assert.That(state.CountOf(InjuryKind.Concussed), Is.EqualTo(2));
            Assert.That(state.HasActiveEffect(InjuryKind.Concussed), Is.True);

            Assert.That(state.TryHeal(first.Id, 1), Is.True);
            Assert.That(state.HasActiveEffect(InjuryKind.Concussed), Is.True);
            Assert.That(state.TryHeal(second.Id, 1), Is.True);
            Assert.That(state.HasActiveEffect(InjuryKind.Concussed), Is.False);
        }

        [Test]
        public void LossOfSense_RequiresTheAffectedSense()
        {
            var state = new InjuryState();
            var pool = new DicePool(6);
            InjuryResolution result = InjuryGenerator.FromBaseRoll(7, 0);

            Assert.That(() => state.Apply(result, pool), Throws.ArgumentException);

            InjuryInstance injury = state.Apply(result, pool, SenseKind.Hearing);
            Assert.That(injury.LostSense, Is.EqualTo(SenseKind.Hearing));
        }

        [Test]
        public void SevereInjury_RequiresTwoSuccessesAndTwoWeeks()
        {
            var state = new InjuryState();
            var pool = new DicePool(6);
            InjuryInstance injury = state.Apply(InjuryGenerator.FromBaseRoll(8, 0), pool);

            Assert.That(state.TryHeal(injury.Id, 1), Is.False);
            Assert.That(state.TryHealNaturally(injury.Id, 1), Is.False);
            Assert.That(state.TryHealNaturally(injury.Id, 2), Is.True);
        }

        [Test]
        public void DireInjury_CannotHealNaturallyAndRequiresThreeSuccesses()
        {
            var state = new InjuryState();
            var pool = new DicePool(6);
            InjuryInstance injury = state.Apply(InjuryGenerator.FromBaseRoll(10, 0), pool);

            Assert.That(pool.Limit, Is.Zero);
            Assert.That(state.TryHealNaturally(injury.Id, 100), Is.False);
            Assert.That(state.TryHeal(injury.Id, 2), Is.False);
            Assert.That(state.TryHeal(injury.Id, 3), Is.True);
        }

        [Test]
        public void ThirdMatchingUniqueInjury_IsFatalUnlessAvoided()
        {
            var deadState = new InjuryState();
            deadState.SufferUnique(InjuryKind.Burned);
            deadState.SufferUnique(InjuryKind.Burned);
            deadState.SufferUnique(InjuryKind.Burned);
            Assert.That(deadState.IsDead, Is.True);

            var survivingState = new InjuryState();
            survivingState.SufferUnique(InjuryKind.Choking);
            survivingState.SufferUnique(InjuryKind.Choking);
            survivingState.SufferUnique(InjuryKind.Choking, fatalOutcomeAvoided: true);
            Assert.That(survivingState.IsDead, Is.False);
            Assert.That(survivingState.CountOf(InjuryKind.Choking), Is.EqualTo(2));
        }

        [Test]
        public void FatalTableResult_CanBeInterceptedByInsightLimitSacrifice()
        {
            var state = new InjuryState();
            var pool = new DicePool(6);
            var insight = new InsightPool(3, 3);
            InjuryResolution fatal = InjuryGenerator.FromBaseRoll(6, existingInjuries: 0, externalModifier: 5);

            bool avoided = insight.TrySacrificeLimitToAvoidRemoval();
            state.Apply(fatal, pool, fatalOutcomeAvoided: avoided);

            Assert.That(avoided, Is.True);
            Assert.That(insight.Limit, Is.EqualTo(1));
            Assert.That(state.IsDead, Is.False);
        }

        [Test]
        public void StaleGeneratedResult_IsRejectedBeforeMutation()
        {
            var state = new InjuryState();
            var pool = new DicePool(6);
            InjuryResolution stale = InjuryGenerator.FromBaseRoll(2, 0);
            state.Apply(InjuryGenerator.FromBaseRoll(1, 0), pool);

            Assert.That(() => state.Apply(stale, pool), Throws.InvalidOperationException);
            Assert.That(state.Count, Is.EqualTo(1));
        }

        [Test]
        public void HeavyBlowEffectEndsAfterNextTurnButInjuryRemains()
        {
            var state = new InjuryState();
            var pool = new DicePool(6);
            InjuryInstance injury = state.Apply(InjuryGenerator.FromBaseRoll(1, 0), pool);

            Assert.That(state.HasActiveEffect(InjuryKind.HeavyBlow), Is.True);
            state.ResolveHeavyBlowEffectsAfterNextTurn();

            Assert.That(state.HasActiveEffect(InjuryKind.HeavyBlow), Is.False);
            Assert.That(state.CountOf(InjuryKind.HeavyBlow), Is.EqualTo(1));
            Assert.That(state.TryHeal(injury.Id, 1), Is.True);
        }

        [Test]
        public void UntreatedDireInjuryKillsAfterDeadlineButTreatmentCancelsDeadline()
        {
            var deadState = new InjuryState();
            var deadPool = new DicePool(6);
            InjuryInstance untreated = deadState.Apply(InjuryGenerator.FromBaseRoll(10, 0), deadPool);
            Assert.That(deadState.ExpireDireTreatmentDeadline(untreated.Id), Is.True);
            Assert.That(deadState.IsDead, Is.True);

            var treatedState = new InjuryState();
            var treatedPool = new DicePool(6);
            InjuryInstance treated = treatedState.Apply(InjuryGenerator.FromBaseRoll(10, 0), treatedPool);
            Assert.That(treatedState.MarkDireInjuryTreated(treated.Id), Is.True);
            Assert.That(treatedState.ExpireDireTreatmentDeadline(treated.Id), Is.False);
            Assert.That(treatedState.IsDead, Is.False);
        }
    }
}
