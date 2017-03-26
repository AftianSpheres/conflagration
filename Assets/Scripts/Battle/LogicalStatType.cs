namespace CnfBattleSys
{
    /// <summary>
    /// Enum specifying the various stats (including "virtual stats" produced by finding the average of various primary stats)
    /// that are plugged into the various in-battle formulas for damage calculation, evasion, hit rates, etc.
    /// </summary>
    public enum LogicalStatType
    {
        None,
        Stat_ATK,
        Stat_DEF,
        Stat_MATK,
        Stat_MDEF,
        Stat_SPE,
        Stat_HIT,
        Stat_EVA,
        Stats_ATKDEF,
        Stats_ATKMATK,
        Stats_MATKMDEF,
        Stats_DEFMDEF,
        Stats_ATKSPE,
        Stats_MATKSPE,
        Stats_ATKHIT,
        Stats_MATKHIT,
        Stats_DEFEVA,
        Stats_MDEFEVA,
        Stats_All,
        Stat_MaxHP,
        Stat_CurrentSP
    }
}
