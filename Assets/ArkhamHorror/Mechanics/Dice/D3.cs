using System;

namespace ArkhamHorror.Mechanics.Dice
{
    public static class D3
    {
        public static int FromD6(int d6Result)
        {
            if (d6Result < 1 || d6Result > 6)
            {
                throw new ArgumentOutOfRangeException(nameof(d6Result), "A d6 result must be from 1 through 6.");
            }

            return (d6Result + 1) / 2;
        }

        public static int Roll(IDieRoller dieRoller)
        {
            if (dieRoller == null)
            {
                throw new ArgumentNullException(nameof(dieRoller));
            }

            return FromD6(dieRoller.RollD6());
        }
    }
}
