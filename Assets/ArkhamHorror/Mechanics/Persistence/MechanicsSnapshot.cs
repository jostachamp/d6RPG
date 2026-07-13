using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ArkhamHorror.Mechanics.Consequences;
using ArkhamHorror.Mechanics.DynamicPool;

namespace ArkhamHorror.Mechanics.Persistence
{
    public sealed class MechanicsSnapshot
    {
        public const int CurrentVersion = 1;
        private readonly IReadOnlyList<ActorSnapshot> actors;

        public MechanicsSnapshot(int version, IEnumerable<ActorSnapshot> actors)
        {
            if (version < 0) throw new ArgumentOutOfRangeException(nameof(version));
            if (actors == null) throw new ArgumentNullException(nameof(actors));
            var copy = new List<ActorSnapshot>(actors);
            for (int index = 0; index < copy.Count; index++)
                if (copy[index] == null) throw new ArgumentException("Actor snapshots cannot be null.", nameof(actors));
            copy.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
            for (int index = 0; index < copy.Count; index++)
            {
                if (index > 0 && copy[index - 1].Id == copy[index].Id)
                    throw new ArgumentException("Actor snapshot IDs must be unique.", nameof(actors));
            }
            Version = version;
            this.actors = copy.AsReadOnly();
        }

        public int Version { get; }
        public IReadOnlyList<ActorSnapshot> Actors => actors;

        public static MechanicsSnapshot Capture(MechanicsState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var snapshots = new List<ActorSnapshot>();
            foreach (MechanicsActorState actor in state.Actors)
                snapshots.Add(ActorSnapshot.Capture(actor));
            return new MechanicsSnapshot(CurrentVersion, snapshots);
        }

        public MechanicsState Restore()
        {
            if (Version != CurrentVersion)
                throw new InvalidOperationException("Migrate the snapshot to the current schema before restoring it.");
            var state = new MechanicsState();
            for (int index = 0; index < actors.Count; index++)
                state.AddActor(actors[index].Restore());
            return state;
        }

        public string ToCanonicalText()
        {
            var text = new StringBuilder();
            text.Append("mechanics|").Append(Version.ToString(CultureInfo.InvariantCulture));
            for (int index = 0; index < actors.Count; index++)
                actors[index].AppendCanonical(text);
            return text.ToString();
        }
    }

    public sealed class ActorSnapshot
    {
        private readonly IReadOnlyList<InjurySnapshot> injuries;

        public ActorSnapshot(
            string id, int maximum, int limit, int regularDice, int horrorDice, int horrorLimit,
            int pendingStrainInjuries, int insightLimit, int insightCurrent,
            IEnumerable<InjurySnapshot> injuries, int nextInjuryId, bool isDead,
            int traumaSessionModifier, bool isLostForever)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("An actor ID is required.", nameof(id));
            var poolValidation = new DicePool(maximum, limit, regularDice, horrorDice, horrorLimit, pendingStrainInjuries);
            var insightValidation = new InsightPool(insightLimit, insightCurrent);
            if (injuries == null) throw new ArgumentNullException(nameof(injuries));
            var injuryCopy = new List<InjurySnapshot>(injuries);
            for (int index = 0; index < injuryCopy.Count; index++)
                if (injuryCopy[index] == null) throw new ArgumentException("Injury snapshots cannot be null.", nameof(injuries));
            injuryCopy.Sort((left, right) => left.Id.CompareTo(right.Id));
            int greatestId = 0;
            for (int index = 0; index < injuryCopy.Count; index++)
            {
                if (injuryCopy[index].Id <= greatestId)
                    throw new ArgumentException("Injury snapshots require unique positive IDs.", nameof(injuries));
                greatestId = injuryCopy[index].Id;
            }
            if (nextInjuryId <= greatestId) throw new ArgumentOutOfRangeException(nameof(nextInjuryId));
            if (traumaSessionModifier < 0) throw new ArgumentOutOfRangeException(nameof(traumaSessionModifier));

            Id = id; Maximum = maximum; Limit = limit; RegularDice = regularDice; HorrorDice = horrorDice;
            HorrorLimit = horrorLimit; PendingStrainInjuries = pendingStrainInjuries;
            InsightLimit = insightLimit; InsightCurrent = insightCurrent; this.injuries = injuryCopy.AsReadOnly();
            NextInjuryId = nextInjuryId; IsDead = isDead; TraumaSessionModifier = traumaSessionModifier;
            IsLostForever = isLostForever;
        }

        public string Id { get; }
        public int Maximum { get; }
        public int Limit { get; }
        public int RegularDice { get; }
        public int HorrorDice { get; }
        public int HorrorLimit { get; }
        public int PendingStrainInjuries { get; }
        public int InsightLimit { get; }
        public int InsightCurrent { get; }
        public IReadOnlyList<InjurySnapshot> Injuries => injuries;
        public int NextInjuryId { get; }
        public bool IsDead { get; }
        public int TraumaSessionModifier { get; }
        public bool IsLostForever { get; }

        internal static ActorSnapshot Capture(MechanicsActorState actor)
        {
            var injuries = new List<InjurySnapshot>();
            for (int index = 0; index < actor.Injuries.Injuries.Count; index++)
                injuries.Add(InjurySnapshot.Capture(actor.Injuries.Injuries[index]));
            return new ActorSnapshot(actor.Id, actor.DicePool.Maximum, actor.DicePool.Limit,
                actor.DicePool.RegularDice, actor.DicePool.HorrorDice, actor.DicePool.HorrorLimit,
                actor.DicePool.PendingStrainInjuries, actor.Insight.Limit, actor.Insight.Current,
                injuries, actor.Injuries.NextId, actor.Injuries.IsDead,
                actor.Trauma.SessionRollModifier, actor.Trauma.IsLostForever);
        }

        internal MechanicsActorState Restore()
        {
            var restored = new InjuryInstance[injuries.Count];
            for (int index = 0; index < injuries.Count; index++) restored[index] = injuries[index].Restore();
            var injuryState = new InjuryState();
            injuryState.Restore(restored, IsDead, NextInjuryId);
            var trauma = new TraumaState();
            trauma.Restore(TraumaSessionModifier, IsLostForever);
            return new MechanicsActorState(Id,
                new DicePool(Maximum, Limit, RegularDice, HorrorDice, HorrorLimit, PendingStrainInjuries),
                new InsightPool(InsightLimit, InsightCurrent), injuryState, trauma);
        }

        internal void AppendCanonical(StringBuilder text)
        {
            text.Append("|actor:").Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(Id)))
                .Append(',').Append(Maximum).Append(',').Append(Limit).Append(',').Append(RegularDice)
                .Append(',').Append(HorrorDice).Append(',').Append(HorrorLimit).Append(',').Append(PendingStrainInjuries)
                .Append(',').Append(InsightLimit).Append(',').Append(InsightCurrent).Append(',').Append(NextInjuryId)
                .Append(',').Append(IsDead ? 1 : 0).Append(',').Append(TraumaSessionModifier)
                .Append(',').Append(IsLostForever ? 1 : 0);
            for (int index = 0; index < injuries.Count; index++) injuries[index].AppendCanonical(text);
        }
    }

    public sealed class InjurySnapshot
    {
        public InjurySnapshot(int id, InjuryKind kind, SenseKind? lostSense, bool temporaryEffectActive, bool direTreatmentPending)
        {
            if (id < 1) throw new ArgumentOutOfRangeException(nameof(id));
            InjuryDefinition definition = InjuryCatalog.Get(kind);
            if (definition.RequiresSense != lostSense.HasValue)
                throw new ArgumentException("The saved sense does not match the injury kind.", nameof(lostSense));
            if (temporaryEffectActive && kind != InjuryKind.HeavyBlow)
                throw new ArgumentException("Only Heavy Blow has a temporary effect flag.", nameof(temporaryEffectActive));
            if (direTreatmentPending && kind != InjuryKind.Dire)
                throw new ArgumentException("Only Dire has a treatment deadline flag.", nameof(direTreatmentPending));
            Id = id; Kind = kind; LostSense = lostSense;
            TemporaryEffectActive = temporaryEffectActive; DireTreatmentPending = direTreatmentPending;
        }

        public int Id { get; }
        public InjuryKind Kind { get; }
        public SenseKind? LostSense { get; }
        public bool TemporaryEffectActive { get; }
        public bool DireTreatmentPending { get; }

        internal static InjurySnapshot Capture(InjuryInstance injury) =>
            new InjurySnapshot(injury.Id, injury.Kind, injury.LostSense, injury.TemporaryEffectActive, injury.DireTreatmentPending);

        internal InjuryInstance Restore()
        {
            var injury = new InjuryInstance(Id, Kind, LostSense);
            injury.TemporaryEffectActive = TemporaryEffectActive;
            injury.DireTreatmentPending = DireTreatmentPending;
            return injury;
        }

        internal void AppendCanonical(StringBuilder text)
        {
            text.Append(";injury:").Append(Id).Append(',').Append((int)Kind).Append(',')
                .Append(LostSense.HasValue ? ((int)LostSense.Value).ToString(CultureInfo.InvariantCulture) : "-")
                .Append(',').Append(TemporaryEffectActive ? 1 : 0).Append(',').Append(DireTreatmentPending ? 1 : 0);
        }
    }
}
