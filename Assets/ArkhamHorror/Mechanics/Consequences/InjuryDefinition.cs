namespace ArkhamHorror.Mechanics.Consequences
{
    public sealed class InjuryDefinition
    {
        internal InjuryDefinition(
            InjuryKind kind,
            int healingSuccesses,
            int? naturalHealingWeeks,
            int? poolRecoveryCeiling,
            bool locksPoolLimit,
            bool requiresSense)
        {
            Kind = kind;
            HealingSuccesses = healingSuccesses;
            NaturalHealingWeeks = naturalHealingWeeks;
            PoolRecoveryCeiling = poolRecoveryCeiling;
            LocksPoolLimit = locksPoolLimit;
            RequiresSense = requiresSense;
        }

        public InjuryKind Kind { get; }

        public int HealingSuccesses { get; }

        public int? NaturalHealingWeeks { get; }

        public int? PoolRecoveryCeiling { get; }

        public bool LocksPoolLimit { get; }

        public bool RequiresSense { get; }
    }
}
