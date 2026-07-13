using System;
using System.Collections.Generic;
using ArkhamHorror.Mechanics.Dice;

namespace ArkhamHorror.Mechanics.DynamicPool
{
    public sealed class ComplexActionResult
    {
        private readonly IReadOnlyList<int> dieResults;
        private readonly IReadOnlyList<DieRoll> allRolls;
        private readonly IReadOnlyList<DieRoll> keptRolls;
        private readonly IReadOnlyList<DieRollEvent> rollEvents;

        internal ComplexActionResult(
            DieRoll[] allRolls,
            DieRoll[] keptRolls,
            DieRollEvent[] rollEvents,
            ActionKind actionKind,
            int successCount,
            ActionDifficulty difficulty,
            int horrorOnesRolled)
        {
            this.allRolls = Array.AsReadOnly(allRolls ?? throw new ArgumentNullException(nameof(allRolls)));
            this.keptRolls = Array.AsReadOnly(keptRolls ?? throw new ArgumentNullException(nameof(keptRolls)));
            this.rollEvents = Array.AsReadOnly(rollEvents ?? throw new ArgumentNullException(nameof(rollEvents)));

            var naturalResults = new int[keptRolls.Length];
            for (int index = 0; index < keptRolls.Length; index++)
            {
                naturalResults[index] = keptRolls[index].NaturalResult;
            }

            dieResults = Array.AsReadOnly(naturalResults);
            SuccessCount = successCount;
            RequiredSuccesses = (int)difficulty;
            HorrorOnesRolled = horrorOnesRolled;
            ActionKind = actionKind;
        }

        /// <summary>
        /// Natural results remaining after advantage/disadvantage removal.
        /// </summary>
        public IReadOnlyList<int> DieResults => dieResults;

        public IReadOnlyList<DieRoll> AllRolls => allRolls;

        public IReadOnlyList<DieRoll> KeptRolls => keptRolls;

        public IReadOnlyList<DieRollEvent> RollEvents => rollEvents;

        public ActionKind ActionKind { get; }

        public int SuccessCount { get; }

        public int RequiredSuccesses { get; }

        public int HorrorOnesRolled { get; }

        public bool TriggersTrauma => HorrorOnesRolled > 0;

        public int TraumaRollModifier => HorrorOnesRolled;

        public bool Succeeded => SuccessCount >= RequiredSuccesses;
    }
}
