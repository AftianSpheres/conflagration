using UnityEngine;
using System;
using System.Collections.Generic;
using MovementEffects;

namespace CnfBattleSys
{
    /// <summary>
    /// Static class that forms the "brains" of the battle system.
    /// Handles turn order, command execution, inter-battler communication,
    /// win/loss, etc.
    /// </summary>
    public static class BattleOverseer
    {
        public const float battleTickLength = 1 / 60;
        public const float fieldRadius = 50;
        public static BattleData currentBattle { get; private set; }

        /// <summary>
        /// Load datasets as soon as you actually get to do that, but no sooner.
        /// </summary>
        static BattleOverseer ()
        {
            ActionDatabase.Load();
            StanceDatabase.Load(); // stances reference actions
            BattlerDatabase.Load(); // battlers reference stances
            FormationDatabase.Load(); // formations reference battlers
        }

        /// <summary>
        /// Sets up battle based on given formation and starts
        /// executing Battle Shit.
        /// </summary>
        public static void StartBattle (BattleFormation formation)
        {
            currentBattle = new BattleData(formation);
            Timing.RunCoroutine(_WaitForPuppets());
        }

        /// <summary>
        /// Ends the current battle.
        /// </summary>
        public static void EndBattle ()
        {
            currentBattle = null;
        }

        /// <summary>
        /// Coroutine: Wait for all battler puppets to exist before first communication with BattleStage.
        /// </summary>
        static IEnumerator<float> _WaitForPuppets ()
        {
            bool allPuppetsExist = false;
            while (!allPuppetsExist)
            {
                allPuppetsExist = true;
                for (int i = 0; i < currentBattle.allBattlers.Length; i++)
                {
                    if (currentBattle.allBattlers[i].puppet == null) allPuppetsExist = false;
                }
                yield return 0;
            }
            BattleStage.instance.StartOfBattle();
        }

        /// <summary>
        /// Passthrough to CurrentActionExecutionSubsystem.StepSubactions; keeps from exposing BattleOverseer internal structure.
        /// Executes steps subactions from the current action's subaction set, or however many are left.
        /// Returns true if any of the subactions we step through do anything at all.
        /// </summary>
        public static bool StepSubactions (int steps = 1)
        {
            return currentBattle.actionExecutionSubsystem.StepSubactions(steps);
        }

        // Bits and pieces for use within the battle loop

        /// <summary>
        /// Passthrough to CurrentActionExecutionSubsystem.BattlerIsDead; keeps from exposing BattleOverseer internal structure.
        /// The specified battler is dead, so if we're running an action, we
        /// need to remove it from any target lists it might be on.
        /// If it's _using_ an action, we need to stop that entirely.
        /// </summary>
        public static void BattlerIsDead(Battler b)
        {
            currentBattle.actionExecutionSubsystem.BattlerIsDead(b);
        }

        /// <summary>
        /// Passthrough to TurnManagementSubsystem.ExtendCurrentTurn.
        /// So long as something's taking a turn right now, gives that battler a second turn immediately after this one.
        /// </summary>
        internal static void ExtendCurrentTurn()
        {
            currentBattle.turnManagementSubsystem.ExtendCurrentTurn();
        }

        /// <summary>
        /// Passthrough to CurrentActionExecutionSubsystem.FinishCurrentAction; keeps from exposing BattleOverseer internal structure.
        /// Handles all remaining subactions, then stops executing the current action.
        /// Returns true if any subaction does anything.
        /// </summary>
        public static bool FinishCurrentAction()
        {
            return currentBattle.actionExecutionSubsystem.FinishCurrentAction();
        }

        /// <summary>
        /// Passthrough to TurnManagementSubsystem.RequestTurn.
        /// Battler b needs to take a turn as soon as it can be allowed to do so.
        /// </summary>
        public static void RequestTurn(Battler b)
        {
            currentBattle.turnManagementSubsystem.RequestTurn(b);
        }
    }
}