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

        // Bits and pieces for use within the battle loop

        /// <summary>
        /// Passthrough to TurnManagementSubsystem.ExtendCurrentTurn.
        /// So long as something's taking a turn right now, gives that battler a second turn immediately after this one.
        /// </summary>
        internal static void ExtendCurrentTurn()
        {
            currentBattle.turnManagementSubsystem.ExtendCurrentTurn();
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