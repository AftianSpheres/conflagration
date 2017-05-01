namespace CnfBattleSys
{
    /// <summary>
    /// Events that a Battler can pass to its puppet in order to
    /// prompt UI updates for HP bars and whatnot.
    /// </summary>
    public enum BattlerUIEventType
    {
        None,
        HPValueChange,
        StaminaValueChange,
        StanceChange
    }
}