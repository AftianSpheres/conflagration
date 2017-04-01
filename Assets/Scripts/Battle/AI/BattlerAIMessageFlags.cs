namespace CnfBattleSys
{
    /// <summary>
    /// Bitflags the AI system uses to pass additional information (separate from turnACtions) to the Battler.
    /// </summary>
    [System.Flags]
    public enum BattlerAIMessageFlags
    {
        None = 0,
        ExtendTurn = 1,
        ForbidMovementOnNextTurn = 1 << 1
    }
}