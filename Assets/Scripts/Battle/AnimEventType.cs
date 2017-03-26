namespace CnfBattleSys
{
    /// <summary>
    /// Enum specifying animation events that battlers can be told to process at various points during the battle.
    /// </summary>
    public enum AnimEventType
    {
        None,
        TestAnim_OnHit,
        TestAnim_OnUse,
        TestStance_Idle,
        TestStance_Move,
        TestStance_Hit,
        TestStance_Break,
        TestStance_Dodge
    }
}