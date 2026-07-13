using System;
using ArkhamHorror.Mechanics.DynamicPool;
using ArkhamHorror.Mechanics.Scenes;
using NUnit.Framework;

namespace ArkhamHorror.Mechanics.Tests
{
    public sealed class SceneTests
    {
        [Test]
        public void NarrativeScene_RefillsActorsAtStartAndOnEntry()
        {
            var firstPool = new DicePool(6, 4, 1);
            var scene = new NarrativeScene(new[] { new SceneActor("first", SceneSide.A, firstPool) });
            var latePool = new DicePool(6, 3, 0);

            scene.AddActor(new SceneActor("late", SceneSide.B, latePool));

            Assert.That(firstPool.AvailableDice, Is.EqualTo(4));
            Assert.That(latePool.AvailableDice, Is.EqualTo(3));
        }

        [Test]
        public void NarrativeScene_ExactTaskCanBeAttemptedOnlyOncePerActor()
        {
            var scene = new NarrativeScene(new[] { Actor("actor", SceneSide.A, 3) });

            scene.DeclareComplexAction("actor", "open-door");

            Assert.That(() => scene.DeclareComplexAction("actor", "open-door"), Throws.InvalidOperationException);
            Assert.That(() => scene.DeclareComplexAction("actor", "inspect-door"), Throws.Nothing);
        }

        [Test]
        public void NarrativeSimpleActions_AreFreeExceptAidAnAlly()
        {
            var actor = Actor("actor", SceneSide.A, 3);
            var scene = new NarrativeScene(new[] { actor });

            scene.SpendSimpleAction("actor", SimpleActionKind.General, default(DiceSelection));
            scene.SpendSimpleAction("actor", SimpleActionKind.AidAlly, new DiceSelection(1, 0));

            Assert.That(actor.DicePool.AvailableDice, Is.EqualTo(2));
        }

        [Test]
        public void StructuredScene_RefillsOnlyTheSideWhoseTurnBegins()
        {
            var sideA = Actor("a", SceneSide.A, 1, limit: 4);
            var sideB = Actor("b", SceneSide.B, 1, limit: 4);

            var scene = new StructuredScene(new[] { sideA, sideB }, SceneSide.A);

            Assert.That(sideA.DicePool.AvailableDice, Is.EqualTo(4));
            Assert.That(sideB.DicePool.AvailableDice, Is.EqualTo(1));
            scene.Pass("a");
            scene.EndCurrentSideTurn();
            Assert.That(sideB.DicePool.AvailableDice, Is.EqualTo(4));
        }

        [Test]
        public void StructuredScene_RequiresEveryReadyActorToPassOrExhaustPool()
        {
            var scene = new StructuredScene(
                new[] { Actor("a1", SceneSide.A, 1), Actor("a2", SceneSide.A, 1) },
                SceneSide.A);

            scene.Pass("a1");

            Assert.That(scene.CanEndCurrentSideTurn, Is.False);
            Assert.That(() => scene.EndCurrentSideTurn(), Throws.InvalidOperationException);
            scene.Pass("a2");
            Assert.That(scene.CanEndCurrentSideTurn, Is.True);
        }

        [Test]
        public void StructuredScene_ExactTaskRestrictionResetsOnActorsNextTurn()
        {
            var a = Actor("a", SceneSide.A, 3);
            var b = Actor("b", SceneSide.B, 3);
            var scene = new StructuredScene(new[] { a, b }, SceneSide.A);

            scene.DeclareComplexAction("a", "attack-target");
            Assert.That(() => scene.DeclareComplexAction("a", "attack-target"), Throws.InvalidOperationException);
            scene.Pass("a");
            scene.EndCurrentSideTurn();
            scene.Pass("b");
            scene.EndCurrentSideTurn();

            Assert.That(scene.RoundNumber, Is.EqualTo(2));
            Assert.That(() => scene.DeclareComplexAction("a", "attack-target"), Throws.Nothing);
        }

        [Test]
        public void StructuredSimpleAction_CostsOneExplicitlySelectedDie()
        {
            var actor = new SceneActor("actor", SceneSide.A, new DicePool(3, 3, 2, 1, 1));
            var scene = new StructuredScene(new[] { actor }, SceneSide.A);

            scene.SpendSimpleAction("actor", new DiceSelection(0, 1));

            Assert.That(actor.DicePool.RegularDice, Is.EqualTo(2));
            Assert.That(actor.DicePool.HorrorDice, Is.Zero);
        }

        [Test]
        public void ActorEnteringOpposingTurn_WaitsForOwnTurnToRefill()
        {
            var a = Actor("a", SceneSide.A, 1);
            var scene = new StructuredScene(new[] { a }, SceneSide.A);
            var late = Actor("late", SceneSide.B, 0, limit: 3);

            scene.AddActor(late);
            Assert.That(late.DicePool.AvailableDice, Is.Zero);
            scene.Pass("a");
            scene.EndCurrentSideTurn();
            Assert.That(late.DicePool.AvailableDice, Is.EqualTo(3));
        }

        [Test]
        public void SurpriseRound_RefillsSurprisingSideByOneAndSkipsSurprisedSide()
        {
            var surprising = Actor("surprising", SceneSide.A, 1, limit: 4);
            var surprised = Actor("surprised", SceneSide.B, 1, limit: 4);
            var scene = new StructuredScene(
                new[] { surprising, surprised }, SceneSide.A,
                hasSurpriseRound: true, surprisingSide: SceneSide.A);

            Assert.That(surprising.DicePool.AvailableDice, Is.EqualTo(2));
            scene.Pass("surprising");
            scene.EndCurrentSideTurn();
            Assert.That(surprised.DicePool.AvailableDice, Is.EqualTo(1));
            scene.Pass("surprised");
            scene.EndCurrentSideTurn();
            Assert.That(scene.RoundNumber, Is.EqualTo(2));
            Assert.That(surprising.DicePool.AvailableDice, Is.EqualTo(4));
        }

        [Test]
        public void RefillBy_StopsAtLimitAndRestoresHorrorFirst()
        {
            var pool = new DicePool(6, 4, regularDice: 0, horrorDice: 0, horrorLimit: 3);

            pool.RefillBy(2);
            pool.RefillBy(10);

            Assert.That(pool.HorrorDice, Is.EqualTo(3));
            Assert.That(pool.RegularDice, Is.EqualTo(1));
            Assert.That(pool.AvailableDice, Is.EqualTo(4));
        }

        [Test]
        public void StructuredReaction_IsOpposingTurnOnlyAndOncePerTrigger()
        {
            var scene = new StructuredScene(
                new[] { Actor("acting", SceneSide.A, 2), Actor("reacting", SceneSide.B, 2) },
                SceneSide.A);

            scene.DeclareReaction("reacting", "attack-1");

            Assert.That(() => scene.DeclareReaction("reacting", "attack-1"), Throws.InvalidOperationException);
            Assert.That(() => scene.DeclareReaction("acting", "attack-1"), Throws.InvalidOperationException);
        }

        [Test]
        public void DecisionRequest_AcceptsOnlyAListedChoice()
        {
            var request = new DecisionRequest<string>("choose-sense", new[] { "sight", "hearing" });

            Assert.That(request.Validate("sight"), Is.EqualTo("sight"));
            Assert.That(() => request.Validate("smell"), Throws.InvalidOperationException);
        }

        private static SceneActor Actor(string id, SceneSide side, int available, int limit = 3)
        {
            return new SceneActor(id, side, new DicePool(6, limit, available));
        }
    }
}
