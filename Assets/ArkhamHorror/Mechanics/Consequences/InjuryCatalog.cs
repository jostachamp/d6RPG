using System;

namespace ArkhamHorror.Mechanics.Consequences
{
    public static class InjuryCatalog
    {
        public static InjuryDefinition Get(InjuryKind kind)
        {
            switch (kind)
            {
                case InjuryKind.HeavyBlow:
                case InjuryKind.Slowed:
                case InjuryKind.Concussed:
                case InjuryKind.InjuredArm:
                case InjuryKind.InjuredLeg:
                case InjuryKind.Burned:
                case InjuryKind.Choking:
                case InjuryKind.Sickened:
                    return new InjuryDefinition(kind, 1, 1, null, false, false);
                case InjuryKind.NastyCut:
                    return new InjuryDefinition(kind, 1, 1, 3, false, false);
                case InjuryKind.LossOfSense:
                    return new InjuryDefinition(kind, 2, 2, null, false, true);
                case InjuryKind.SeverelyInjured:
                    return new InjuryDefinition(kind, 2, 2, null, false, false);
                case InjuryKind.Comatose:
                    return new InjuryDefinition(kind, 2, 2, 0, true, false);
                case InjuryKind.Dire:
                    return new InjuryDefinition(kind, 3, null, 0, true, false);
                case InjuryKind.Dead:
                    return new InjuryDefinition(kind, int.MaxValue, null, 0, true, false);
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        public static InjuryKind ResolveTableResult(int total)
        {
            if (total < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(total), "An injury-table result must be at least 1.");
            }

            return total >= 11 ? InjuryKind.Dead : (InjuryKind)total;
        }

        public static bool IsUniqueEnvironmentalInjury(InjuryKind kind)
        {
            return kind == InjuryKind.Burned || kind == InjuryKind.Choking || kind == InjuryKind.Sickened;
        }
    }
}
