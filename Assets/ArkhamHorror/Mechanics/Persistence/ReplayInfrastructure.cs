using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ArkhamHorror.Mechanics.Dice;
using ArkhamHorror.Mechanics.Scenes;

namespace ArkhamHorror.Mechanics.Persistence
{
    public sealed class MechanicsEvent
    {
        internal MechanicsEvent(int sequence, string commandId, string kind, string actorId, string data)
        {
            Sequence = sequence; CommandId = commandId; Kind = kind; ActorId = actorId; Data = data;
        }

        public int Sequence { get; }
        public string CommandId { get; }
        public string Kind { get; }
        public string ActorId { get; }
        public string Data { get; }

        internal void AppendCanonical(StringBuilder text)
        {
            text.Append(Sequence).Append(':').Append(Encode(CommandId)).Append(':').Append(Encode(Kind))
                .Append(':').Append(Encode(ActorId)).Append(':').Append(Encode(Data)).Append('|');
        }

        private static string Encode(string value) =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));
    }

    public sealed class EventJournal
    {
        private readonly List<MechanicsEvent> events = new List<MechanicsEvent>();
        private readonly IReadOnlyList<MechanicsEvent> readOnlyEvents;

        public EventJournal() { readOnlyEvents = events.AsReadOnly(); }
        public IReadOnlyList<MechanicsEvent> Events => readOnlyEvents;

        public void Record(string commandId, string kind, string actorId, string data)
        {
            if (string.IsNullOrWhiteSpace(commandId)) throw new ArgumentException("A command ID is required.", nameof(commandId));
            if (string.IsNullOrWhiteSpace(kind)) throw new ArgumentException("An event kind is required.", nameof(kind));
            events.Add(new MechanicsEvent(events.Count + 1, commandId, kind, actorId, data ?? string.Empty));
        }

        public string Fingerprint()
        {
            var text = new StringBuilder();
            for (int index = 0; index < events.Count; index++) events[index].AppendCanonical(text);
            return CanonicalFingerprint.Compute(text.ToString());
        }
    }

    public sealed class DecisionRecord
    {
        public DecisionRecord(string requestId, string choice)
        {
            if (string.IsNullOrWhiteSpace(requestId)) throw new ArgumentException("A request ID is required.", nameof(requestId));
            RequestId = requestId;
            Choice = choice ?? throw new ArgumentNullException(nameof(choice));
        }
        public string RequestId { get; }
        public string Choice { get; }
    }

    public sealed class RecordedDecisionProvider : IDecisionProvider
    {
        private readonly Queue<DecisionRecord> decisions;

        public RecordedDecisionProvider(IEnumerable<DecisionRecord> decisions)
        {
            if (decisions == null) throw new ArgumentNullException(nameof(decisions));
            this.decisions = new Queue<DecisionRecord>(decisions);
        }

        public int Remaining => decisions.Count;

        public T Choose<T>(DecisionRequest<T> request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (decisions.Count == 0) throw new InvalidOperationException("No recorded decision remains.");
            DecisionRecord record = decisions.Dequeue();
            if (!string.Equals(record.RequestId, request.Id, StringComparison.Ordinal))
                throw new InvalidOperationException("The recorded decision does not match the current request.");
            object converted = typeof(T).IsEnum
                ? Enum.Parse(typeof(T), record.Choice, false)
                : Convert.ChangeType(record.Choice, typeof(T), CultureInfo.InvariantCulture);
            return request.Validate((T)converted);
        }
    }

    public sealed class ReplayContext
    {
        public ReplayContext(IStatefulDieRoller roller, EventJournal journal)
        {
            Roller = roller ?? throw new ArgumentNullException(nameof(roller));
            Journal = journal ?? throw new ArgumentNullException(nameof(journal));
        }
        public IStatefulDieRoller Roller { get; }
        public EventJournal Journal { get; }
    }

    public sealed class ReplayResult
    {
        internal ReplayResult(MechanicsSnapshot snapshot, RandomState randomState, EventJournal journal)
        {
            Snapshot = snapshot; RandomState = randomState; Journal = journal;
        }
        public MechanicsSnapshot Snapshot { get; }
        public RandomState RandomState { get; }
        public EventJournal Journal { get; }
        public string StateFingerprint => CanonicalFingerprint.Compute(Snapshot.ToCanonicalText());
        public string EventFingerprint => Journal.Fingerprint();
    }

    public sealed class ReplayRecording
    {
        private readonly IReadOnlyList<MechanicsCommand> commands;

        public ReplayRecording(MechanicsSnapshot initialSnapshot, RandomState initialRandomState,
            IEnumerable<MechanicsCommand> commands, string expectedStateFingerprint, string expectedEventFingerprint)
        {
            InitialSnapshot = initialSnapshot ?? throw new ArgumentNullException(nameof(initialSnapshot));
            if (commands == null) throw new ArgumentNullException(nameof(commands));
            this.commands = new List<MechanicsCommand>(commands).AsReadOnly();
            InitialRandomState = initialRandomState;
            ExpectedStateFingerprint = expectedStateFingerprint ?? throw new ArgumentNullException(nameof(expectedStateFingerprint));
            ExpectedEventFingerprint = expectedEventFingerprint ?? throw new ArgumentNullException(nameof(expectedEventFingerprint));
        }

        public MechanicsSnapshot InitialSnapshot { get; }
        public RandomState InitialRandomState { get; }
        public IReadOnlyList<MechanicsCommand> Commands => commands;
        public string ExpectedStateFingerprint { get; }
        public string ExpectedEventFingerprint { get; }
    }

    public static class ReplayEngine
    {
        public static ReplayResult Run(MechanicsSnapshot initial, RandomState randomState, IEnumerable<MechanicsCommand> commands)
        {
            if (initial == null) throw new ArgumentNullException(nameof(initial));
            if (commands == null) throw new ArgumentNullException(nameof(commands));
            MechanicsState state = initial.Restore();
            var roller = new DeterministicDieRoller(randomState);
            var journal = new EventJournal();
            var context = new ReplayContext(roller, journal);
            var commandIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (MechanicsCommand command in commands)
            {
                if (command == null) throw new ArgumentException("A replay command cannot be null.", nameof(commands));
                if (!commandIds.Add(command.Id)) throw new InvalidOperationException("Command IDs must be unique within a replay.");
                command.Execute(state, context);
            }
            return new ReplayResult(MechanicsSnapshot.Capture(state), roller.CaptureState(), journal);
        }

        public static ReplayResult Verify(ReplayRecording recording)
        {
            if (recording == null) throw new ArgumentNullException(nameof(recording));
            ReplayResult result = Run(recording.InitialSnapshot, recording.InitialRandomState, recording.Commands);
            if (!string.Equals(result.StateFingerprint, recording.ExpectedStateFingerprint, StringComparison.Ordinal)
                || !string.Equals(result.EventFingerprint, recording.ExpectedEventFingerprint, StringComparison.Ordinal))
                throw new InvalidOperationException("Replay output does not match the certified recording.");
            return result;
        }
    }

    public static class CanonicalFingerprint
    {
        public static string Compute(string value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            ulong hash = 14695981039346656037UL;
            for (int index = 0; index < bytes.Length; index++)
            {
                hash ^= bytes[index];
                unchecked { hash *= 1099511628211UL; }
            }
            return hash.ToString("x16", CultureInfo.InvariantCulture);
        }
    }
}
