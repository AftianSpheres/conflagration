namespace CnfBattleSys
{
    /// <summary>
    /// Bitflags that modify the behavior of the various basal battler AI modes.
    /// </summary>
    [System.Flags]
    public enum BattlerAIFlags
    {
        None = 0,
        DetermineActionsAtRandom = 1,
        ResistanceAware = 1 << 1,
        WeaknessAware = 1 << 2,
        EvadeAware = 1 << 3
    }
}