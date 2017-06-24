namespace CnfBattleSys
{
    /// <summary>
    /// Flags corresponding to various on/off effects a StatusPAcket can have.
    /// </summary>
    [System.Flags]
    public enum MiscStatusEffectFlags
    {
        None = 0,
        DebuffBlock = 1,
        AllDmgBlock = 1 << 1
    }
}