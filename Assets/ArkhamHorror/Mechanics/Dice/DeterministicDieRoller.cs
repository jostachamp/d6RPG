using System;

namespace ArkhamHorror.Mechanics.Dice
{
    /// <summary>Portable PCG-XSH-RR generator with explicitly capturable state.</summary>
    public sealed class DeterministicDieRoller : IStatefulDieRoller
    {
        private ulong state;
        private readonly ulong stream;

        public DeterministicDieRoller(ulong seed, ulong sequence = 1)
        {
            stream = checked(sequence * 2UL + 1UL);
            NextUInt();
            unchecked { state += seed; }
            NextUInt();
        }

        public DeterministicDieRoller(RandomState randomState)
        {
            if ((randomState.Stream & 1UL) == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(randomState), "The captured PCG stream increment must be odd.");
            }
            state = randomState.State;
            stream = randomState.Stream;
        }

        public int RollD6()
        {
            const uint bound = 6;
            uint threshold = unchecked(0U - bound) % bound;
            uint value;
            do
            {
                value = NextUInt();
            }
            while (value < threshold);

            return (int)(value % bound) + 1;
        }

        public RandomState CaptureState() => new RandomState(state, stream);

        private uint NextUInt()
        {
            ulong oldState = state;
            unchecked { state = oldState * 6364136223846793005UL + stream; }
            uint xorShifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
            int rotation = (int)(oldState >> 59);
            return (xorShifted >> rotation) | (xorShifted << ((-rotation) & 31));
        }
    }
}
