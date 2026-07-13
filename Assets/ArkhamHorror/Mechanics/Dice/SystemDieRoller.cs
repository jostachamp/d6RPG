using System;

namespace ArkhamHorror.Mechanics.Dice
{
    public sealed class SystemDieRoller : IDieRoller
    {
        private readonly Random random;

        public SystemDieRoller()
            : this(new Random())
        {
        }

        public SystemDieRoller(int seed)
            : this(new Random(seed))
        {
        }

        private SystemDieRoller(Random random)
        {
            this.random = random;
        }

        public int RollD6()
        {
            return random.Next(1, 7);
        }
    }
}
