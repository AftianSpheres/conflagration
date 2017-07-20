namespace CnfBattleSys
{
    /// <summary>
    /// Flags corresponding to various on/off effects a StatusPAcket can have.
    /// </summary>
    [System.Flags]
    public enum MiscStatusEffectFlags
    {
        DebuffBlock = 1,
        AllDmgBlock = 1 << 1
    }
}