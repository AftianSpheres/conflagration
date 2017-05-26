namespace CnfBattleSys
{
    /// <summary>
    /// Valid action IDs.
    /// </summary>
    public enum ActionType
    {
        INTERNAL_BreakOwnStance = -2,
        InvalidAction = -1,
        None = 0,
        TestMeleeAtk_OneHit,
        TestMeleeAtk_3XCombo,
        TestMeleeAtk_Knockback,
        TestRangedAtk_LineOfSight,
        TestRangedAtk_AOE,
        TestRangedAtk_AllFoesAndDOTEffect,
        TestMeleeAtk_StaminaDmg,
        TestRangedAtk_StaminaDmg,
        TestHeal,
        TestBuff_DamageOutputUp,
        TestDebuff_Slow,
        TestCounter
    }
}