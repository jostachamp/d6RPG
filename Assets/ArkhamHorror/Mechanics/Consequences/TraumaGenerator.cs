using System;
using ArkhamHorror.Mechanics.Dice;

namespace ArkhamHorror.Mechanics.Consequences
{
    public sealed class TraumaGenerator
    {
        private readonly IDieRoller dieRoller;

        public TraumaGenerator(IDieRoller dieRoller)
        {
            this.dieRoller = dieRoller ?? throw new ArgumentNullException(nameof(dieRoller));
        }

        public TraumaResolution Roll(
            int horrorOnesModifier,
            int sessionModifier,
            int externalModifier = 0)
        {
            int naturalRoll = dieRoller.RollD6();
            if (naturalRoll < 1 || naturalRoll > 6)
            {
                throw new ArgumentOutOfRangeException(nameof(naturalRoll), "A d6 result must be from 1 through 6.");
            }

            return FromRoll(naturalRoll, horrorOnesModifier, sessionModifier, externalModifier);
        }

        public static TraumaResolution FromRoll(
            int naturalRoll,
            int horrorOnesModifier,
            int sessionModifier,
            int externalModifier = 0)
        {
            if (naturalRoll < 1 || naturalRoll > 6)
            {
                throw new ArgumentOutOfRangeException(nameof(naturalRoll));
            }

            if (horrorOnesModifier < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(horrorOnesModifier));
            }

            if (sessionModifier < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sessionModifier));
            }

            long unboundedTotal = (long)naturalRoll + horrorOnesModifier + sessionModifier + externalModifier;
            int total = unboundedTotal > int.MaxValue ? int.MaxValue
                : unboundedTotal < 1 ? 1
                : (int)unboundedTotal;

            return new TraumaResolution(
                naturalRoll,
                horrorOnesModifier,
                sessionModifier,
                externalModifier,
                total,
                ResolveKind(total));
        }

        private static TraumaKind ResolveKind(int total)
        {
            if (total <= 2)
            {
                return TraumaKind.SubtleStrangeness;
            }

            if (total == 3)
            {
                return TraumaKind.Shocked;
            }

            if (total == 4)
            {
                return TraumaKind.Stunned;
            }

            if (total <= 7)
            {
                return TraumaKind.OvercomeByHorror;
            }

            if (total <= 10)
            {
                return TraumaKind.MindUndone;
            }

            return TraumaKind.LostForever;
        }
    }
}
