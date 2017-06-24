namespace CnfBattleSys
{
    /// <summary>
    /// Flags corresponding to each of the sides or factions or whatever a unit can belong to.
    /// Used to determine which units are allies, enemies, or neutral, relative to the
    /// unit that's acting.
    /// </summary>
    [System.Flags]
    public enum BattlerSideFlags
    {
        None = 0,
        PlayerSide = 1,
        GenericEnemySide = 1 << 1,
        GenericAlliedSide = 1 << 2,
        GenericNeutralSide = 1 << 3,
    }
}