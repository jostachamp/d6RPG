using System;

namespace ArkhamHorror.Mechanics.DynamicPool
{
    public readonly struct DiceSelection
    {
        public DiceSelection(int regularDice, int horrorDice)
        {
            if (regularDice < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(regularDice));
            }

            if (horrorDice < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(horrorDice));
            }

            RegularDice = regularDice;
            HorrorDice = horrorDice;
        }

        public int RegularDice { get; }

        public int HorrorDice { get; }

        public int Total => checked(RegularDice + HorrorDice);
    }
}
