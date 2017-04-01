using System;

namespace CnfBattleSys.AI
{
    /// <summary>
    /// Test AI module.
    /// </summary>
    public static class AIModule_TestAI
    {
        /// <summary>
        /// Decides what the test AI does.
        /// This should be _very simple!
        /// </summary>
        public static void DecideTurnActions_AndStanceIfApplicable (Battler b, bool changeStances, out Battler.TurnActions turnActions, out BattlerAIMessageFlags messageFlags)
        {
            if (changeStances) throw new NotImplementedException();
            BattleAction action = b.currentStance.actionSet[UnityEngine.Random.Range(0, b.currentStance.actionSet.Length)];
            throw new NotImplementedException(); // need to do target acquisition tools!
        }
    }
}