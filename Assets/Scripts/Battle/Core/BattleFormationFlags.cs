namespace CnfBattleSys
{
    /// <summary>
    /// Bitflags attached to a BattleFormation that communicate things miscellaneous.
    /// ex: if I wanted to specify "you can lose this without game over" that's a flag
    /// </summary>
    [System.Flags]
    public enum BattleFormationFlags
    {
        ForbidPlayerFromRunning = 1
    }
}