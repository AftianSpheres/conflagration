namespace CnfBattleSys
{
    /// <summary>
    /// Enum specifying animation events that battlers can be told to process at various points during the battle.
    /// </summary>
    public enum AnimEventType
    {
        None,
        StanceBreak,
        Dodge,
        Idle,
        Move,
        Hit,
        Heal,
        Die,
        TestAnim_OnHit,
        TestAnim_OnUse,
    }
}