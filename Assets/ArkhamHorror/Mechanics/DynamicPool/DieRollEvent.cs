using ArkhamHorror.Mechanics.Dice;

namespace ArkhamHorror.Mechanics.DynamicPool
{
    public readonly struct DieRollEvent
    {
        public DieRollEvent(int dieIndex, DieKind kind, int naturalResult, bool isReroll)
        {
            DieIndex = dieIndex;
            Kind = kind;
            NaturalResult = naturalResult;
            IsReroll = isReroll;
        }

        public int DieIndex { get; }

        public DieKind Kind { get; }

        public int NaturalResult { get; }

        public bool IsReroll { get; }
    }
}
