using System;
using System.Collections.Generic;
using ArkhamHorror.Mechanics.Consequences;
using ArkhamHorror.Mechanics.DynamicPool;

namespace ArkhamHorror.Mechanics.Persistence
{
    public sealed class MechanicsActorState
    {
        public MechanicsActorState(
            string id,
            DicePool dicePool,
            InsightPool insight,
            InjuryState injuries = null,
            TraumaState trauma = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("An actor ID is required.", nameof(id));
            Id = id;
            DicePool = dicePool ?? throw new ArgumentNullException(nameof(dicePool));
            Insight = insight ?? throw new ArgumentNullException(nameof(insight));
            Injuries = injuries ?? new InjuryState();
            Trauma = trauma ?? new TraumaState();
        }

        public string Id { get; }
        public DicePool DicePool { get; }
        public InsightPool Insight { get; }
        public InjuryState Injuries { get; }
        public TraumaState Trauma { get; }
    }

    public sealed class MechanicsState
    {
        private readonly Dictionary<string, MechanicsActorState> actors =
            new Dictionary<string, MechanicsActorState>(StringComparer.Ordinal);

        public IEnumerable<MechanicsActorState> Actors => actors.Values;

        public void AddActor(MechanicsActorState actor)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            if (actors.ContainsKey(actor.Id))
                throw new InvalidOperationException("An actor with that ID already exists.");
            actors.Add(actor.Id, actor);
        }

        public MechanicsActorState GetActor(string actorId)
        {
            if (string.IsNullOrWhiteSpace(actorId) || !actors.TryGetValue(actorId, out MechanicsActorState actor))
                throw new KeyNotFoundException("The actor does not exist in this mechanics state.");
            return actor;
        }
    }
}
