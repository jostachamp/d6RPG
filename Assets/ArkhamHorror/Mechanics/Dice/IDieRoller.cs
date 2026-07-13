namespace ArkhamHorror.Mechanics.Dice
{
    /// <summary>
    /// Randomness boundary for reproducible simulations and tests.
    /// </summary>
    public interface IDieRoller
    {
        int RollD6();
    }
}
