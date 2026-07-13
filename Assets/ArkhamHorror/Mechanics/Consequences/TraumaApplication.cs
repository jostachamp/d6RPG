namespace ArkhamHorror.Mechanics.Consequences
{
    public sealed class TraumaApplication
    {
        internal TraumaApplication(
            TraumaKind kind,
            bool requestedChoiceHonored,
            int insightSpent,
            bool triggersNegativePersonality,
            bool extendsNegativePersonalityDuration,
            int poolDiceDiscarded,
            bool removalAvoided)
        {
            Kind = kind;
            RequestedChoiceHonored = requestedChoiceHonored;
            InsightSpent = insightSpent;
            TriggersNegativePersonality = triggersNegativePersonality;
            ExtendsNegativePersonalityDuration = extendsNegativePersonalityDuration;
            PoolDiceDiscarded = poolDiceDiscarded;
            RemovalAvoided = removalAvoided;
        }

        public TraumaKind Kind { get; }

        public bool RequestedChoiceHonored { get; }

        public int InsightSpent { get; }

        public bool TriggersNegativePersonality { get; }

        public bool ExtendsNegativePersonalityDuration { get; }

        public int PoolDiceDiscarded { get; }

        public bool RemovalAvoided { get; }
    }
}
