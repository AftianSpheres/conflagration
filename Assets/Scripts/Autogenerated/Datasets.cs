// ------------------------------------------------------------------------------
//  <autogenerated>
//      This code was generated by a tool.
//      Mono Runtime Version: 2.0.50727.1433
// 
//      Changes to this file may cause incorrect behavior and will be lost if 
//      the code is regenerated.
//  </autogenerated>
// ------------------------------------------------------------------------------

namespace GeneratedDatasets {
    
    /// <summary> Static class containing misc. datasets. Automatically generated. </summary>
    public static class Datasets {
        /// <summary> BattleAction definition lookup table. Automatically generated. Basically an unreadable mess, but that's ok - use ActionDatabase.Get() to grab entries out of this. Don't try to work out what's what yourself! </summary>
        public static CnfBattleSys.BattleAction[] battleActions = new CnfBattleSys.BattleAction[] {
                new CnfBattleSys.BattleAction(null, null, null, CnfBattleSys.ActionType.None, 0F, 0F, 0F, 0F, 0F, 0, CnfBattleSys.TargetSideFlags.None, CnfBattleSys.TargetSideFlags.None, CnfBattleSys.ActionTargetType.None, CnfBattleSys.ActionTargetType.None, CnfBattleSys.BattleActionCategoryFlags.None, Util.PopulateDictWith(new string[0], new CnfBattleSys.BattleAction.Subaction[0])),
                new CnfBattleSys.BattleAction(null, null, null, CnfBattleSys.ActionType.TestMeleeAtk_OneHit, 0F, 1F, 3F, 0F, 1F, 15, CnfBattleSys.TargetSideFlags.None, CnfBattleSys.TargetSideFlags.Neutral, CnfBattleSys.ActionTargetType.None, CnfBattleSys.ActionTargetType.SingleTarget, CnfBattleSys.BattleActionCategoryFlags.Heal, Util.PopulateDictWith(new string[] {
                                "atk0"}, new CnfBattleSys.BattleAction.Subaction[] {
                                new CnfBattleSys.BattleAction.Subaction(null, 30, 1F, false, CnfBattleSys.LogicalStatType.Stat_ATK, CnfBattleSys.LogicalStatType.Stat_DEF, CnfBattleSys.LogicalStatType.Stat_HIT, CnfBattleSys.LogicalStatType.Stat_EVA, "", "", "", CnfBattleSys.BattleActionCategoryFlags.Heal, new CnfBattleSys.BattleAction.Subaction.EffectPackage[0], CnfBattleSys.DamageTypeFlags.Slash)})),
                new CnfBattleSys.BattleAction(null, null, null, CnfBattleSys.ActionType.TestMeleeAtk_3XCombo, 0F, 0F, 0F, 0F, 0F, 0, CnfBattleSys.TargetSideFlags.None, CnfBattleSys.TargetSideFlags.None, CnfBattleSys.ActionTargetType.None, CnfBattleSys.ActionTargetType.None, CnfBattleSys.BattleActionCategoryFlags.None, Util.PopulateDictWith(new string[] {
                                "",
                                "",
                                ""}, new CnfBattleSys.BattleAction.Subaction[] {
                                new CnfBattleSys.BattleAction.Subaction(null, 0, 0F, false, CnfBattleSys.LogicalStatType.None, CnfBattleSys.LogicalStatType.None, CnfBattleSys.LogicalStatType.None, CnfBattleSys.LogicalStatType.None, "", "", "", CnfBattleSys.BattleActionCategoryFlags.None, new CnfBattleSys.BattleAction.Subaction.EffectPackage[0], CnfBattleSys.DamageTypeFlags.None),
                                new CnfBattleSys.BattleAction.Subaction(null, 0, 0F, false, CnfBattleSys.LogicalStatType.None, CnfBattleSys.LogicalStatType.None, CnfBattleSys.LogicalStatType.None, CnfBattleSys.LogicalStatType.None, "", "", "", CnfBattleSys.BattleActionCategoryFlags.None, new CnfBattleSys.BattleAction.Subaction.EffectPackage[0], CnfBattleSys.DamageTypeFlags.None),
                                new CnfBattleSys.BattleAction.Subaction(null, 0, 0F, false, CnfBattleSys.LogicalStatType.None, CnfBattleSys.LogicalStatType.None, CnfBattleSys.LogicalStatType.None, CnfBattleSys.LogicalStatType.None, "", "", "", CnfBattleSys.BattleActionCategoryFlags.None, new CnfBattleSys.BattleAction.Subaction.EffectPackage[0], CnfBattleSys.DamageTypeFlags.None)})),
                new CnfBattleSys.BattleAction(null, null, null, CnfBattleSys.ActionType.TestMeleeAtk_Knockback, 0F, 0F, 0F, 0F, 0F, 0, CnfBattleSys.TargetSideFlags.None, CnfBattleSys.TargetSideFlags.None, CnfBattleSys.ActionTargetType.None, CnfBattleSys.ActionTargetType.None, CnfBattleSys.BattleActionCategoryFlags.None, Util.PopulateDictWith(new string[0], new CnfBattleSys.BattleAction.Subaction[0])),
                new CnfBattleSys.BattleAction(null, null, null, CnfBattleSys.ActionType.TestRangedAtk_LineOfSight, 0F, 0F, 0F, 0F, 0F, 0, CnfBattleSys.TargetSideFlags.None, CnfBattleSys.TargetSideFlags.None, CnfBattleSys.ActionTargetType.None, CnfBattleSys.ActionTargetType.None, CnfBattleSys.BattleActionCategoryFlags.None, Util.PopulateDictWith(new string[0], new CnfBattleSys.BattleAction.Subaction[0])),
                new CnfBattleSys.BattleAction(null, null, null, CnfBattleSys.ActionType.TestRangedAtk_AOE, 0F, 0F, 0F, 0F, 0F, 0, CnfBattleSys.TargetSideFlags.None, CnfBattleSys.TargetSideFlags.None, CnfBattleSys.ActionTargetType.None, CnfBattleSys.ActionTargetType.None, CnfBattleSys.BattleActionCategoryFlags.None, Util.PopulateDictWith(new string[0], new CnfBattleSys.BattleAction.Subaction[0])),
                new CnfBattleSys.BattleAction(null, null, null, CnfBattleSys.ActionType.TestRangedAtk_AllFoesAndDOTEffect, 0F, 0F, 0F, 0F, 0F, 0, CnfBattleSys.TargetSideFlags.None, CnfBattleSys.TargetSideFlags.None, CnfBattleSys.ActionTargetType.None, CnfBattleSys.ActionTargetType.None, CnfBattleSys.BattleActionCategoryFlags.None, Util.PopulateDictWith(new string[0], new CnfBattleSys.BattleAction.Subaction[0])),
                new CnfBattleSys.BattleAction(null, null, null, CnfBattleSys.ActionType.TestMeleeAtk_StaminaDmg, 0F, 0F, 0F, 0F, 0F, 0, CnfBattleSys.TargetSideFlags.None, CnfBattleSys.TargetSideFlags.None, CnfBattleSys.ActionTargetType.None, CnfBattleSys.ActionTargetType.None, CnfBattleSys.BattleActionCategoryFlags.None, Util.PopulateDictWith(new string[0], new CnfBattleSys.BattleAction.Subaction[0])),
                new CnfBattleSys.BattleAction(null, null, null, CnfBattleSys.ActionType.TestRangedAtk_StaminaDmg, 0F, 0F, 0F, 0F, 0F, 0, CnfBattleSys.TargetSideFlags.None, CnfBattleSys.TargetSideFlags.None, CnfBattleSys.ActionTargetType.None, CnfBattleSys.ActionTargetType.None, CnfBattleSys.BattleActionCategoryFlags.None, Util.PopulateDictWith(new string[0], new CnfBattleSys.BattleAction.Subaction[0])),
                new CnfBattleSys.BattleAction(null, null, null, CnfBattleSys.ActionType.TestHeal, 0F, 0F, 0F, 0F, 0F, 0, CnfBattleSys.TargetSideFlags.None, CnfBattleSys.TargetSideFlags.None, CnfBattleSys.ActionTargetType.None, CnfBattleSys.ActionTargetType.None, CnfBattleSys.BattleActionCategoryFlags.None, Util.PopulateDictWith(new string[0], new CnfBattleSys.BattleAction.Subaction[0])),
                new CnfBattleSys.BattleAction(null, null, null, CnfBattleSys.ActionType.TestBuff_DamageOutputUp, 0F, 0F, 0F, 0F, 0F, 0, CnfBattleSys.TargetSideFlags.None, CnfBattleSys.TargetSideFlags.None, CnfBattleSys.ActionTargetType.None, CnfBattleSys.ActionTargetType.None, CnfBattleSys.BattleActionCategoryFlags.None, Util.PopulateDictWith(new string[0], new CnfBattleSys.BattleAction.Subaction[0])),
                new CnfBattleSys.BattleAction(null, null, null, CnfBattleSys.ActionType.TestDebuff_Slow, 0F, 0F, 0F, 0F, 0F, 0, CnfBattleSys.TargetSideFlags.None, CnfBattleSys.TargetSideFlags.None, CnfBattleSys.ActionTargetType.None, CnfBattleSys.ActionTargetType.None, CnfBattleSys.BattleActionCategoryFlags.None, Util.PopulateDictWith(new string[0], new CnfBattleSys.BattleAction.Subaction[0])),
                new CnfBattleSys.BattleAction(null, null, null, CnfBattleSys.ActionType.TestCounter, 0F, 0F, 0F, 0F, 0F, 0, CnfBattleSys.TargetSideFlags.None, CnfBattleSys.TargetSideFlags.None, CnfBattleSys.ActionTargetType.None, CnfBattleSys.ActionTargetType.None, CnfBattleSys.BattleActionCategoryFlags.None, Util.PopulateDictWith(new string[0], new CnfBattleSys.BattleAction.Subaction[0]))};
    }
}
