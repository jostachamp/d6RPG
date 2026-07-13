using System;
using System.Collections.Generic;
using ArkhamHorror.Mechanics.DynamicPool;

namespace ArkhamHorror.Mechanics.Scenes
{
    public sealed class StructuredScene
    {
        private readonly Dictionary<string, SceneActor> actors = new Dictionary<string, SceneActor>();
        private readonly HashSet<string> passedActors = new HashSet<string>();
        private readonly HashSet<string> attemptedTasks = new HashSet<string>();
        private readonly HashSet<string> attemptedReactions = new HashSet<string>();
        private readonly SceneSide firstSide;
        private readonly bool hasSurpriseRound;
        private readonly SceneSide surprisingSide;
        private readonly int surpriseRefill;
        private int sideTurnsCompletedThisRound;

        public StructuredScene(
            IEnumerable<SceneActor> initialActors,
            SceneSide firstSide,
            bool hasSurpriseRound = false,
            SceneSide surprisingSide = SceneSide.A,
            int surpriseRefill = 1)
        {
            if (initialActors == null)
            {
                throw new ArgumentNullException(nameof(initialActors));
            }

            ValidateSide(firstSide);
            ValidateSide(surprisingSide);
            if (surpriseRefill < 1 || surpriseRefill > 2)
            {
                throw new ArgumentOutOfRangeException(nameof(surpriseRefill), "A surprise refill is one die, or two when allowed by the GM.");
            }

            this.firstSide = firstSide;
            this.hasSurpriseRound = hasSurpriseRound;
            this.surprisingSide = surprisingSide;
            this.surpriseRefill = surpriseRefill;
            foreach (SceneActor actor in initialActors)
            {
                AddActorWithoutRefill(actor);
            }

            RoundNumber = 1;
            CurrentSide = firstSide;
            BeginCurrentSideTurn();
        }

        public int RoundNumber { get; private set; }

        public SceneSide CurrentSide { get; private set; }

        public bool IsSurpriseRound => hasSurpriseRound && RoundNumber == 1;

        public bool CanEndCurrentSideTurn
        {
            get
            {
                foreach (SceneActor actor in actors.Values)
                {
                    if (actor.Side == CurrentSide
                        && actor.DicePool.AvailableDice > 0
                        && !passedActors.Contains(actor.Id))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public void AddActor(SceneActor actor)
        {
            AddActorWithoutRefill(actor);
            if (actor.Side == CurrentSide)
            {
                ApplyTurnRefill(actor);
            }
        }

        public void DeclareComplexAction(string actorId, string taskId)
        {
            SceneActor actor = RequireCurrentActor(actorId);
            if (actor.DicePool.AvailableDice == 0)
            {
                throw new InvalidOperationException("An actor with an empty pool cannot perform a complex action.");
            }

            string attemptKey = MakeAttemptKey(actorId, taskId);
            if (!attemptedTasks.Add(attemptKey))
            {
                throw new InvalidOperationException("The actor already attempted that exact task during this side turn.");
            }
        }

        public void SpendSimpleAction(string actorId, DiceSelection payment)
        {
            SceneActor actor = RequireCurrentActor(actorId);
            if (payment.Total != 1)
            {
                throw new ArgumentOutOfRangeException(nameof(payment), "A simple action costs one die in a structured scene.");
            }

            actor.DicePool.Spend(payment);
        }

        public void Pass(string actorId)
        {
            RequireCurrentActor(actorId);
            passedActors.Add(actorId);
        }

        public void DeclareReaction(string actorId, string triggeringActionId)
        {
            if (string.IsNullOrWhiteSpace(actorId) || !actors.TryGetValue(actorId, out SceneActor actor))
            {
                throw new KeyNotFoundException("The actor is not in this scene.");
            }

            if (actor.Side == CurrentSide)
            {
                throw new InvalidOperationException("Reactions occur during the opposing side's turn.");
            }

            if (actor.DicePool.AvailableDice == 0)
            {
                throw new InvalidOperationException("An actor with an empty pool cannot react.");
            }

            string attemptKey = MakeAttemptKey(actorId, triggeringActionId);
            if (!attemptedReactions.Add(attemptKey))
            {
                throw new InvalidOperationException("The actor already reacted to that action.");
            }
        }

        public void EndCurrentSideTurn()
        {
            if (!CanEndCurrentSideTurn)
            {
                throw new InvalidOperationException("Every actor on the current side must pass or exhaust their pool.");
            }

            sideTurnsCompletedThisRound++;
            if (sideTurnsCompletedThisRound == 2)
            {
                RoundNumber++;
                sideTurnsCompletedThisRound = 0;
                CurrentSide = firstSide;
            }
            else
            {
                CurrentSide = Other(firstSide);
            }

            BeginCurrentSideTurn();
        }

        private void BeginCurrentSideTurn()
        {
            passedActors.Clear();
            attemptedTasks.Clear();
            foreach (SceneActor actor in actors.Values)
            {
                if (actor.Side == CurrentSide)
                {
                    ApplyTurnRefill(actor);
                }
            }
        }

        private void ApplyTurnRefill(SceneActor actor)
        {
            if (!IsSurpriseRound)
            {
                actor.DicePool.Refill();
            }
            else if (actor.Side == surprisingSide)
            {
                actor.DicePool.RefillBy(surpriseRefill);
            }
        }

        private void AddActorWithoutRefill(SceneActor actor)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            if (actors.ContainsKey(actor.Id))
            {
                throw new InvalidOperationException("An actor with that ID is already in the scene.");
            }

            actors.Add(actor.Id, actor);
        }

        private SceneActor RequireCurrentActor(string actorId)
        {
            if (string.IsNullOrWhiteSpace(actorId) || !actors.TryGetValue(actorId, out SceneActor actor))
            {
                throw new KeyNotFoundException("The actor is not in this scene.");
            }

            if (actor.Side != CurrentSide)
            {
                throw new InvalidOperationException("The actor cannot take an action during the opposing side's turn.");
            }

            if (passedActors.Contains(actorId))
            {
                throw new InvalidOperationException("The actor has passed for this side turn.");
            }

            return actor;
        }

        private static string MakeAttemptKey(string actorId, string taskId)
        {
            if (string.IsNullOrWhiteSpace(taskId))
            {
                throw new ArgumentException("A stable task ID is required.", nameof(taskId));
            }

            return actorId + "\u001f" + taskId;
        }

        private static SceneSide Other(SceneSide side)
        {
            return side == SceneSide.A ? SceneSide.B : SceneSide.A;
        }

        private static void ValidateSide(SceneSide side)
        {
            if (!Enum.IsDefined(typeof(SceneSide), side))
            {
                throw new ArgumentOutOfRangeException(nameof(side));
            }
        }
    }
}
