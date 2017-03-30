using System.Collections.Generic;

namespace CnfBattleSys
{
    /// <summary>
    /// Static class that forms the "brains" of the battle system.
    /// Handles turn order, command execution, inter-battler communication,
    /// win/loss, etc.
    /// </summary>
    public static class BattleOverseer
    {
        /// <summary>
        /// Speed stats determine the delay units acquire after acting.
        /// Less speed = more delay.
        /// normalizedSpeed is the mean speed of all living units in the current
        /// battle, recalculated at the beginning of each turn taken.
        /// Base delay values are divided by the unit's speed factor - its speed 
        /// stat expressed as a proportion of normalizedSpeed - to determine
        /// the delay that's actually applied.
        /// For example: if the battle's normalizedSpeed is 200, and the acting
        /// battler's speed is 300 after applying all bonuses and modifiers,
        /// then all delay values applied to that battler will be divided by 1.5.
        /// A delay of 60.0 will be reduced to 40.0!
        /// Conversely, if the unit's speed is only 100, the speed factor will be 0.5,
        /// and a base delay of 60.0 would translate to a real delay of 120.0.
        /// </summary>
        public static int normalizedSpeed { get; private set; }

        public static List<Battler> allBattlers { get; private set; }
        public static Dictionary<BattlerSideFlags, Battler> battlersBySide { get; private set; }

        /// <summary>
        /// Initializes the BattleOverseer and loads in the various datasets the battle system uses.
        /// </summary>
        public static void FirstRunSetup ()
        {
            if (allBattlers != null) throw new System.Exception("BattleOverseer.FirstRunSetup can't be called more than once!");
            ActionDatabase.Load();
            StanceDatabase.Load(); // stances reference actions
            BattlerDatabase.Load(); // battlers reference stances
            FormationDatabase.Load(); // formations reference battlers
            allBattlers = new List<Battler>();
            battlersBySide = new Dictionary<BattlerSideFlags, Battler>();
        }
    }
}