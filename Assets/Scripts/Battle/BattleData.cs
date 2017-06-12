using System;
using System.Collections.Generic;
using UnityEngine;

namespace CnfBattleSys
{
    /// <summary>
    /// Contains data for a single battle.
    /// Make one when you start a battle, throw it away when
    /// the battle is over.
    /// </summary>
    public class BattleData
    {
        /// <summary>
        /// Battle overseer state values.
        /// </summary>
        public enum State
        {
            None,
            Offline,
            BetweenTurns,
            WaitingForInput,
            ExecutingAction,
            Paused,
            BattleWon,
            BattleLost
        }
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
        public float normalizedSpeed { get; private set; }
        public ActionExecutionSubsystem actionExecutionSubsystem { get; private set; }
        public TurnManagementSubsystem turnManagementSubsystem { get; private set; }
        public BattleFormation activeFormation { get; private set; }
        public Battler currentTurnBattler { get; private set; }
        public Battler[] allBattlers { get; private set; }
        public Dictionary<BattlerSideFlags, Battler[]> battlersBySide; // xzibit.jpg
        public State state { get; private set; }
        private readonly static int layerMask = Animator.StringToHash("BATTLE_Battlers");

        /// <summary>
        /// Turn order tiebreakers work as follows: every battler is a part of this stack in a random order. When multiple battlers are trying to take their turn at the same time, 
        /// or "tied" in some other sense, we just pop Battlers off the top of battlerTiebreakerStack until we get one of the ones that's trying to act, then rerandomize it.
        /// </summary>
        private Stack<Battler> battlerTiebreakerStack;

        /// <summary>
        /// Change overseer state, if allowed.
        /// </summary>
        public void ChangeState(State _state)
        {
            switch (state)
            {
                case State.Offline:
                case State.BetweenTurns:
                case State.ExecutingAction:
                case State.WaitingForInput:
                    if (_state == State.Offline) Util.Crash(new System.Exception("Can't change state back to offline!"));
                    break;
                case State.BattleWon:
                case State.BattleLost:
                    if (_state != State.Offline) Util.Crash(new System.Exception("Can only change state to offline after end of battle"));
                    break;
                default:
                    break;
            }
            state = _state;
        }

        /// <summary>
        /// Initializes the BattleOverseer and loads in the various datasets the battle system uses.
        /// </summary>
        public BattleData(BattleFormation formation)
        {
            activeFormation = formation;
            state = State.Offline;
            battlersBySide = new Dictionary<BattlerSideFlags, Battler[]>();
            battlerTiebreakerStack = new Stack<Battler>();
            BattlerSideFlags[] sides = (BattlerSideFlags[])Enum.GetValues(typeof(BattlerSideFlags));
            Battler[] battlers = new Battler[activeFormation.battlers.Length];
            int[] sideCounts = new int[sides.Length];
            for (int b = 0; b < activeFormation.battlers.Length; b++)
            {
                GenerateBattlerFromFormationMember(activeFormation.battlers[b], b, ref battlers, ref sides, ref sideCounts);
            }
            allBattlers = battlers;
            for (int s = 0; s < sides.Length; s++)
            {
                battlersBySide[sides[s]] = new Battler[sideCounts[s]];
                for (int b = 0, bS = 0; b < allBattlers.Length && bS < sideCounts[s]; b++)
                {
                    if (allBattlers[b].side == sides[s])
                    {
                        battlersBySide[sides[s]][bS] = allBattlers[b];
                        bS++;
                    }
                }
            }
            DeriveNormalizedSpeed();
            for (int b = 0; b < allBattlers.Length; b++) allBattlers[b].ApplyDelay(1.0f); // Applying the starting delay of 1.0 gives each battler something to apply its speed factor to, and lets us build starting turn order
            actionExecutionSubsystem = new ActionExecutionSubsystem(this);
            turnManagementSubsystem = new TurnManagementSubsystem(this);
        }

        /// <summary>
        /// Advances the battle simulation by one "step."
        /// The way this is intended to be used, basically, is that BattleStage calls BattleStep() whenever it doesn't have any animation events
        /// to process, goes through all of those, and then calls BattleStep() again.
        /// </summary>
        public void BattleStep()
        {
            if (!CheckIfBattleAlreadyOver()) switch (state)
                {
                    case State.Paused:
                        Util.Crash(new Exception("Can't advance battle state: battle is paused."));
                        break;
                    case State.Offline:
                        Util.Crash(new Exception("Can't advance battle state: battle system is offline."));
                        break;
                    case State.BetweenTurns:
                        BetweenTurns();
                        if (turnManagementSubsystem.ReadyToTakeATurn()) turnManagementSubsystem.StartTurn();
                        break;
                    case State.WaitingForInput:
                        Util.Crash(new Exception("Can't advance battle state: waiting for player input."));
                        break;
                    case State.ExecutingAction:
                        actionExecutionSubsystem.StepSubactions(1);
                        break;
                    case State.BattleWon:
                        Util.Crash(new Exception("Can't advance battle state: battle is already won."));
                        break;
                    case State.BattleLost:
                        Util.Crash(new Exception("Can't advance battle state: battle is already lost."));
                        break;
                    default:
                        Util.Crash(new Exception("Can't advance battle state: invalid overseer state " + state.ToString()));
                        break;
                }
        }

        /// <summary>
        /// Derives normalizedSpeed, which is just the mean
        /// of the final speed stats of all living Battlers.
        /// </summary>
        public void DeriveNormalizedSpeed()
        {
            float nS = 0;
            int c = 0;
            for (int i = 0; i < allBattlers.Length; i++)
            {
                if (!allBattlers[i].isDead) nS += allBattlers[i].stats.Spe;
                c++;
            }
            normalizedSpeed = nS / c; // if c = 0 every battler is dead. so, yeah, /0, but if we're running this with every battler dead something is very very wrong anyway
        }

        /// <summary>
        /// Generates a Battler based on the given formation member and attaches a puppet gameobject to it.
        /// </summary>
        private void GenerateBattlerFromFormationMember(BattleFormation.FormationMember formationMember, int index, ref Battler[] battlers, ref BattlerSideFlags[] sides, ref int[] sideCounts)
        {
            Battler battler = new Battler(formationMember, this, index);
            battlers[index] = battler;
            for (int s = 0; s < sides.Length; s++)
            {
                if (battler.side == sides[s])
                {
                    sideCounts[s]++;
                    break;
                }
            }
        }

        /// <summary>
        /// Returns true if we've either won or lost.
        /// </summary>
        private bool CheckIfBattleAlreadyOver()
        {
            if (CheckIfBattleWon())
            {
                Debug.Log("You're winner!");
                ChangeState(State.BattleWon);
                return true;
            }
            else if (CheckIfBattleLost())
            {
                Debug.Log("You're loser!");
                ChangeState(State.BattleLost);
                return true;
            }
            else return false;
        }

        /// <summary>
        /// Called between turns - handles delay logic and updates battlers so that they can request turns.
        /// </summary>
        private void BetweenTurns()
        {
            if (!CheckIfBattleAlreadyOver())
            {
                float lowestDelay = float.MaxValue;
                for (int i = 0; i < allBattlers.Length; i++)
                {
                    if (allBattlers[i].currentDelay < lowestDelay) lowestDelay = allBattlers[i].currentDelay;
                }
                for (int i = 0; i < allBattlers.Length; i++) allBattlers[i].BetweenTurns(lowestDelay);
            }
        }

        /// <summary>
        /// Uses BattlerTiebreakerStack to break a tie.
        /// Returns the Battler that won.
        /// </summary>
        private Battler BreakTie(Battler[] tiedBattlers)
        {
            if (tiedBattlers.Length == 0) Util.Crash(new Exception("You're trying to break a tie between no battlers. Protip: ain't nobody gonna win that one."));
            while (battlerTiebreakerStack.Count > 0)
            {
                Battler b = battlerTiebreakerStack.Pop();
                for (int i = 0; i < tiedBattlers.Length; i++)
                {
                    if (tiedBattlers[i] == b) return b;
                }
            }
            Util.Crash(new Exception("Tried to break a tie, but none of the tiedBattlers were in the battlerTiebreakerStack. That... shouldn't happen."));
            return default(Battler);
        }

        /// <summary>
        /// Returns true if no enemy units are still alive.
        /// </summary>
        private bool CheckIfBattleWon()
        {
            int liveEnemiesCount = 0;
            Battler[] enemies = GetBattlersEnemiesTo(BattlerSideFlags.PlayerSide);
            for (int i = 0; i < enemies.Length; i++) if (!enemies[i].isDead) liveEnemiesCount++;
            return liveEnemiesCount < 1;
        }

        /// <summary>
        /// Returns true if no player units are still alive.
        /// </summary>
        private bool CheckIfBattleLost()
        {
            int livePlayersCount = 0;
            for (int i = 0; i < battlersBySide[BattlerSideFlags.PlayerSide].Length; i++) if (!battlersBySide[BattlerSideFlags.PlayerSide][i].isDead) livePlayersCount++;
            return livePlayersCount < 1;
        }

        /// <summary>
        /// Rerandomizes BattlerTiebreakerStack
        /// </summary>
        private void RandomizeBattlerTiebreakerStack()
        {
            battlerTiebreakerStack.Clear();
            while (battlerTiebreakerStack.Count < allBattlers.Length)
            {
                int randomIndex = UnityEngine.Random.Range(0, allBattlers.Length);
                if (!battlerTiebreakerStack.Contains(allBattlers[randomIndex])) battlerTiebreakerStack.Push(allBattlers[randomIndex]);
            }
        }

        /// <summary>
        /// Gets all Battlers that are considered allies of a battler of side side.
        /// This includes those of the battler's own side - call GetBattlersAlliedTo_Strict
        /// if you want, specifically, allies of a _different_ side.
        /// If strict == true, this excludes those of the battler's own side.
        /// </summary>
        public Battler[] GetBattlersAlliedTo(BattlerSideFlags side, bool strict = false)
        {
            int count = 0;
            Battler[] output = new Battler[0];
            int lastIndex = 0;
            Action<BattlerSideFlags> addSide = (_side) => { for (int i = 0; i < battlersBySide[_side].Length; i++) { output[lastIndex] = battlersBySide[_side][i]; lastIndex++; } };
            Action<BattlerSideFlags> countSide = (_side) => { for (int i = 0; i < battlersBySide[_side].Length; i++) count++; };
            switch (side)
            {
                case BattlerSideFlags.PlayerSide:
                    if (!strict) countSide(BattlerSideFlags.PlayerSide);
                    countSide(BattlerSideFlags.GenericAlliedSide);
                    output = new Battler[count];
                    if (!strict) addSide(BattlerSideFlags.PlayerSide);
                    addSide(BattlerSideFlags.GenericAlliedSide);
                    break;
                case BattlerSideFlags.GenericAlliedSide:
                    countSide(BattlerSideFlags.PlayerSide);
                    if (!strict) countSide(BattlerSideFlags.GenericAlliedSide);
                    output = new Battler[count];
                    addSide(BattlerSideFlags.PlayerSide);
                    if (!strict) addSide(BattlerSideFlags.GenericAlliedSide);
                    break;
                case BattlerSideFlags.GenericEnemySide:
                    if (!strict)
                    {
                        countSide(BattlerSideFlags.GenericEnemySide);
                        output = new Battler[count];
                        addSide(BattlerSideFlags.GenericEnemySide);
                    }
                    break;
                case BattlerSideFlags.GenericNeutralSide:
                    break;
                default:
                    Util.Crash(new Exception("Tried to find allies of side " + side + ", but it wasn't in the table."));
                    break;
            }
            return output;
        }

        /// <summary>
        /// Gets all battlers by the order they're going to take their turns in.
        /// This is actually just a more readable shortcut for GetBattlersBySimulatedTurnOrder(-1)
        /// </summary>
        public Battler[] GetBattlersByTurnOrder()
        {
            return GetBattlersBySimulatedTurnOrder(-1);
        }

        /// <summary>
        /// Gets all battlers by the order they're going to take their turns in,
        /// plus an additional entry representing the next turn of the current acting battler
        /// if it has a delay of prospectiveDelay.
        /// If prospectiveDelay is less than 0, returns just the current turn order.
        /// </summary>
        public Battler[] GetBattlersBySimulatedTurnOrder(float prospectiveDelay)
        {
            bool[] skips = new bool[allBattlers.Length];
            Battler[] output;
            int skipCount = 0;
            for (int i = 0; i < allBattlers.Length; i++) if (allBattlers[i].isDead) { skips[i] = true; skipCount++; }
            if (prospectiveDelay >= 0) output = new Battler[allBattlers.Length + 1 - skipCount];
            else
            {
                output = new Battler[allBattlers.Length - skipCount];
                skips[turnManagementSubsystem.currentTurnBattler.index] = true; // We generate one placement for the battler in this instance. If we have a prospective delay, we don't set the skip value yet... 
            }
            output[0] = turnManagementSubsystem.currentTurnBattler;
            int lastFilledIndex = 0;
            for (int i = 0; i < turnManagementSubsystem.battlersReadyToTakeTurns.Count; i++)
            {
                output[i + 1] = turnManagementSubsystem.battlersReadyToTakeTurns[i];
                skips[output[i + 1].index] = true;
                lastFilledIndex++;
            }
            while (lastFilledIndex + 1 < output.Length)
            {
                Battler foundBattler = null;
                float lowestDelay = float.MaxValue;
                for (int b = 0; b < allBattlers.Length; b++)
                {
                    if (skips[b] == true) continue;
                    Battler battler = allBattlers[b];
                    float delay;
                    if (battler == turnManagementSubsystem.currentTurnBattler) delay = prospectiveDelay;
                    else delay = battler.currentDelay;
                    if (delay < lowestDelay)
                    {
                        foundBattler = battler;
                        lowestDelay = delay;
                    }
                }
                if (foundBattler == null) { Util.Crash("Didn't find a battler during GetBattlersBySimulatedTurnOrder, which... shouldn't happen, ever."); break; }
                output[lastFilledIndex] = foundBattler;
                skips[foundBattler.index] = true;
                lastFilledIndex++;
            }
            return output;
        }

        /// <summary>
        /// Gets all Battlers that are considered enemies of a battler of side side.
        /// </summary>
        public Battler[] GetBattlersEnemiesTo(BattlerSideFlags side)
        {
            int count = 0;
            Battler[] output = new Battler[0];
            int lastIndex = 0;
            Action<BattlerSideFlags> addSide = (_side) => { for (int i = 0; i < battlersBySide[_side].Length; i++) { output[lastIndex] = battlersBySide[_side][i]; lastIndex++; } };
            Action<BattlerSideFlags> countSide = (_side) => { for (int i = 0; i < battlersBySide[_side].Length; i++) count++; };
            switch (side)
            {
                case BattlerSideFlags.PlayerSide:
                case BattlerSideFlags.GenericAlliedSide:
                    countSide(BattlerSideFlags.GenericEnemySide);
                    output = new Battler[count];
                    addSide(BattlerSideFlags.GenericEnemySide);
                    break;
                case BattlerSideFlags.GenericEnemySide:
                    countSide(BattlerSideFlags.PlayerSide);
                    countSide(BattlerSideFlags.GenericAlliedSide);
                    output = new Battler[count];
                    addSide(BattlerSideFlags.PlayerSide);
                    addSide(BattlerSideFlags.GenericAlliedSide);
                    break;
                case BattlerSideFlags.GenericNeutralSide:
                    break;
                default:
                    Util.Crash(new Exception("Tried to find enemies of side " + side + ", but it wasn't in the table."));
                    break;
            }
            return output;
        }

        /// <summary>
        /// Gets all Battlers that are considered neutral to a battler of side side.
        /// </summary>
        public Battler[] GetBattlersNeutralTo(BattlerSideFlags side)
        {
            int count = 0;
            Battler[] output = new Battler[0];
            int lastIndex = 0;
            Action<BattlerSideFlags> addSide = (_side) => { for (int i = 0; i < battlersBySide[_side].Length; i++) { output[lastIndex] = battlersBySide[_side][i]; lastIndex++; } };
            Action<BattlerSideFlags> countSide = (_side) => { for (int i = 0; i < battlersBySide[_side].Length; i++) count++; };
            switch (side)
            {
                case BattlerSideFlags.PlayerSide:
                case BattlerSideFlags.GenericAlliedSide:
                case BattlerSideFlags.GenericEnemySide:
                    countSide(BattlerSideFlags.GenericNeutralSide);
                    output = new Battler[count];
                    addSide(BattlerSideFlags.GenericNeutralSide);
                    break;
                case BattlerSideFlags.GenericNeutralSide:
                    countSide(BattlerSideFlags.PlayerSide);
                    countSide(BattlerSideFlags.GenericAlliedSide);
                    countSide(BattlerSideFlags.GenericEnemySide);
                    output = new Battler[count];
                    addSide(BattlerSideFlags.PlayerSide);
                    addSide(BattlerSideFlags.GenericAlliedSide);
                    addSide(BattlerSideFlags.GenericEnemySide);
                    break;
                default:
                    Util.Crash(new Exception("Tried to find neutrals for side " + side + ", but it wasn't in the table."));
                    break;
            }
            return output;
        }

        /// <summary>
        /// Gets all Battlers that are the same side as a battler of side side.
        /// Side. Side side side sidddeeee.
        /// Whose side is this side? My side, your side, side's side, side side side.
        /// </summary>
        public Battler[] GetBattlersSameSideAs(BattlerSideFlags side)
        {
            return battlersBySide[side];
        }

        /// <summary>
        /// Given an AOE targeting type, user, main target, and radius, returns the subset of battlersToCheck that are within range of the given AOE.
        /// </summary>
        public Battler[] GetBattlersWithinAOERangeOf(Battler user, Battler target, ActionTargetType targetType, float radius, Battler[] battlersToCheck)
        {
            List<Battler> buffer = new List<Battler>(16);
            switch (targetType)
            {
                case ActionTargetType.LineOfSightPiercing:
                    RaycastHit[] hits = Physics.BoxCastAll(user.capsuleCollider.center, new Vector3(radius, 1, radius), target.logicalPosition - user.logicalPosition, Quaternion.FromToRotation(Vector3.zero, target.logicalPosition - user.logicalPosition), BattleOverseer.fieldRadius, layerMask);
                    for (int h = 0; h < hits.Length; h++)
                    {
                        for (int b = 0; b < battlersToCheck.Length; b++)
                        {
                            if (hits[h].collider == battlersToCheck[b].capsuleCollider)
                            {
                                buffer.Add(battlersToCheck[b]);
                                break;
                            }
                        }
                    }
                    break;
                case ActionTargetType.AllTargetsInRange:
                    if (radius >= BattleOverseer.fieldRadius * 2) return battlersToCheck; // if you cover a wider range than the battlefield (usually infinite range for hit-all shit) then ofc there's no point in going further
                    for (int b = 0; b < battlersToCheck.Length; b++)
                    {
                        RaycastHit hit;
                        user.capsuleCollider.Raycast(new Ray(battlersToCheck[b].capsuleCollider.center, battlersToCheck[b].logicalPosition - user.logicalPosition), out hit, BattleOverseer.fieldRadius);
                        if (hit.collider != null && hit.distance < radius + battlersToCheck[b].footprintRadius + user.footprintRadius) buffer.Add(battlersToCheck[b]);
                    }
                    break;
                case ActionTargetType.CircularAOE:
                    if (radius >= BattleOverseer.fieldRadius * 2) return battlersToCheck; // if you cover a wider range than the battlefield (usually infinite range for hit-all shit) then ofc there's no point in going further
                    for (int b = 0; b < battlersToCheck.Length; b++)
                    {
                        buffer.Add(battlersToCheck[b]);
                        break; // the code below this point only makes sense once movement and collision are a thing
                        RaycastHit r;
                        target.capsuleCollider.Raycast(new Ray(battlersToCheck[b].capsuleCollider.center, battlersToCheck[b].logicalPosition - target.logicalPosition), out r, BattleOverseer.fieldRadius);
                        if (r.collider != null && r.distance < radius + battlersToCheck[b].footprintRadius + target.footprintRadius) buffer.Add(battlersToCheck[b]);
                    }
                    break;
            }
            return buffer.ToArray();
        }
    }
}