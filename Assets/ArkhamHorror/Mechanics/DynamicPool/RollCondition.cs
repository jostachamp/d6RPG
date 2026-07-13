using System;

namespace ArkhamHorror.Mechanics.DynamicPool
{
    [Flags]
    public enum RollCondition
    {
        None = 0,
        Advantage = 1,
        Disadvantage = 2
    }
}
