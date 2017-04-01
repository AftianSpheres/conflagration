using System;
using CnfBattleSys.AI;

namespace CnfBattleSys
{
    /// <summary>
    /// Static class that contains all of the different functions that Battlers can (depending on their AI type)
    /// use to determine what they're gonna do.
    /// If changeStances is true, we decide on a stance, too.
    /// AI-driven units select stance and actions at the same time; player-controlled units
    /// have to select a stance before we can talk about what they're gonna do, since
    /// we're waiting on player input in that case and the options we give the player
    /// are a function of the stance the unit is in.
    /// </summary>
    public static class BattlerAISystem
    {
        /// <summary>
        /// Starts the AI on deciding what the unit should do.
        /// Thia is basically just an intermediary that goes between the Battler and
        /// the individual AI modules, so as to keep the (already pretty gnarly) Battler
        /// object from having to deal with the additional headache of managing this
        /// huge-ass jump table. (Well it's not huge _yet_ but, architecturally: this
        /// table is fat af.)
        /// </summary>
        public static void StartThinking (Battler b, bool changeStances)
        {
            Battler.TurnActions turnActions = Battler.defaultTurnActions;
            BattlerAIMessageFlags messageFlags = BattlerAIMessageFlags.None;
            bool outputIsDelayed = false; // if the module we're running can't return output immediately, either because it requires player input or because we're doing something inane and unsafe with the AI itself
            switch (b.aiType)
            {
                case BattlerAIType.None:
                    break; // if the battler says it doesn't have any AI, we dutifully decline to provide the battler with AI
                case BattlerAIType.PlayerSide_ManualControl:
                    outputIsDelayed = true;
                    throw new NotImplementedException();
                case BattlerAIType.TestAI:
                    AIModule_TestAI.DecideTurnActions_AndStanceIfApplicable(b, changeStances, out turnActions, out messageFlags);
                    break;
                default:
                    throw new Exception("No entry in AI function jump table for AI type of: " + b.aiType.ToString());
            }
            if (outputIsDelayed) throw new NotImplementedException(); // this should start a coroutine that waits for the AI or player "AI" to finish, then calls b.ReceiveAThought whenever that's done
            else b.ReceiveAThought(turnActions, messageFlags);
        }
    }
}