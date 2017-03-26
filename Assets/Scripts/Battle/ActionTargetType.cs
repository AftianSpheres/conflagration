namespace CnfBattleSys
{
    /// <summary>
    /// The targeting type associated with a specific action.
    /// Used to determine how we acquire targets before executing the action.
    /// </summary>
    public enum ActionTargetType
    {
        None,
        SingleTarget,
        AllTargetsInRange,
        CircularAOE,
        AllTargetsAlongLinearCorridor,
        Self
    }
}