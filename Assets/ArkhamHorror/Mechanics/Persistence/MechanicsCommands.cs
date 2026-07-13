using System;
using System.Globalization;
using System.Text;
using ArkhamHorror.Mechanics.Consequences;
using ArkhamHorror.Mechanics.DynamicPool;

namespace ArkhamHorror.Mechanics.Persistence
{
    public abstract class MechanicsCommand
    {
        protected MechanicsCommand(string id, string actorId)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A command ID is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(actorId)) throw new ArgumentException("An actor ID is required.", nameof(actorId));
            Id = id; ActorId = actorId;
        }
        public string Id { get; }
        public string ActorId { get; }
        internal abstract void Execute(MechanicsState state, ReplayContext context);
    }

    public enum StateChangeKind
    {
        RefillPool = 0, SufferDamage = 1, HealDamage = 2, SufferHorror = 3,
        ReduceHorror = 4, GainInsight = 5, SpendInsight = 6, EndSession = 7
    }

    public sealed class StateChangeCommand : MechanicsCommand
    {
        public StateChangeCommand(string id, string actorId, StateChangeKind kind, int amount = 0)
            : base(id, actorId)
        {
            if (!Enum.IsDefined(typeof(StateChangeKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (kind == StateChangeKind.EndSession || kind == StateChangeKind.RefillPool)
            {
                if (amount != 0) throw new ArgumentOutOfRangeException(nameof(amount));
            }
            else if (amount < 1) throw new ArgumentOutOfRangeException(nameof(amount));
            Kind = kind; Amount = amount;
        }
        public StateChangeKind Kind { get; }
        public int Amount { get; }

        internal override void Execute(MechanicsState state, ReplayContext context)
        {
            MechanicsActorState actor = state.GetActor(ActorId);
            bool honored = true;
            switch (Kind)
            {
                case StateChangeKind.RefillPool: actor.DicePool.Refill(); break;
                case StateChangeKind.SufferDamage: actor.DicePool.SufferDamage(Amount); break;
                case StateChangeKind.HealDamage: RecoveryService.HealDamage(actor.DicePool, actor.Injuries, Amount); break;
                case StateChangeKind.SufferHorror: actor.DicePool.SufferHorror(Amount); break;
                case StateChangeKind.ReduceHorror: actor.DicePool.ReduceHorror(Amount); break;
                case StateChangeKind.GainInsight: actor.Insight.Gain(Amount); break;
                case StateChangeKind.SpendInsight: honored = actor.Insight.TrySpend(Amount); break;
                case StateChangeKind.EndSession: actor.Trauma.EndSession(); break;
                default: throw new ArgumentOutOfRangeException();
            }
            context.Journal.Record(Id, Kind.ToString(), ActorId,
                "amount=" + Amount.ToString(CultureInfo.InvariantCulture) + ";honored=" + (honored ? "1" : "0"));
        }
    }

    public sealed class RollActionCommand : MechanicsCommand
    {
        public RollActionCommand(string id, string actorId, ActionKind actionKind, DiceSelection poolDice,
            SkillLevel skillLevel, ActionDifficulty difficulty = ActionDifficulty.Standard,
            RollCondition rollCondition = RollCondition.None, int additionalHandDice = 0, int dieResultModifier = 0)
            : base(id, actorId)
        {
            if (actionKind != ActionKind.Complex && actionKind != ActionKind.Reaction)
                throw new ArgumentOutOfRangeException(nameof(actionKind));
            ActionKind = actionKind; PoolDice = poolDice; SkillLevel = skillLevel; Difficulty = difficulty;
            RollCondition = rollCondition; AdditionalHandDice = additionalHandDice; DieResultModifier = dieResultModifier;
        }
        public ActionKind ActionKind { get; }
        public DiceSelection PoolDice { get; }
        public SkillLevel SkillLevel { get; }
        public ActionDifficulty Difficulty { get; }
        public RollCondition RollCondition { get; }
        public int AdditionalHandDice { get; }
        public int DieResultModifier { get; }

        internal override void Execute(MechanicsState state, ReplayContext context)
        {
            MechanicsActorState actor = state.GetActor(ActorId);
            var resolver = new ComplexActionResolver(context.Roller);
            ComplexActionResult result = ActionKind == ActionKind.Reaction
                ? resolver.PerformReaction(actor.DicePool, PoolDice, SkillLevel, RollCondition, AdditionalHandDice, DieResultModifier)
                : resolver.Perform(actor.DicePool, PoolDice, SkillLevel, Difficulty, RollCondition, AdditionalHandDice, DieResultModifier);
            var rolls = new StringBuilder();
            for (int index = 0; index < result.AllRolls.Count; index++)
            {
                if (index > 0) rolls.Append(',');
                rolls.Append((int)result.AllRolls[index].Kind).Append('-').Append(result.AllRolls[index].NaturalResult);
            }
            context.Journal.Record(Id, ActionKind.ToString(), ActorId,
                "rolls=" + rolls + ";successes=" + result.SuccessCount.ToString(CultureInfo.InvariantCulture)
                + ";required=" + result.RequiredSuccesses.ToString(CultureInfo.InvariantCulture)
                + ";horrorOnes=" + result.HorrorOnesRolled.ToString(CultureInfo.InvariantCulture));
        }
    }

    public sealed class GenerateInjuryCommand : MechanicsCommand
    {
        public GenerateInjuryCommand(string id, string actorId, int externalModifier = 0,
            SenseKind? lostSense = null, bool fatalOutcomeAvoided = false) : base(id, actorId)
        {
            ExternalModifier = externalModifier; LostSense = lostSense; FatalOutcomeAvoided = fatalOutcomeAvoided;
        }
        public int ExternalModifier { get; }
        public SenseKind? LostSense { get; }
        public bool FatalOutcomeAvoided { get; }

        internal override void Execute(MechanicsState state, ReplayContext context)
        {
            MechanicsActorState actor = state.GetActor(ActorId);
            int roll = context.Roller.RollD6();
            InjuryResolution resolution = InjuryGenerator.FromBaseRoll(roll, actor.Injuries.Count, ExternalModifier);
            InjuryInstance injury = actor.Injuries.Apply(resolution, actor.DicePool, LostSense, FatalOutcomeAvoided);
            context.Journal.Record(Id, "Injury", ActorId,
                "roll=" + roll + ";kind=" + resolution.Kind + ";id=" + (injury == null ? "-" : injury.Id.ToString(CultureInfo.InvariantCulture))
                + ";dead=" + (actor.Injuries.IsDead ? "1" : "0"));
        }
    }

    public sealed class GenerateTraumaCommand : MechanicsCommand
    {
        public GenerateTraumaCommand(string id, string actorId, int horrorOnesRolled,
            int externalModifier = 0, TraumaChoice choice = TraumaChoice.Accept) : base(id, actorId)
        {
            if (horrorOnesRolled < 0) throw new ArgumentOutOfRangeException(nameof(horrorOnesRolled));
            HorrorOnesRolled = horrorOnesRolled; ExternalModifier = externalModifier; Choice = choice;
        }
        public int HorrorOnesRolled { get; }
        public int ExternalModifier { get; }
        public TraumaChoice Choice { get; }

        internal override void Execute(MechanicsState state, ReplayContext context)
        {
            MechanicsActorState actor = state.GetActor(ActorId);
            int roll = context.Roller.RollD6();
            TraumaResolution resolution = TraumaGenerator.FromRoll(
                roll, HorrorOnesRolled, actor.Trauma.SessionRollModifier, ExternalModifier);
            TraumaApplication applied = actor.Trauma.Apply(resolution, actor.DicePool, actor.Insight, Choice);
            context.Journal.Record(Id, "Trauma", ActorId,
                "roll=" + roll + ";kind=" + resolution.Kind + ";insight=" + applied.InsightSpent
                + ";discarded=" + applied.PoolDiceDiscarded + ";lost=" + (actor.Trauma.IsLostForever ? "1" : "0"));
        }
    }
}
