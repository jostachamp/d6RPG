namespace ArkhamHorror.Mechanics.Dice
{
    public readonly struct DieRoll
    {
        public DieRoll(DieKind kind, int naturalResult)
        {
            Kind = kind;
            NaturalResult = naturalResult;
        }

        public DieKind Kind { get; }

        public int NaturalResult { get; }
    }
}
