using System;
using ArkhamHorror.Mechanics.DynamicPool;

namespace ArkhamHorror.Mechanics.Consequences
{
    public sealed class TraumaState
    {
        public int SessionRollModifier { get; private set; }

        public bool IsLostForever { get; private set; }

        internal void Restore(int sessionRollModifier, bool isLostForever)
        {
            if (sessionRollModifier < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sessionRollModifier));
            }

            SessionRollModifier = sessionRollModifier;
            IsLostForever = isLostForever;
        }

        public TraumaApplication Apply(
            TraumaResolution resolution,
            DicePool dicePool,
            InsightPool insight,
            TraumaChoice choice = TraumaChoice.Accept)
        {
            if (resolution == null)
            {
                throw new ArgumentNullException(nameof(resolution));
            }

            if (dicePool == null)
            {
                throw new ArgumentNullException(nameof(dicePool));
            }

            if (insight == null)
            {
                throw new ArgumentNullException(nameof(insight));
            }

            if (resolution.SessionModifier != SessionRollModifier)
            {
                throw new InvalidOperationException("The trauma result was generated for a different session modifier.");
            }

            ValidateChoice(resolution.Kind, choice);

            bool honored = choice == TraumaChoice.Accept;
            int insightSpent = 0;
            bool triggersPersonality = false;
            bool extendsDuration = false;
            int diceDiscarded = 0;
            bool removalAvoided = false;

            switch (resolution.Kind)
            {
                case TraumaKind.SubtleStrangeness:
                    break;
                case TraumaKind.Shocked:
                    if (dicePool.AvailableDice > 0)
                    {
                        dicePool.Discard(1);
                        diceDiscarded = 1;
                    }
                    else
                    {
                        SessionRollModifier++;
                    }
                    break;
                case TraumaKind.Stunned:
                    if (dicePool.AvailableDice > 0)
                    {
                        diceDiscarded = dicePool.AvailableDice;
                        dicePool.Discard(diceDiscarded);
                    }
                    else
                    {
                        SessionRollModifier++;
                    }
                    break;
                case TraumaKind.OvercomeByHorror:
                    if (choice == TraumaChoice.SpendInsight && insight.TrySpend(1))
                    {
                        honored = true;
                        insightSpent = 1;
                    }
                    else
                    {
                        triggersPersonality = true;
                    }
                    break;
                case TraumaKind.MindUndone:
                    SessionRollModifier++;
                    if (choice == TraumaChoice.SpendInsight && insight.TrySpend(2))
                    {
                        honored = true;
                        insightSpent = 2;
                    }
                    else
                    {
                        triggersPersonality = true;
                        extendsDuration = true;
                    }
                    break;
                case TraumaKind.LostForever:
                    if (choice == TraumaChoice.SacrificeInsightLimit
                        && insight.TrySacrificeLimitToAvoidRemoval())
                    {
                        honored = true;
                        removalAvoided = true;
                    }
                    else
                    {
                        IsLostForever = true;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(resolution));
            }

            return new TraumaApplication(
                resolution.Kind,
                honored,
                insightSpent,
                triggersPersonality,
                extendsDuration,
                diceDiscarded,
                removalAvoided);
        }

        public void EndSession()
        {
            SessionRollModifier = 0;
        }

        private static void ValidateChoice(TraumaKind kind, TraumaChoice choice)
        {
            bool valid = choice == TraumaChoice.Accept
                || (choice == TraumaChoice.SpendInsight
                    && (kind == TraumaKind.OvercomeByHorror || kind == TraumaKind.MindUndone))
                || (choice == TraumaChoice.SacrificeInsightLimit && kind == TraumaKind.LostForever);

            if (!valid)
            {
                throw new InvalidOperationException("That insight choice cannot be applied to this trauma result.");
            }
        }
    }
}
