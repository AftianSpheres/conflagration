using UnityEngine;
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
        public const float battleTickLength = 1 / 60;
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
        public static float normalizedSpeed { get; private set; }
        public static BattleFormation activeFormation { get; private set; }


        public static List<Battler> allBattlers { get; private set; }
        public static Dictionary<BattlerSideFlags, List<Battler>> battlersBySide { get; private set; } // xzibit.jpg

        /// <summary>
        /// Battlers to give turns when that next becomes possible. Normally there should only actually be one Battler here at a time... but ties are a thing,
        /// and having multiple members in this list gives us an easy way to say "these guys need a tiebreaker."
        /// </summary>
        private static List<Battler> battlersReadyToTakeTurns;
        /// <summary>
        /// Turn order tiebreakers work as follows: every battler is a part of this stack in a random order. When multiple battlers are trying to take their turn at the same time, 
        /// or "tied" in some other sense, we just pop Battlers off the top of battlerTiebreakerStack until we get one of the ones that's trying to act, then rerandomize it.
        /// </summary>
        private static Stack<Battler> battlerTiebreakerStack;


        // Communication between the battle system and "not the battle system"

        /// <summary>
        /// Sets up battle based on given formation and starts
        /// executing Battle Shit.
        /// </summary>
        public static void StartBattle(BattleFormation formation)
        {
            activeFormation = formation;
            for (int b = 0; b < activeFormation.battlers.Length; b++)
            {
                Battler bat = new Battler(activeFormation.battlers[b]);
                allBattlers.Add(bat);
                battlersBySide[bat.side].Add(bat);
            }
        }

        // BattleOverseer state management

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
            battlersBySide = new Dictionary<BattlerSideFlags, List<Battler>>();
            battlersBySide[BattlerSideFlags.PlayerSide] = new List<Battler>();
            battlersBySide[BattlerSideFlags.GenericAlliedSide] = new List<Battler>();
            battlersBySide[BattlerSideFlags.GenericEnemySide] = new List<Battler>();
            battlersBySide[BattlerSideFlags.GenericNeutralSide] = new List<Battler>();
            battlersReadyToTakeTurns = new List<Battler>();
            battlerTiebreakerStack = new Stack<Battler>();
            
        }

        /// <summary>
        /// Resets all battle state info.
        /// You can keep battle state outside of the battle scene, which could
        /// be useful for certain kinds of setpieces, but if you're actually
        /// _ending_ a battle you're expected to call this.
        /// Also, note that the way BattleOverseer works right now precludes
        /// anything that'd require multiple battles to be running concurrently.
        /// You can fight a few turns, go back to the overworld, and pick up
        /// where you left off, but you can't fight a few turns, go back to the
        /// overworld, and start a _different_ encounter without losing the
        /// current state of BattleOverseer.
        /// (If that seems like a relevant problem, introduce a means by which
        /// BattleOverseer can store its state elsewhere and reacquire the
        /// saved state.)
        /// </summary>
        private static void Cleanup()
        {
            activeFormation = null;
            normalizedSpeed = 0;
            allBattlers.Clear();
            battlersBySide[BattlerSideFlags.PlayerSide].Clear();
            battlersBySide[BattlerSideFlags.GenericAlliedSide].Clear();
            battlersBySide[BattlerSideFlags.GenericEnemySide].Clear();
            battlersBySide[BattlerSideFlags.GenericNeutralSide].Clear();
            battlersReadyToTakeTurns.Clear();
            battlerTiebreakerStack.Clear();
        }

        // Bits and pieces for use within the battle loop

        /// <summary>
        /// Uses BattlerTiebreakerStack to break a tie.
        /// Returns the Battler that won.
        /// </summary>
        private static Battler BreakTie (Battler[] tiedBattlers)
        {
            if (tiedBattlers.Length == 0) throw new System.Exception("You're trying to break a tie between no battlers. Protip: ain't nobody gonna win that one."); 
            while (battlerTiebreakerStack.Count > 0)
            {
                Battler b = battlerTiebreakerStack.Pop();
                for (int i = 0; i < tiedBattlers.Length; i++)
                {
                    if (tiedBattlers[i] == b) return b;
                }
            }
            throw new System.Exception("Tried to break a tie, but none of the tiedBattlers were in the battlerTiebreakerStack. That... shouldn't happen.");
        }

        /// <summary>
        /// Derives normalizedSpeed, which is just the mean
        /// of the final speed stats of all living Battlers.
        /// </summary>
        private static void DeriveNormalizedSpeed()
        {
            float nS = 0;
            int c = 0;
            for (int i = 0; i < allBattlers.Count; i++)
            {
                if (!allBattlers[i].isDead) nS += allBattlers[i].stats.Spe;
                c++;
            }
            normalizedSpeed = nS / c; // if c = 0 every battler is dead. so, yeah, /0, but if we're running this with every battler dead something is very very wrong anyway
        }

        /// <summary>
        /// Updates BattleOverseer to reflect that however much time has passed.
        /// This is only accurate to within 1/60 of a second. You'll need to make sure
        /// that you don't call ElapsedTime at intervals smaller than battleTickLength or
        /// things will get sorta weird.
        /// If there's a battler ready to take a turn, returns any time remaining, since we
        /// can't step any further until all turns are taken; otherwise, returns 0.
        /// </summary>
        private static float ElapsedTime (float time)
        {
            float remainingTime = 0;
            while (time - battleTickLength > 0) // If you're calling this at very infrequent intervals it's gonna be kinda slow each time. This should run every frame when the simulation is running, though...
            {
                time -= battleTickLength;
                for (int i = 0; i < allBattlers.Count; i++)
                {
                    allBattlers[i].ElapsedTime(battleTickLength);
                }
                if (battlersReadyToTakeTurns.Count > 0)
                {
                    remainingTime = time; // hold onto the remainder and add it next time you call remainingTime
                    break;
                }
            }
            return remainingTime;
        }

        /// <summary>
        /// Rerandomizes BattlerTiebreakerStack
        /// </summary>
        private static void RandomizeBattlerTiebreakerStack ()
        {
            battlerTiebreakerStack.Clear();
            while (battlerTiebreakerStack.Count < allBattlers.Count)
            {
                int randomIndex = Random.Range(0, allBattlers.Count);
                if (!battlerTiebreakerStack.Contains(allBattlers[randomIndex])) battlerTiebreakerStack.Push(allBattlers[randomIndex]);
            }
        }

        /// <summary>
        /// Battler b needs to take a turn as soon as it can be allowed to do so.
        /// </summary>
        public static void RequestTurn (Battler b)
        {
            battlersReadyToTakeTurns.Add(b);
        }
    }
}