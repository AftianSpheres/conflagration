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
            Action callbackFromConstructors;
            currentBattle = new BattleData(formation, out callbackFromConstructors);
            callbackFromConstructors();
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
    }
}