namespace CnfBattleSys
{
    /// <summary>
    /// Flags defining the relative groupings of factions an action is allowed to target.
    /// </summary>
    [System.Flags]
    public enum TargetSideFlags
    {
        None = 0,
        MySide = 1,
        MyFriends = 1 << 1,
        MyEnemies = 1 << 2,
        Neutral = 1 << 3
    }
}