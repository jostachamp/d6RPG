using System;
using ArkhamHorror.Mechanics.DynamicPool;

namespace ArkhamHorror.Mechanics.Consequences
{
    public static class RecoveryService
    {
        public static void HealDamage(DicePool dicePool, InjuryState injuries, int amount)
        {
            Validate(dicePool, injuries);
            dicePool.HealDamage(amount, injuries.GetPoolRecoveryCeiling(dicePool.Maximum));
        }

        public static void RecoverAllDamageNaturally(DicePool dicePool, InjuryState injuries)
        {
            Validate(dicePool, injuries);
            dicePool.HealAllDamage(injuries.GetPoolRecoveryCeiling(dicePool.Maximum));
        }

        public static void Strain(DicePool dicePool, InjuryState injuries)
        {
            Validate(dicePool, injuries);
            dicePool.Strain(injuries.GetPoolRecoveryCeiling(dicePool.Maximum));
        }

        public static bool ApplyIntrospection(DicePool dicePool, bool actionSucceeded)
        {
            return ApplyOnePointHorrorRecovery(dicePool, actionSucceeded);
        }

        public static bool ApplyCounseling(DicePool dicePool, bool actionSucceeded)
        {
            return ApplyOnePointHorrorRecovery(dicePool, actionSucceeded);
        }

        public static bool RecoverHorrorAfterSafeWeek(DicePool dicePool, bool horrorIncreasedDuringWeek)
        {
            if (dicePool == null)
            {
                throw new ArgumentNullException(nameof(dicePool));
            }

            if (horrorIncreasedDuringWeek)
            {
                return false;
            }

            bool changed = dicePool.HorrorLimit > 0;
            dicePool.ReduceHorror(int.MaxValue);
            return changed;
        }

        private static bool ApplyOnePointHorrorRecovery(DicePool dicePool, bool actionSucceeded)
        {
            if (dicePool == null)
            {
                throw new ArgumentNullException(nameof(dicePool));
            }

            if (!actionSucceeded || dicePool.HorrorLimit == 0)
            {
                return false;
            }

            dicePool.ReduceHorror(1);
            return true;
        }

        private static void Validate(DicePool dicePool, InjuryState injuries)
        {
            if (dicePool == null)
            {
                throw new ArgumentNullException(nameof(dicePool));
            }

            if (injuries == null)
            {
                throw new ArgumentNullException(nameof(injuries));
            }
        }
    }
}
