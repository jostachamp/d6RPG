namespace ArkhamHorror.Mechanics.Consequences
{
    public sealed class InjuryResolution
    {
        internal InjuryResolution(
            int naturalRoll,
            int existingInjuryModifier,
            int externalModifier,
            int total,
            InjuryKind kind)
        {
            NaturalRoll = naturalRoll;
            ExistingInjuryModifier = existingInjuryModifier;
            ExternalModifier = externalModifier;
            Total = total;
            Kind = kind;
        }

        public int NaturalRoll { get; }

        public int ExistingInjuryModifier { get; }

        public int ExternalModifier { get; }

        public int Total { get; }

        public InjuryKind Kind { get; }

        public bool IsFatal => Kind == InjuryKind.Dead;
    }
}
