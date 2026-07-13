namespace ArkhamHorror.Mechanics.Consequences
{
    public sealed class InjuryInstance
    {
        internal InjuryInstance(int id, InjuryKind kind, SenseKind? lostSense)
        {
            Id = id;
            Kind = kind;
            LostSense = lostSense;
            TemporaryEffectActive = kind == InjuryKind.HeavyBlow;
            DireTreatmentPending = kind == InjuryKind.Dire;
        }

        public int Id { get; }

        public InjuryKind Kind { get; }

        public SenseKind? LostSense { get; }

        public bool TemporaryEffectActive { get; internal set; }

        public bool DireTreatmentPending { get; internal set; }
    }
}
