using System;

namespace ArkhamHorror.Mechanics.Consequences
{
    public sealed class InsightPool
    {
        public const int MaximumLimit = 10;

        public InsightPool(int limit, int current)
        {
            if (limit < 0 || limit > MaximumLimit)
            {
                throw new ArgumentOutOfRangeException(nameof(limit));
            }

            if (current < 0 || current > limit)
            {
                throw new ArgumentOutOfRangeException(nameof(current));
            }

            Limit = limit;
            Current = current;
        }

        public int Limit { get; private set; }

        public int Current { get; private set; }

        public void RefillForSession()
        {
            Current = Limit;
        }

        public bool TrySpend(int amount)
        {
            ValidatePositive(amount, nameof(amount));
            if (Current < amount)
            {
                return false;
            }

            Current -= amount;
            return true;
        }

        public void Gain(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            Current = (int)Math.Min(Limit, (long)Current + amount);
        }

        public void IncreaseLimit(int amount)
        {
            ValidatePositive(amount, nameof(amount));
            Limit = (int)Math.Min(MaximumLimit, (long)Limit + amount);
        }

        public bool TrySacrificeLimitToAvoidRemoval()
        {
            if (Limit < 2)
            {
                return false;
            }

            Limit -= 2;
            Current = Math.Min(Current, Limit);
            return true;
        }

        private static void ValidatePositive(int amount, string parameterName)
        {
            if (amount < 1)
            {
                throw new ArgumentOutOfRangeException(parameterName, "The amount must be positive.");
            }
        }
    }
}
