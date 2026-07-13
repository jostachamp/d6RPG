using System;
using ArkhamHorror.Mechanics.Dice;

namespace ArkhamHorror.Mechanics.Consequences
{
    public sealed class InjuryGenerator
    {
        private readonly IDieRoller dieRoller;

        public InjuryGenerator(IDieRoller dieRoller)
        {
            this.dieRoller = dieRoller ?? throw new ArgumentNullException(nameof(dieRoller));
        }

        public InjuryResolution RollD6(int existingInjuries, int externalModifier = 0)
        {
            int naturalRoll = dieRoller.RollD6();
            ValidateD6(naturalRoll);
            return FromBaseRoll(naturalRoll, existingInjuries, externalModifier);
        }

        public static InjuryResolution FromBaseRoll(int baseRoll, int existingInjuries, int externalModifier = 0)
        {
            if (baseRoll < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(baseRoll));
            }

            if (existingInjuries < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(existingInjuries));
            }

            long unboundedTotal = (long)baseRoll + existingInjuries + externalModifier;
            int total = unboundedTotal > int.MaxValue ? int.MaxValue
                : unboundedTotal < 1 ? 1
                : (int)unboundedTotal;

            return new InjuryResolution(
                baseRoll,
                existingInjuries,
                externalModifier,
                total,
                InjuryCatalog.ResolveTableResult(total));
        }

        private static void ValidateD6(int result)
        {
            if (result < 1 || result > 6)
            {
                throw new ArgumentOutOfRangeException(nameof(result), "A d6 result must be from 1 through 6.");
            }
        }
    }
}
