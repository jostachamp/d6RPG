using System;
using System.Collections.Generic;
using ArkhamHorror.Mechanics.Dice;

namespace ArkhamHorror.Mechanics.DynamicPool
{
    public sealed class ComplexActionResolver
    {
        private readonly IDieRoller dieRoller;

        public ComplexActionResolver(IDieRoller dieRoller)
        {
            this.dieRoller = dieRoller ?? throw new ArgumentNullException(nameof(dieRoller));
        }

        public ComplexActionResult Perform(
            DicePool pool,
            int diceFromPool,
            SkillLevel skillLevel,
            ActionDifficulty difficulty = ActionDifficulty.Standard,
            int additionalHandDice = 0,
            int dieResultModifier = 0)
        {
            if (pool != null && pool.HorrorDice > 0)
            {
                throw new InvalidOperationException("Use a DiceSelection when the pool contains horror dice.");
            }

            return Begin(
                pool,
                new DiceSelection(diceFromPool, 0),
                skillLevel,
                difficulty,
                RollCondition.None,
                additionalHandDice,
                dieResultModifier).Complete();
        }

        public ComplexActionResult Perform(
            DicePool pool,
            DiceSelection poolDice,
            SkillLevel skillLevel,
            ActionDifficulty difficulty = ActionDifficulty.Standard,
            RollCondition rollCondition = RollCondition.None,
            int additionalHandDice = 0,
            int dieResultModifier = 0)
        {
            return Begin(
                pool,
                poolDice,
                skillLevel,
                difficulty,
                rollCondition,
                additionalHandDice,
                dieResultModifier).Complete();
        }

        public ComplexActionResult PerformReaction(
            DicePool pool,
            DiceSelection poolDie,
            SkillLevel skillLevel,
            RollCondition rollCondition = RollCondition.None,
            int additionalHandDice = 0,
            int dieResultModifier = 0)
        {
            return BeginReaction(
                pool,
                poolDie,
                skillLevel,
                rollCondition,
                additionalHandDice,
                dieResultModifier).Complete();
        }

        public ActionRollSession Begin(
            DicePool pool,
            DiceSelection poolDice,
            SkillLevel skillLevel,
            ActionDifficulty difficulty = ActionDifficulty.Standard,
            RollCondition rollCondition = RollCondition.None,
            int additionalHandDice = 0,
            int dieResultModifier = 0,
            int rerollsAllowed = 0,
            int specialResultRemovalsAllowed = 0,
            DiceSelection preRollDiceRemoved = default(DiceSelection))
        {
            return BeginAction(
                pool,
                poolDice,
                ActionKind.Complex,
                skillLevel,
                difficulty,
                rollCondition,
                additionalHandDice,
                dieResultModifier,
                rerollsAllowed,
                specialResultRemovalsAllowed,
                preRollDiceRemoved);
        }

        public ActionRollSession BeginReaction(
            DicePool pool,
            DiceSelection poolDie,
            SkillLevel skillLevel,
            RollCondition rollCondition = RollCondition.None,
            int additionalHandDice = 0,
            int dieResultModifier = 0,
            int rerollsAllowed = 0,
            int specialResultRemovalsAllowed = 0,
            DiceSelection preRollDiceRemoved = default(DiceSelection))
        {
            if (poolDie.Total != 1)
            {
                throw new ArgumentOutOfRangeException(nameof(poolDie), "A reaction must spend exactly one die from the character's pool.");
            }

            return BeginAction(
                pool,
                poolDie,
                ActionKind.Reaction,
                skillLevel,
                ActionDifficulty.Standard,
                rollCondition,
                additionalHandDice,
                dieResultModifier,
                rerollsAllowed,
                specialResultRemovalsAllowed,
                preRollDiceRemoved);
        }

        private ActionRollSession BeginAction(
            DicePool pool,
            DiceSelection poolDice,
            ActionKind actionKind,
            SkillLevel skillLevel,
            ActionDifficulty difficulty,
            RollCondition rollCondition,
            int additionalHandDice,
            int dieResultModifier,
            int rerollsAllowed,
            int specialResultRemovalsAllowed,
            DiceSelection preRollDiceRemoved)
        {
            if (pool == null)
            {
                throw new ArgumentNullException(nameof(pool));
            }

            ValidateSkillLevel(skillLevel);
            ValidateDifficulty(difficulty);
            ValidateRollCondition(rollCondition);

            if (poolDice.Total < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(poolDice), "A complex action must spend at least one pool die.");
            }

            if (additionalHandDice < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(additionalHandDice), "Additional hand dice cannot be negative.");
            }

            if (rerollsAllowed < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rerollsAllowed));
            }

            if (specialResultRemovalsAllowed < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(specialResultRemovalsAllowed));
            }

            if (poolDice.RegularDice > pool.RegularDice || poolDice.HorrorDice > pool.HorrorDice)
            {
                throw new InvalidOperationException("The pool does not contain enough dice for this action.");
            }

            int conditionDice = 0;
            if ((rollCondition & RollCondition.Advantage) != 0)
            {
                conditionDice++;
            }

            if ((rollCondition & RollCondition.Disadvantage) != 0)
            {
                conditionDice++;
            }

            int extraRegularHandDice = checked(additionalHandDice + conditionDice);
            int regularHandDice = checked(poolDice.RegularDice + extraRegularHandDice);
            int horrorHandDice = poolDice.HorrorDice;

            if (preRollDiceRemoved.RegularDice > regularHandDice
                || preRollDiceRemoved.HorrorDice > horrorHandDice)
            {
                throw new InvalidOperationException("The hand does not contain enough dice of the selected kind to remove before rolling.");
            }

            int extraRegularDiceRemoved = Math.Min(extraRegularHandDice, preRollDiceRemoved.RegularDice);
            extraRegularHandDice -= extraRegularDiceRemoved;
            int poolRegularDiceRemoved = preRollDiceRemoved.RegularDice - extraRegularDiceRemoved;
            int poolRegularHandDice = poolDice.RegularDice - poolRegularDiceRemoved;
            horrorHandDice -= preRollDiceRemoved.HorrorDice;
            int handSize = checked(poolRegularHandDice + horrorHandDice + extraRegularHandDice);
            pool.Spend(poolDice);

            var rolls = new DieRoll[handSize];
            int rollIndex = 0;

            for (int index = 0; index < poolRegularHandDice; index++)
            {
                rolls[rollIndex++] = Roll(DieKind.Regular);
            }

            for (int index = 0; index < horrorHandDice; index++)
            {
                rolls[rollIndex++] = Roll(DieKind.Horror);
            }

            while (rollIndex < rolls.Length)
            {
                rolls[rollIndex++] = Roll(DieKind.Regular);
            }

            return new ActionRollSession(
                dieRoller,
                rolls,
                actionKind,
                skillLevel,
                difficulty,
                rollCondition,
                dieResultModifier,
                rerollsAllowed,
                specialResultRemovalsAllowed);
        }

        public static ComplexActionResult Evaluate(
            IReadOnlyList<int> dieResults,
            SkillLevel skillLevel,
            ActionDifficulty difficulty = ActionDifficulty.Standard,
            int dieResultModifier = 0)
        {
            if (dieResults == null)
            {
                throw new ArgumentNullException(nameof(dieResults));
            }

            if (dieResults.Count < 1)
            {
                throw new ArgumentException("At least one die result is required.", nameof(dieResults));
            }

            ValidateSkillLevel(skillLevel);
            ValidateDifficulty(difficulty);

            var rolls = new DieRoll[dieResults.Count];
            var rollEvents = new DieRollEvent[dieResults.Count];
            for (int index = 0; index < dieResults.Count; index++)
            {
                rolls[index] = new DieRoll(DieKind.Regular, dieResults[index]);
                rollEvents[index] = new DieRollEvent(index, DieKind.Regular, dieResults[index], false);
            }

            return EvaluateRolls(
                rolls,
                rolls,
                rollEvents,
                ActionKind.Complex,
                skillLevel,
                difficulty,
                dieResultModifier);
        }

        internal static ComplexActionResult EvaluateRolls(
            DieRoll[] allRolls,
            DieRoll[] keptRolls,
            DieRollEvent[] rollEvents,
            ActionKind actionKind,
            SkillLevel skillLevel,
            ActionDifficulty difficulty,
            int dieResultModifier)
        {
            int successCount = 0;
            int horrorOnesRolled = 0;
            int threshold = (int)skillLevel;

            for (int index = 0; index < allRolls.Length; index++)
            {
                ValidateNaturalResult(allRolls[index].NaturalResult);
            }

            for (int index = 0; index < rollEvents.Length; index++)
            {
                ValidateNaturalResult(rollEvents[index].NaturalResult);
                if (rollEvents[index].Kind == DieKind.Horror && rollEvents[index].NaturalResult == 1)
                {
                    horrorOnesRolled++;
                }
            }

            for (int index = 0; index < keptRolls.Length; index++)
            {
                int naturalResult = keptRolls[index].NaturalResult;

                long modifiedResult = (long)naturalResult + dieResultModifier;
                bool isSuccess = naturalResult == 6
                    || (naturalResult != 1 && modifiedResult >= threshold);

                if (isSuccess)
                {
                    successCount++;
                }
            }

            return new ComplexActionResult(
                allRolls,
                keptRolls,
                rollEvents,
                actionKind,
                successCount,
                difficulty,
                horrorOnesRolled);
        }

        private DieRoll Roll(DieKind kind)
        {
            int naturalResult = dieRoller.RollD6();
            ValidateNaturalResult(naturalResult);
            return new DieRoll(kind, naturalResult);
        }

        internal static void ValidateNaturalResult(int naturalResult)
        {
            if (naturalResult < 1 || naturalResult > 6)
            {
                throw new ArgumentOutOfRangeException(nameof(naturalResult), "Every result must be from 1 through 6.");
            }
        }

        private static void ValidateSkillLevel(SkillLevel skillLevel)
        {
            if (!Enum.IsDefined(typeof(SkillLevel), skillLevel))
            {
                throw new ArgumentOutOfRangeException(nameof(skillLevel));
            }
        }

        private static void ValidateDifficulty(ActionDifficulty difficulty)
        {
            if (!Enum.IsDefined(typeof(ActionDifficulty), difficulty))
            {
                throw new ArgumentOutOfRangeException(nameof(difficulty));
            }
        }

        private static void ValidateRollCondition(RollCondition rollCondition)
        {
            const RollCondition allDefined = RollCondition.Advantage | RollCondition.Disadvantage;
            if ((rollCondition & ~allDefined) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rollCondition));
            }
        }
    }
}
