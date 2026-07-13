using System;
using System.Collections.Generic;
using ArkhamHorror.Mechanics.Consequences;
using ArkhamHorror.Mechanics.Dice;
using ArkhamHorror.Mechanics.DynamicPool;
using ArkhamHorror.Mechanics.Persistence;
using ArkhamHorror.Mechanics.Scenes;
using NUnit.Framework;

namespace ArkhamHorror.Mechanics.Tests
{
    public sealed class PersistenceTests
    {
        [Test]
        public void DeterministicRoller_RestoresExactContinuationFromCapturedState()
        {
            var first = new DeterministicDieRoller(12345, 9);
            first.RollD6();
            RandomState captured = first.CaptureState();
            var restored = new DeterministicDieRoller(captured);

            for (int index = 0; index < 20; index++)
                Assert.That(restored.RollD6(), Is.EqualTo(first.RollD6()));
        }

        [Test]
        public void DeterministicRoller_SameSeedProducesSameSequence()
        {
            var first = new DeterministicDieRoller(77);
            var second = new DeterministicDieRoller(77);

            for (int index = 0; index < 100; index++)
                Assert.That(second.RollD6(), Is.EqualTo(first.RollD6()));
        }

        [Test]
        public void Snapshot_RoundTripPreservesAllSharedMutableState()
        {
            MechanicsState original = StateWithDetailedActor();
            MechanicsSnapshot snapshot = MechanicsSnapshot.Capture(original);
            MechanicsState restored = snapshot.Restore();

            Assert.That(MechanicsSnapshot.Capture(restored).ToCanonicalText(), Is.EqualTo(snapshot.ToCanonicalText()));
        }

        [Test]
        public void Snapshot_CanonicalOrderDoesNotDependOnActorInsertionOrder()
        {
            var first = new MechanicsState();
            first.AddActor(Actor("b")); first.AddActor(Actor("a"));
            var second = new MechanicsState();
            second.AddActor(Actor("a")); second.AddActor(Actor("b"));

            Assert.That(MechanicsSnapshot.Capture(first).ToCanonicalText(),
                Is.EqualTo(MechanicsSnapshot.Capture(second).ToCanonicalText()));
        }

        [Test]
        public void Snapshot_RejectsInvalidAndDuplicateData()
        {
            ActorSnapshot actor = MechanicsSnapshot.Capture(StateWithActor()).Actors[0];

            Assert.That(() => new MechanicsSnapshot(1, new[] { actor, actor }), Throws.ArgumentException);
            Assert.That(() => new InjurySnapshot(1, InjuryKind.Concussed, SenseKind.Sight, false, false),
                Throws.ArgumentException);
            Assert.That(() => new InjurySnapshot(1, InjuryKind.Concussed, null, true, false),
                Throws.ArgumentException);
        }

        [Test]
        public void RecordedDecisionProvider_RequiresMatchingRequestAndLegalChoice()
        {
            var provider = new RecordedDecisionProvider(new[] { new DecisionRecord("sense", "Sight") });
            var request = new DecisionRequest<SenseKind>("sense", new[] { SenseKind.Sight, SenseKind.Hearing });

            Assert.That(provider.Choose(request), Is.EqualTo(SenseKind.Sight));
            Assert.That(provider.Remaining, Is.Zero);
            Assert.That(() => provider.Choose(request), Throws.InvalidOperationException);
        }

        [Test]
        public void Replay_ProducesIdenticalStateEventsAndRandomContinuation()
        {
            MechanicsSnapshot initial = MechanicsSnapshot.Capture(StateWithActor());
            RandomState random = new DeterministicDieRoller(90410, 3).CaptureState();
            IReadOnlyList<MechanicsCommand> commands = GoldenCommands();

            ReplayResult first = ReplayEngine.Run(initial, random, commands);
            ReplayResult second = ReplayEngine.Run(initial, random, commands);

            Assert.That(second.StateFingerprint, Is.EqualTo(first.StateFingerprint));
            Assert.That(second.EventFingerprint, Is.EqualTo(first.EventFingerprint));
            Assert.That(second.RandomState, Is.EqualTo(first.RandomState));
            Assert.That(second.Journal.Events.Count, Is.EqualTo(commands.Count));
        }

        [Test]
        public void CertifiedRecording_VerifiesAndRejectsTamperedExpectation()
        {
            MechanicsSnapshot initial = MechanicsSnapshot.Capture(StateWithActor());
            RandomState random = new DeterministicDieRoller(42).CaptureState();
            IReadOnlyList<MechanicsCommand> commands = GoldenCommands();
            ReplayResult expected = ReplayEngine.Run(initial, random, commands);
            var valid = new ReplayRecording(initial, random, commands,
                expected.StateFingerprint, expected.EventFingerprint);
            var tampered = new ReplayRecording(initial, random, commands,
                "0000000000000000", expected.EventFingerprint);

            Assert.That(() => ReplayEngine.Verify(valid), Throws.Nothing);
            Assert.That(() => ReplayEngine.Verify(tampered), Throws.InvalidOperationException);
        }

        [Test]
        public void MidScenarioSnapshot_ContinuesToSameFinalStateAndRandomPosition()
        {
            MechanicsSnapshot initial = MechanicsSnapshot.Capture(StateWithActor());
            RandomState random = new DeterministicDieRoller(2311).CaptureState();
            var commands = new List<MechanicsCommand>(GoldenCommands());
            ReplayResult uninterrupted = ReplayEngine.Run(initial, random, commands);
            ReplayResult firstHalf = ReplayEngine.Run(initial, random, commands.GetRange(0, 6));

            ReplayResult resumed = ReplayEngine.Run(
                firstHalf.Snapshot, firstHalf.RandomState, commands.GetRange(6, commands.Count - 6));

            Assert.That(resumed.StateFingerprint, Is.EqualTo(uninterrupted.StateFingerprint));
            Assert.That(resumed.RandomState, Is.EqualTo(uninterrupted.RandomState));
        }

        [Test]
        public void Replay_RejectsDuplicateCommandIdsBeforeSecondMutation()
        {
            MechanicsSnapshot initial = MechanicsSnapshot.Capture(StateWithActor());
            RandomState random = new DeterministicDieRoller(1).CaptureState();
            var commands = new MechanicsCommand[]
            {
                new StateChangeCommand("same", "actor", StateChangeKind.SufferDamage, 1),
                new StateChangeCommand("same", "actor", StateChangeKind.SufferDamage, 1)
            };

            Assert.That(() => ReplayEngine.Run(initial, random, commands), Throws.InvalidOperationException);
        }

        [Test]
        public void MigrationRegistry_RequiresAdjacentCompleteMigrationPath()
        {
            MechanicsSnapshot current = MechanicsSnapshot.Capture(StateWithActor());
            var legacy = new MechanicsSnapshot(0, current.Actors);
            var registry = new SnapshotMigrationRegistry();
            Assert.That(() => registry.MigrateToCurrent(legacy), Throws.InvalidOperationException);
            registry.Register(new VersionZeroToOneMigrator());

            MechanicsSnapshot migrated = registry.MigrateToCurrent(legacy);

            Assert.That(migrated.Version, Is.EqualTo(MechanicsSnapshot.CurrentVersion));
            Assert.That(migrated.ToCanonicalText(), Is.EqualTo(current.ToCanonicalText()));
        }

        [Test]
        public void MigrationRegistry_RejectsFutureAndSkippedVersions()
        {
            var registry = new SnapshotMigrationRegistry();
            Assert.That(() => registry.Register(new SkippingMigrator()), Throws.ArgumentException);
            Assert.That(() => registry.MigrateToCurrent(new MechanicsSnapshot(2, new ActorSnapshot[0])),
                Throws.InvalidOperationException);
        }

        [Test]
        public void GoldenCommands_CoverActionsReactionsConsequencesAndRecovery()
        {
            MechanicsSnapshot initial = MechanicsSnapshot.Capture(StateWithActor());
            RandomState random = new DeterministicDieRoller(1987).CaptureState();

            ReplayResult result = ReplayEngine.Run(initial, random, GoldenCommands());
            MechanicsActorState actor = result.Snapshot.Restore().GetActor("actor");

            Assert.That(result.Journal.Events.Count, Is.EqualTo(GoldenCommands().Count));
            Assert.That(actor.Injuries.Count, Is.EqualTo(1));
            Assert.That(actor.DicePool.HorrorLimit, Is.EqualTo(1));
            Assert.That(actor.Insight.Current, Is.EqualTo(3));
        }

        private static MechanicsState StateWithActor()
        {
            var state = new MechanicsState(); state.AddActor(Actor("actor")); return state;
        }

        private static MechanicsActorState Actor(string id) =>
            new MechanicsActorState(id, new DicePool(6), new InsightPool(4, 4));

        private static MechanicsState StateWithDetailedActor()
        {
            var pool = new DicePool(6, 4, regularDice: 1, horrorDice: 2, horrorLimit: 3, pendingStrainInjuries: 2);
            var injuries = new InjuryState();
            InjuryInstance heavy = injuries.Apply(InjuryGenerator.FromBaseRoll(1, 0), pool);
            injuries.ResolveHeavyBlowEffectsAfterNextTurn();
            injuries.Apply(InjuryGenerator.FromBaseRoll(6, 1), pool, SenseKind.Hearing);
            var trauma = new TraumaState();
            trauma.Apply(TraumaGenerator.FromRoll(4, 4, 0), pool, new InsightPool(4, 4));
            var state = new MechanicsState();
            state.AddActor(new MechanicsActorState("detailed", pool, new InsightPool(3, 2), injuries, trauma));
            return state;
        }

        private static IReadOnlyList<MechanicsCommand> GoldenCommands() => new MechanicsCommand[]
        {
            new StateChangeCommand("01-damage", "actor", StateChangeKind.SufferDamage, 2),
            new StateChangeCommand("02-horror", "actor", StateChangeKind.SufferHorror, 2),
            new StateChangeCommand("03-refill", "actor", StateChangeKind.RefillPool),
            new RollActionCommand("04-action", "actor", ActionKind.Complex, new DiceSelection(1, 0), SkillLevel.Normal),
            new RollActionCommand("05-reaction", "actor", ActionKind.Reaction, new DiceSelection(1, 0), SkillLevel.Normal),
            new GenerateInjuryCommand("06-injury", "actor", externalModifier: -100),
            new GenerateTraumaCommand("07-trauma", "actor", horrorOnesRolled: 1, choice: TraumaChoice.Accept),
            new StateChangeCommand("08-heal", "actor", StateChangeKind.HealDamage, 1),
            new StateChangeCommand("09-recover-horror", "actor", StateChangeKind.ReduceHorror, 1),
            new StateChangeCommand("10-spend-insight", "actor", StateChangeKind.SpendInsight, 1),
            new StateChangeCommand("11-gain-insight", "actor", StateChangeKind.GainInsight, 1),
            new StateChangeCommand("12-spend-insight", "actor", StateChangeKind.SpendInsight, 1)
        };

        private sealed class VersionZeroToOneMigrator : IMechanicsSnapshotMigrator
        {
            public int FromVersion => 0;
            public int ToVersion => 1;
            public MechanicsSnapshot Migrate(MechanicsSnapshot snapshot) => new MechanicsSnapshot(1, snapshot.Actors);
        }

        private sealed class SkippingMigrator : IMechanicsSnapshotMigrator
        {
            public int FromVersion => 0;
            public int ToVersion => 2;
            public MechanicsSnapshot Migrate(MechanicsSnapshot snapshot) => snapshot;
        }
    }
}
