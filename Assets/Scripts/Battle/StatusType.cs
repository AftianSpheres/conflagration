namespace CnfBattleSys
{
    /// <summary>
    /// Enum that determined "which" effect a status is.
    /// This doesn't actually relate to any business logic - DoT, stat bonuses/cuts, etc. are all
    /// handled individually. This is used to determine name and icon and to handle
    /// collisions between different versions of the "same" status.
    /// The advantage of doing things this way is that it makes it very easy for us to stack effects.
    /// </summary>
    public enum StatusType
    {
        None,
        TestBuff,
        TestDebuff
    }
}