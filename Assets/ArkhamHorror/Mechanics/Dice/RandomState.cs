using System;

namespace ArkhamHorror.Mechanics.Dice
{
    public struct RandomState : IEquatable<RandomState>
    {
        public RandomState(ulong state, ulong stream)
        {
            if ((stream & 1UL) == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stream), "The PCG stream increment must be odd.");
            }

            State = state;
            Stream = stream;
        }

        public ulong State { get; }
        public ulong Stream { get; }

        public bool Equals(RandomState other) => State == other.State && Stream == other.Stream;
        public override bool Equals(object obj) => obj is RandomState other && Equals(other);
        public override int GetHashCode() => State.GetHashCode() ^ Stream.GetHashCode();
    }

    public interface IStatefulDieRoller : IDieRoller
    {
        RandomState CaptureState();
    }
}
