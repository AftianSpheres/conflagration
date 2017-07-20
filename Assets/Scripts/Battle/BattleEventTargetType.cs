/// <summary>
/// Targets that an AnimEvent, AudioEvent, or FXEvent can be applied to.
/// </summary>
[System.Flags]
public enum BattleEventTargetType
{
    /// <summary>
    /// If you're dispatching an event _directly_ instead of as part of an event block,
    /// set targetType to None. It doesn't really matter but it's a helpful convention.
    /// </summary>
    User = 1,
    PrimaryTargets = 1 << 1,
    SecondaryTargets = 1 << 2,
    Stage = 1 << 3,
}