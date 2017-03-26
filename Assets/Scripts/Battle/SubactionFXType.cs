namespace CnfBattleSys
{
    /// <summary>
    /// The various types of (non-damage/non-healing) effects a Subaction can have on its targets.
    /// Subactions can have as many FXPackages associated with them as they want, so
    /// simple and granular is ideal here. An FXType is One Thing.
    /// </summary>
    public enum SubactionFXType
    {
        None,
        PushTargetBackward,
        Buff_STR
    }
}