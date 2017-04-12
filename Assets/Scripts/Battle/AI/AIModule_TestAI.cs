using System;

namespace CnfBattleSys.AI
{
    /// <summary>
    /// Test AI module.
    /// </summary>
    public static class AIModule_TestAI
    {
        const float stanceChangeDifficulty = 2.5f;

        /// <summary>
        /// Decides what the test AI does.
        /// This should be _very simple!
        /// </summary>
        public static void DecideTurnActions_AndStanceIfApplicable (Battler b, bool changeStances, out Battler.TurnActions turnActions, out BattlerAIMessageFlags messageFlags)
        {
            if (changeStances)
            {
                turnActions = new Battler.TurnActions(false, 0, new Battler[0], new Battler[0], ActionDatabase.SpecialActions.selfStanceBreakAction, b.currentStance);
                messageFlags = BattlerAIMessageFlags.None;
            }
            else
            {
                turnActions = BattlerAISystem.GetOptimumActionsForTurn(b.aiFlags, b, stanceChangeDifficulty);
                messageFlags = BattlerAIMessageFlags.None;
            }
        }
    }
}