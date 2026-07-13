using System;

namespace ArkhamHorror.Mechanics.Consequences
{
    public static class TraumaDurationRules
    {
        public static PersonalityEffectDuration ExtendForMindUndone(PersonalityEffectDuration normalDuration)
        {
            switch (normalDuration)
            {
                case PersonalityEffectDuration.UntilEndOfNextTurn:
                    return PersonalityEffectDuration.UntilEndOfScene;
                case PersonalityEffectDuration.UntilEndOfScene:
                case PersonalityEffectDuration.UntilEndOfSession:
                    return PersonalityEffectDuration.UntilEndOfSession;
                default:
                    throw new ArgumentOutOfRangeException(nameof(normalDuration));
            }
        }
    }
}
