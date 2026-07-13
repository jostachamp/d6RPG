using System;

namespace ArkhamHorror.Mechanics.DynamicPool
{
    /// <summary>
    /// Tracks regular and horror dice independently while exposing their combined total.
    /// </summary>
    public sealed class DicePool
    {
        public const int DefaultPlayerMaximum = 6;

        public DicePool(int maximum)
            : this(maximum, maximum, maximum)
        {
        }

        public DicePool(int maximum, int limit, int availableDice)
            : this(maximum, limit, availableDice, 0, 0)
        {
        }

        public DicePool(int maximum, int limit, int regularDice, int horrorDice, int horrorLimit)
            : this(maximum, limit, regularDice, horrorDice, horrorLimit, 0)
        {
        }

        public DicePool(
            int maximum,
            int limit,
            int regularDice,
            int horrorDice,
            int horrorLimit,
            int pendingStrainInjuries)
        {
            if (maximum < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maximum), "A dice pool maximum must be positive.");
            }

            if (limit < 0 || limit > maximum)
            {
                throw new ArgumentOutOfRangeException(nameof(limit), "A dice pool limit must be between zero and its maximum.");
            }

            if (regularDice < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(regularDice), "Available regular dice cannot be negative.");
            }

            if (horrorDice < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(horrorDice), "Available horror dice cannot be negative.");
            }

            if (horrorLimit < 0 || horrorLimit > maximum)
            {
                throw new ArgumentOutOfRangeException(nameof(horrorLimit), "A horror dice limit must be between zero and the pool maximum.");
            }

            if (pendingStrainInjuries < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pendingStrainInjuries));
            }

            Maximum = maximum;
            Limit = limit;
            RegularDice = regularDice;
            HorrorDice = horrorDice;
            HorrorLimit = horrorLimit;
            PendingStrainInjuries = pendingStrainInjuries;
        }

        public int Maximum { get; }

        public int Limit { get; private set; }

        public int RegularDice { get; private set; }

        public int HorrorDice { get; private set; }

        public int HorrorLimit { get; private set; }

        public int PendingStrainInjuries { get; private set; }

        public int AvailableDice => checked(RegularDice + HorrorDice);

        public bool IsWounded => Limit == 0;

        public static DicePool CreatePlayerPool()
        {
            return new DicePool(DefaultPlayerMaximum);
        }

        public void Spend(int count)
        {
            if (HorrorDice > 0)
            {
                throw new InvalidOperationException("Select regular and horror dice explicitly when the pool contains horror dice.");
            }

            Spend(new DiceSelection(count, 0));
        }

        public void Spend(DiceSelection selection)
        {
            int count = selection.Total;
            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "At least one die must be spent.");
            }

            if (selection.RegularDice > RegularDice || selection.HorrorDice > HorrorDice)
            {
                throw new InvalidOperationException("The pool does not contain enough dice.");
            }

            RegularDice -= selection.RegularDice;
            HorrorDice -= selection.HorrorDice;
        }

        public void Add(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "The number of dice added cannot be negative.");
            }

            checked
            {
                RegularDice += count;
            }
        }

        public void Refill()
        {
            RefillBy(Math.Max(0, Limit - AvailableDice));
        }

        public void RefillBy(int count)
        {
            ValidateNonNegative(count, nameof(count));
            int openSlots = Math.Min(count, Math.Max(0, Limit - AvailableDice));
            int missingHorrorDice = Math.Max(0, HorrorLimit - HorrorDice);
            int horrorDiceToAdd = Math.Min(openSlots, missingHorrorDice);
            HorrorDice += horrorDiceToAdd;
            openSlots -= horrorDiceToAdd;

            if (openSlots > 0)
            {
                RegularDice += openSlots;
            }
        }

        public void SufferHorror(int amount)
        {
            ValidateNonNegative(amount, nameof(amount));
            HorrorLimit = (int)Math.Min(Maximum, (long)HorrorLimit + amount);
        }

        public void ReduceHorror(int amount)
        {
            ValidateNonNegative(amount, nameof(amount));
            HorrorLimit = Math.Max(0, HorrorLimit - amount);
        }

        public void SufferDamage(int amount)
        {
            ValidateNonNegative(amount, nameof(amount));
            if (amount == 0)
            {
                return;
            }

            Limit = (int)Math.Max(0, (long)Limit - amount);

            int excessDice = Math.Max(0, AvailableDice - Limit);
            Discard(excessDice);
        }

        public void HealDamage(int amount)
        {
            HealDamage(amount, Maximum);
        }

        public void HealDamage(int amount, int recoveryCeiling)
        {
            ValidateNonNegative(amount, nameof(amount));
            ValidateRecoveryCeiling(recoveryCeiling);
            int effectiveMaximum = Math.Min(Maximum, recoveryCeiling);
            int healedLimit = (int)Math.Min(effectiveMaximum, (long)Limit + amount);
            Limit = Math.Max(Limit, healedLimit);
        }

        public void HealAllDamage()
        {
            HealAllDamage(Maximum);
        }

        public void HealAllDamage(int recoveryCeiling)
        {
            ValidateRecoveryCeiling(recoveryCeiling);
            Limit = Math.Max(Limit, Math.Min(Maximum, recoveryCeiling));
        }

        public void Discard(int count)
        {
            ValidateNonNegative(count, nameof(count));
            int remaining = Math.Min(count, AvailableDice);

            int regularToDiscard = Math.Min(RegularDice, remaining);
            RegularDice -= regularToDiscard;
            remaining -= regularToDiscard;

            HorrorDice -= Math.Min(HorrorDice, remaining);
        }

        public void Strain()
        {
            Strain(Maximum);
        }

        public void Strain(int recoveryCeiling)
        {
            ValidateRecoveryCeiling(recoveryCeiling);
            int restoredLimit = Math.Min(Maximum, recoveryCeiling);
            if (Limit >= restoredLimit)
            {
                throw new InvalidOperationException("A character can strain only when doing so would restore their pool limit.");
            }

            Limit = restoredLimit;
            PendingStrainInjuries = checked(PendingStrainInjuries + 1);
        }

        public bool ResolvePendingStrainInjury()
        {
            if (PendingStrainInjuries == 0)
            {
                return false;
            }

            PendingStrainInjuries--;
            return true;
        }

        private static void ValidateNonNegative(int value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, "The amount cannot be negative.");
            }
        }

        private static void ValidateRecoveryCeiling(int recoveryCeiling)
        {
            if (recoveryCeiling < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(recoveryCeiling));
            }
        }
    }
}
