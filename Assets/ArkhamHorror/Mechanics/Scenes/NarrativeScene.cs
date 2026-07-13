using System;
using System.Collections.Generic;
using ArkhamHorror.Mechanics.DynamicPool;

namespace ArkhamHorror.Mechanics.Scenes
{
    public sealed class NarrativeScene
    {
        private readonly Dictionary<string, SceneActor> actors = new Dictionary<string, SceneActor>();
        private readonly HashSet<string> attemptedTasks = new HashSet<string>();
        private readonly HashSet<string> attemptedReactions = new HashSet<string>();

        public NarrativeScene(IEnumerable<SceneActor> initialActors)
        {
            if (initialActors == null)
            {
                throw new ArgumentNullException(nameof(initialActors));
            }

            foreach (SceneActor actor in initialActors)
            {
                AddActor(actor);
            }
        }

        public bool IsEnded { get; private set; }

        public bool AllPoolsExhausted
        {
            get
            {
                if (actors.Count == 0)
                {
                    return false;
                }

                foreach (SceneActor actor in actors.Values)
                {
                    if (actor.DicePool.AvailableDice > 0)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public void AddActor(SceneActor actor)
        {
            if (actor == null)
            {
                throw new ArgumentNullException(nameof(actor));
            }

            EnsureActive();
            if (actors.ContainsKey(actor.Id))
            {
                throw new InvalidOperationException("An actor with that ID is already in the scene.");
            }

            actor.DicePool.Refill();
            actors.Add(actor.Id, actor);
        }

        public void DeclareComplexAction(string actorId, string taskId)
        {
            EnsureActive();
            SceneActor actor = GetActor(actorId);
            if (actor.DicePool.AvailableDice == 0)
            {
                throw new InvalidOperationException("An actor with an empty pool cannot perform a complex action.");
            }

            string attemptKey = MakeAttemptKey(actorId, taskId);
            if (!attemptedTasks.Add(attemptKey))
            {
                throw new InvalidOperationException("The actor already attempted that exact task during this narrative scene.");
            }
        }

        public void SpendSimpleAction(string actorId, SimpleActionKind actionKind, DiceSelection payment)
        {
            EnsureActive();
            SceneActor actor = GetActor(actorId);
            int cost = SceneActionRules.SimpleActionCost(SceneKind.Narrative, actionKind);
            if (payment.Total != cost)
            {
                throw new ArgumentOutOfRangeException(nameof(payment), "The selected payment does not match the action cost.");
            }

            if (cost > 0)
            {
                actor.DicePool.Spend(payment);
            }
        }

        public void DeclareReaction(string actorId, string triggeringActionId)
        {
            EnsureActive();
            SceneActor actor = GetActor(actorId);
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

        public void End()
        {
            EnsureActive();
            IsEnded = true;
        }

        private SceneActor GetActor(string actorId)
        {
            if (string.IsNullOrWhiteSpace(actorId) || !actors.TryGetValue(actorId, out SceneActor actor))
            {
                throw new KeyNotFoundException("The actor is not in this scene.");
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

        private void EnsureActive()
        {
            if (IsEnded)
            {
                throw new InvalidOperationException("The scene has ended.");
            }
        }
    }
}
