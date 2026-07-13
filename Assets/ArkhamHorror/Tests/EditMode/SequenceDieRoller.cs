using System;
using System.Collections.Generic;
using ArkhamHorror.Mechanics.Dice;

namespace ArkhamHorror.Mechanics.Tests
{
    internal sealed class SequenceDieRoller : IDieRoller
    {
        private readonly Queue<int> results;

        public SequenceDieRoller(params int[] results)
        {
            this.results = new Queue<int>(results ?? throw new ArgumentNullException(nameof(results)));
        }

        public int RollD6()
        {
            if (results.Count == 0)
            {
                throw new InvalidOperationException("No deterministic die results remain.");
            }

            return results.Dequeue();
        }
    }
}
