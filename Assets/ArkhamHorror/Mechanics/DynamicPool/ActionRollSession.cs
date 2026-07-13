using System;
using System.Collections.Generic;
using ArkhamHorror.Mechanics.Dice;

namespace ArkhamHorror.Mechanics.DynamicPool
{
    /// <summary>
    /// A staged roll that permits sourced rerolls before post-roll result removal.
    /// </summary>
    public sealed class ActionRollSession
    {
        private readonly IDieRoller dieRoller;
        private readonly DieRoll[] rolls;
        private readonly bool[] keptResults;
        private readonly List<DieRollEvent> rollEvents;
        private readonly IReadOnlyList<DieRoll> currentRolls;
        private readonly ActionKind actionKind;
        private readonly SkillLevel skillLevel;
        private readonly ActionDifficulty difficulty;
        private readonly RollCondition rollCondition;
        private readonly int dieResultModifier;
        private int remainingRerolls;
        private int remainingSpecialResultRemovals;

        internal ActionRollSession(
            IDieRoller dieRoller,
            DieRoll[] rolls,
            ActionKind actionKind,
            SkillLevel skillLevel,
            ActionDifficulty difficulty,
            RollCondition rollCondition,
            int dieResultModifier,
            int rerollsAllowed,
            int specialResultRemovalsAllowed)
        {
            this.dieRoller = dieRoller ?? throw new ArgumentNullException(nameof(dieRoller));
            this.rolls = rolls ?? throw new ArgumentNullException(nameof(rolls));
            this.actionKind = actionKind;
            this.skillLevel = skillLevel;
            this.difficulty = difficulty;
            this.rollCondition = rollCondition;
            this.dieResultModifier = dieResultModifier;
            remainingRerolls = rerollsAllowed;
            remainingSpecialResultRemovals = specialResultRemovalsAllowed;

            keptResults = new bool[rolls.Length];
            rollEvents = new List<DieRollEvent>(rolls.Length);
            for (int index = 0; index < rolls.Length; index++)
            {
                keptResults[index] = true;
                rollEvents.Add(new DieRollEvent(index, rolls[index].Kind, rolls[index].NaturalResult, false));
            }

            currentRolls = Array.AsReadOnly(rolls);
            Phase = ActionRollPhase.Rolled;
        }

        public ActionRollPhase Phase { get; private set; }

        public IReadOnlyList<DieRoll> CurrentRolls => currentRolls;

        public int RemainingRerolls => remainingRerolls;

        public int RemainingSpecialResultRemovals => remainingSpecialResultRemovals;

        public bool IsResultKept(int dieIndex)
        {
            ValidateIndex(dieIndex);
            return keptResults[dieIndex];
        }

        public void Reroll(int dieIndex)
        {
            RequirePhase(ActionRollPhase.Rolled, "Dice may be rerolled only before result removal begins.");
            ValidateIndex(dieIndex);

            if (remainingRerolls == 0)
            {
                throw new InvalidOperationException("No granted rerolls remain for this action.");
            }

            DieRoll current = rolls[dieIndex];
            if (current.Kind == DieKind.Horror && current.NaturalResult == 1)
            {
                throw new InvalidOperationException("A horror die that generated a result of 1 cannot be rerolled.");
            }

            int rerolledResult = dieRoller.RollD6();
            ComplexActionResolver.ValidateNaturalResult(rerolledResult);
            rolls[dieIndex] = new DieRoll(current.Kind, rerolledResult);
            rollEvents.Add(new DieRollEvent(dieIndex, current.Kind, rerolledResult, true));
            remainingRerolls--;
        }

        public void ApplyRollCondition()
        {
            RequirePhase(ActionRollPhase.Rolled, "Roll conditions can be applied only once after rerolls.");

            if ((rollCondition & RollCondition.Advantage) != 0)
            {
                keptResults[FindExtremeIndex(findHighest: false)] = false;
            }

            if ((rollCondition & RollCondition.Disadvantage) != 0)
            {
                keptResults[FindExtremeIndex(findHighest: true)] = false;
            }

            Phase = ActionRollPhase.ResultRemoval;
        }

        public void RemoveResult(int dieIndex)
        {
            RequirePhase(ActionRollPhase.ResultRemoval, "Special result removal occurs after rerolls and roll-condition removal.");
            ValidateIndex(dieIndex);

            if (remainingSpecialResultRemovals == 0)
            {
                throw new InvalidOperationException("No granted special result removals remain for this action.");
            }

            if (!keptResults[dieIndex])
            {
                throw new InvalidOperationException("That die result has already been removed.");
            }

            keptResults[dieIndex] = false;
            remainingSpecialResultRemovals--;
        }

        public ComplexActionResult Complete()
        {
            if (Phase == ActionRollPhase.Rolled)
            {
                ApplyRollCondition();
            }

            RequirePhase(ActionRollPhase.ResultRemoval, "This roll session has already been completed.");

            var keptRolls = new List<DieRoll>(rolls.Length);
            for (int index = 0; index < rolls.Length; index++)
            {
                if (keptResults[index])
                {
                    keptRolls.Add(rolls[index]);
                }
            }

            Phase = ActionRollPhase.Completed;
            return ComplexActionResolver.EvaluateRolls(
                (DieRoll[])rolls.Clone(),
                keptRolls.ToArray(),
                rollEvents.ToArray(),
                actionKind,
                skillLevel,
                difficulty,
                dieResultModifier);
        }

        private int FindExtremeIndex(bool findHighest)
        {
            int selectedIndex = -1;
            for (int index = 0; index < rolls.Length; index++)
            {
                if (!keptResults[index])
                {
                    continue;
                }

                if (selectedIndex < 0
                    || (findHighest && rolls[index].NaturalResult > rolls[selectedIndex].NaturalResult)
                    || (!findHighest && rolls[index].NaturalResult < rolls[selectedIndex].NaturalResult))
                {
                    selectedIndex = index;
                }
            }

            if (selectedIndex < 0)
            {
                throw new InvalidOperationException("No die result remains to remove.");
            }

            return selectedIndex;
        }

        private void ValidateIndex(int dieIndex)
        {
            if (dieIndex < 0 || dieIndex >= rolls.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(dieIndex));
            }
        }

        private void RequirePhase(ActionRollPhase requiredPhase, string message)
        {
            if (Phase != requiredPhase)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
