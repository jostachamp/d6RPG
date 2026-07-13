namespace ArkhamHorror.Mechanics.Consequences
{
    public sealed class TraumaResolution
    {
        internal TraumaResolution(
            int naturalRoll,
            int horrorOnesModifier,
            int sessionModifier,
            int externalModifier,
            int total,
            TraumaKind kind)
        {
            NaturalRoll = naturalRoll;
            HorrorOnesModifier = horrorOnesModifier;
            SessionModifier = sessionModifier;
            ExternalModifier = externalModifier;
            Total = total;
            Kind = kind;
        }

        public int NaturalRoll { get; }

        public int HorrorOnesModifier { get; }

        public int SessionModifier { get; }

        public int ExternalModifier { get; }

        public int Total { get; }

        public TraumaKind Kind { get; }

        public bool IsFatal => Kind == TraumaKind.LostForever;
    }
}
