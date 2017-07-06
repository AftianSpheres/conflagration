/// <summary>
/// Targets that an AnimEvent, AudioEvent, or FXEvent can be applied to.
/// </summary>
[System.Flags]
public enum BattleEventTargetType
{
    None = 0,
    User = 1,
    PrimaryTargets = 1 << 1,
    SecondaryTargets = 1 << 2,
    Stage = 1 << 3
}