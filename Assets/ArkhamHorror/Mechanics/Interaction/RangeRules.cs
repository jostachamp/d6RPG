using System;

namespace ArkhamHorror.Mechanics.Interaction
{
    public static class RangeRules
    {
        public const int EngagementDistanceFeet = 5;

        public static bool IsEngaged(int distanceFeet, bool easilyAccessible)
        {
            if (distanceFeet < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(distanceFeet));
            }

            return easilyAccessible && distanceFeet <= EngagementDistanceFeet;
        }

        public static int FiveFootIncrementsFor(int distanceFeet)
        {
            if (distanceFeet < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(distanceFeet));
            }

            return checked((distanceFeet + 4) / 5);
        }
    }
}
