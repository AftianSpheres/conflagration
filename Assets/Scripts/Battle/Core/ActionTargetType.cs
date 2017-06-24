namespace CnfBattleSys
{
    /// <summary>
    /// The targeting type associated with a specific action.
    /// Used to determine how we acquire targets before executing the action.
    /// </summary>
    public enum ActionTargetType : byte
    {
        None,
        SingleTarget,
        AllTargetsInRange,
        CircularAOE,
        LineOfSightPiercing,
        LineOfSightSingle,
        Self
    }
}