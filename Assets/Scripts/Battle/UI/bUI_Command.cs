/// <summary>
/// Commands that other parts of the battle UI can pass to BattleUIController.
/// </summary>
public enum bUI_Command
{
    None,
    Back,
    Decide_AttackPrimary,
    Decide_AttackSecondary,
    Break,
    Move,
    Run,
    CloseWheel,
    Decide_Stance,
    WheelFromTopLevel
}