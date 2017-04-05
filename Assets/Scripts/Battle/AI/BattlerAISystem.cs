using System;
using System.Collections.Generic;
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
        private static List<Battler> battlersListBuffer;
        private static List<Battler> alsoBattlersListBuffer; // I am probably way too worried about unnecessary allocations to be doing things this way

        /// <summary>
        /// First-run setup for BattlerAISystem.
        /// </summary>
        public static void FirstRunSetup ()
        {
            battlersListBuffer = new List<Battler>();
            alsoBattlersListBuffer = new List<Battler>();
        }

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

        /// <summary>
        /// Returns a 2D array of battlers.
        /// The first array contains all valid primary targets for the specified battler/action combination.
        /// The second array contains all valid secondary targets for the specified battler/action combination.
        /// If there are no valid targets for either targeting type, the array will be empty.
        /// </summary>
        public static Battler[][] FindLegalTargetsForAction (Battler b, BattleAction battleAction)
        {
            Battler[][] output = new Battler[2][];
            Action<TargetSideFlags> populateList = (targetSideFlags) =>
            {
                battlersListBuffer.Clear();
                alsoBattlersListBuffer.Clear();
                if (targetSideFlags == TargetSideFlags.None) throw new Exception("Can't find legal targets for action " + battleAction.actionID.ToString() + " because it doesn't _have_ legal targets. This is either a special case that shouldn't go through normal target acquisition, something that just shouldn't _be_ executed, or completely broken.");
                if ((targetSideFlags & TargetSideFlags.MySide) == TargetSideFlags.MySide)
                {
                    BattleOverseer.GetBattlersSameSideAs(b.side, ref battlersListBuffer);
                }
                if ((targetSideFlags & TargetSideFlags.MyFriends) == TargetSideFlags.MyFriends)
                {
                    BattleOverseer.GetBattlersAlliedTo_Strict(b.side, ref battlersListBuffer);
                }
                if ((targetSideFlags & TargetSideFlags.MyEnemies) == TargetSideFlags.MyEnemies)
                {
                    BattleOverseer.GetBattlersEnemiesTo(b.side, ref battlersListBuffer);
                }
                if ((targetSideFlags & TargetSideFlags.Neutral) == TargetSideFlags.Neutral)
                {
                    BattleOverseer.GetBattlersEnemiesTo(b.side, ref battlersListBuffer);
                }
                for (int i = 0; i < battlersListBuffer.Count; i++) if (battlersListBuffer[i].IsValidTargetFor(b, battleAction)) alsoBattlersListBuffer.Add(battlersListBuffer[i]);
            };
            populateList(battleAction.targetingSideFlags);
            output[0] = alsoBattlersListBuffer.ToArray();
            if (battleAction.alternateTargetSideFlags != TargetSideFlags.None)
            {
                populateList(battleAction.alternateTargetSideFlags);
                output[1] = alsoBattlersListBuffer.ToArray();
            }
            else output[1] = new Battler[0];
            return output;
        }    
    }
}