using UnityEngine;
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
        /// <summary>
        /// Battle overseer state values.
        /// </summary>
        public enum OverseerState 
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
        /// Battle overseer subsystem processing that handles the action we're currently executing.
        /// This is sort of fiddly, so I've taken the liberty of docstringing the hell out of it.
        /// </summary>
        private static class ActionExecutionSubsystem
        {
            internal static bool isRunning { get { return currentActingBattler != null; } }
            /// <summary>
            /// The Battler that's using the action we're executing.
            /// </summary>
            private static Battler currentActingBattler;
            /// <summary>
            /// The action that we're executing.
            /// </summary>
            private static BattleAction actionInExecution;
            /// <summary>
            /// Where are we in the current action's subactions?
            /// </summary>
            private static int subactionExecutionIndex;
            /// <summary>
            /// Where are we in the current subaction's FX packages?
            /// </summary>
            private static int subactionFXExecutionIndex;
            /// <summary>
            /// List of primary-type targets.
            /// Subactions will apply to these Battlers if they aren't using alternate targets.
            /// </summary>
            private static List<Battler> targets;
            /// <summary>
            /// List of alternate-type targets.
            /// If subactions apply to alternate targets, these are those.
            /// </summary>
            private static List<Battler> alternateTargets;
            /// <summary>
            /// List of integer arrays. Indices within this list correspond to the current action's subactions array.
            /// The sub-arrays store the damage figures of the individual subactions, per target index.
            /// </summary>
            private static List<int[]> subactions_FinalDamageFigures;
            /// <summary>
            /// List of boolean arrays. Indices within this list correspond to the current action's subactions array.
            /// The sub-arrays store the success/failure values of the individual subactions, per target index.
            /// </summary>
            private static List<bool[]> subactions_TargetHitArrays;
            /// <summary>
            /// List of booleans. Indices correspond to current subaction's fx array.
            /// True for each fx package that at least did _something._
            /// </summary>
            private static List<bool> currentSubaction_FX_NonFailures;
            /// <summary>
            /// List of boolean arrays. Indices within this list correspond to the current subaction's fx array.
            /// The sub-arrays store the success/failure values of the individual fx packages, per target index.
            /// </summary>
            private static List<bool[]> currentSubaction_FX_SuccessArrays;

            /// <summary>
            /// First run setup for CurrentActionExecutionSubsystem.
            /// </summary>
            internal static void FirstRunSetup ()
            {
                subactionExecutionIndex = 0;
                targets = new List<Battler>();
                alternateTargets = new List<Battler>();
                subactions_FinalDamageFigures = new List<int[]>();
                subactions_TargetHitArrays = new List<bool[]>();
                currentSubaction_FX_SuccessArrays = new List<bool[]>();
                currentSubaction_FX_NonFailures = new List<bool>();
            }

            /// <summary>
            /// Handles cleanup for CurrentActionExecutionSubsystem.
            /// </summary>
            internal static void Cleanup ()
            {
                currentActingBattler = null;
                actionInExecution = ActionDatabase.Get(ActionType.None);
                subactionExecutionIndex = subactionFXExecutionIndex = 0;
                targets.Clear();
                alternateTargets.Clear();
                subactions_FinalDamageFigures.Clear();
                subactions_TargetHitArrays.Clear();
                currentSubaction_FX_SuccessArrays.Clear();
                currentSubaction_FX_NonFailures.Clear();
            }

            /// <summary>
            /// Sets the action execution subsystem up to execute the given action.
            /// </summary>
            internal static void BeginProcessingAction (BattleAction action, Battler user, Battler[] _targets, Battler[] _alternateTargets)
            {
                string targetStr = string.Empty;
                for (int i = 0; i < _targets.Length; i++)
                {
                    targetStr += _targets[i].puppet.gameObject.name;
                    if (i + 1 < _targets.Length) targetStr += ", ";
                }
                Debug.Log(user.puppet.gameObject.name + " is executing action " + action.actionID + " against " + targetStr);
                ChangeState(OverseerState.ExecutingAction);
                user.CommitCurrentChosenActions();
                if (action == ActionDatabase.SpecialActions.selfStanceBreakAction)
                {
                    user.BreakStance(); // we never bother setting up the action execution subsystem in this event
                }
                else
                {
                    actionInExecution = action;
                    currentActingBattler = user;
                    targets.Clear();
                    alternateTargets.Clear();
                    subactions_FinalDamageFigures.Clear();
                    subactions_TargetHitArrays.Clear();
                    subactionExecutionIndex = subactionFXExecutionIndex = 0;
                    for (int i = 0; i < _targets.Length || i < _alternateTargets.Length; i++)
                    {
                        if (i < _targets.Length) targets.Add(_targets[i]);
                        if (i < _alternateTargets.Length) alternateTargets.Add(_targets[i]);
                    }
                }
            }

            /// <summary>
            /// Handles the specified fx package.
            /// Since fx packages can apply effects even in the event that the subaction failed to inflict/heal damage,
            /// we have to iterate over all targets and check them individually, instead of doing that within the subaction success/fail check
            /// loop.
            /// </summary>
            private static bool HandleFXPackage(BattleAction.Subaction.FXPackage fxPackage, List<Battler> t)
            {
                bool atLeastOneSuccess = false;
                currentSubaction_FX_SuccessArrays.Add(new bool[t.Count]);
                for (int targetIndex = 0; targetIndex < t.Count; targetIndex++)
                {
                    currentSubaction_FX_SuccessArrays[subactionExecutionIndex][targetIndex] = HandleFXPackage_ForTargetIndex(fxPackage, targetIndex, t);
                    if (currentSubaction_FX_SuccessArrays[subactionExecutionIndex][targetIndex]) atLeastOneSuccess = true;
                }
                return atLeastOneSuccess;
            }

            /// <summary>
            /// Checks to see if the fx package should be applied to target at the given index, and does that if so.
            /// Returns true if this succeeds, false otherwise.
            /// </summary>
            private static bool HandleFXPackage_ForTargetIndex(BattleAction.Subaction.FXPackage fxPackage, int targetIndex, List<Battler> t)
            {
                bool executionSuccess = true;
                if (fxPackage.thisFXSuccessTiedToFXAtIndex > -1) executionSuccess = (currentSubaction_FX_SuccessArrays[fxPackage.thisFXSuccessTiedToFXAtIndex][targetIndex] == true);
                else if (!fxPackage.applyEvenIfSubactionMisses && subactions_TargetHitArrays[subactionExecutionIndex][targetIndex] == false) executionSuccess = false;
                else if (fxPackage.baseSuccessRate < 1.0f) executionSuccess = t[targetIndex].TryToLandFXAgainstMe(currentActingBattler, fxPackage);
                if (executionSuccess) t[targetIndex].ApplyFXPackage(fxPackage);
                return executionSuccess;
            }

            /// <summary>
            /// Handles a single subaction.
            /// Doesn't do any of the logic to track where we are in the current action's subaction set.
            /// Since the entire point of the subaction system is that we can chain them together either simultaneously or
            /// at different points in e.g. attack animations, normally you'll call StepSubactions() or FinishCurrentAction() and those will
            /// find the subactions that this method should be given.
            /// </summary>
            private static bool HandleSubaction(BattleAction.Subaction subaction)
            {
                bool atLeastOneSuccess = false;
                currentSubaction_FX_SuccessArrays.Clear(); // this needs to be empty before we can start running fxpackages
                currentSubaction_FX_NonFailures.Clear();
                List<Battler> t;
                if (subaction.useAlternateTargetSet) t = alternateTargets;
                else t = targets;
                subactions_FinalDamageFigures.Add(new int[t.Count]);
                subactions_TargetHitArrays.Add(new bool[t.Count]);
                for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
                {
                    subactions_TargetHitArrays[subactionExecutionIndex][targetIndex] = HandleSubaction_ForTargetIndex(subaction, targetIndex, t);
                    if (subactions_TargetHitArrays[subactionExecutionIndex][targetIndex] == true) atLeastOneSuccess = true;
                }
                for (subactionFXExecutionIndex = 0; subactionFXExecutionIndex < subaction.fx.Length; subactionFXExecutionIndex++)
                {
                    currentSubaction_FX_NonFailures.Add(HandleFXPackage(subaction.fx[subactionFXExecutionIndex], t));
                    if (currentSubaction_FX_NonFailures[subactionExecutionIndex] == true) atLeastOneSuccess = true;
                }
                return atLeastOneSuccess;
            }

            /// <summary>
            /// Checks to see if the subaction should succeed on specified target, and tells target to apply subaction if true.
            /// Returns subaction success/fail value.
            /// </summary>
            private static bool HandleSubaction_ForTargetIndex (BattleAction.Subaction subaction, int targetIndex, List<Battler> t)
            {
                bool executionSuccess;
                if (subaction.thisSubactionSuccessTiedToSubactionAtIndex > -1)
                {
                    if (subaction.useAlternateTargetSet && actionInExecution.alternateTargetType == ActionTargetType.Self || !subaction.useAlternateTargetSet && actionInExecution.targetingType == ActionTargetType.Self)
                    {
                        executionSuccess = false;
                        for (int i = 0; i < subactions_TargetHitArrays[subaction.thisSubactionSuccessTiedToSubactionAtIndex].Length; i++)
                        {
                            if (subactions_TargetHitArrays[subaction.thisSubactionSuccessTiedToSubactionAtIndex][i] == true)
                            {
                                executionSuccess = true; // if the current subaction is acting on ourself and the subaction we're yoked to acts on a larger set of targets, we can go ahead with acting on ourself if _any_ of those hits landed
                                break;
                            }
                        }
                    }
                    else if (actionInExecution.Subactions[subaction.thisSubactionSuccessTiedToSubactionAtIndex].useAlternateTargetSet && actionInExecution.alternateTargetType == ActionTargetType.Self ||
                        !actionInExecution.Subactions[subaction.thisSubactionSuccessTiedToSubactionAtIndex].useAlternateTargetSet && actionInExecution.targetingType == ActionTargetType.Self)
                    {
                        executionSuccess = subactions_TargetHitArrays[subaction.thisSubactionSuccessTiedToSubactionAtIndex][0]; // if we're _tied_ to a self-targeting subaction, that's always index 0
                        // and so we can generalize that to a larger set of targets!
                        // (if we get a target type mismatch on tied subactions and one side or the other isn't self-targeting, the parser will throw a shit fit, so we don't have to worry about that case on this side of things)
                    }
                    else executionSuccess = subactions_TargetHitArrays[subactionExecutionIndex][targetIndex];
                }
                else executionSuccess = t[targetIndex].TryToLandAttackAgainstMe(currentActingBattler, subaction);
                if (executionSuccess)
                {
                    subactions_FinalDamageFigures[subactionExecutionIndex][targetIndex] = t[targetIndex].CalcDamageAgainstMe(currentActingBattler, subaction, true, true, true);
                    t[targetIndex].DealOrHealDamage(subactions_FinalDamageFigures[subactionExecutionIndex][targetIndex]);
                }
                return executionSuccess;
            }

            /// <summary>
            /// Executes steps subactions from the current action's subaction set, or however many are left.
            /// Returns true if any of the subactions we step through do anything at all.
            /// </summary>
            internal static bool StepSubactions (int steps)
            {
                bool atLeastOneSuccess = false;
                for (int i = 0; i < steps & subactionExecutionIndex < actionInExecution.Subactions.Length; i++)
                {
                    if (HandleSubaction(actionInExecution.Subactions[subactionExecutionIndex])) atLeastOneSuccess = true;
                    subactionExecutionIndex++;
                }
                if (subactionExecutionIndex == actionInExecution.Subactions.Length) StopExecutingAction();
                return atLeastOneSuccess;
            }

            /// <summary>
            /// Does exactly what it says on the tin.
            /// </summary>
            internal static void StopExecutingAction ()
            {
                Cleanup(); // this is just a passthrough to cleanup atm, but it should acquire functionality as the system grows so it's nice to have the call in place
            }

            /// <summary>
            /// The specified battler is dead, so if we're running an action, we
            /// need to remove it from any target lists it might be on.
            /// If it's _using_ an action, we need to stop that entirely.
            /// </summary>
            internal static void BattlerIsDead (Battler b)
            {
                targets.Remove(b);
                alternateTargets.Remove(b);
                if (currentActingBattler == b) StopExecutingAction();
            }

            /// <summary>
            /// Handles all remaining subactions, then stops executing the current action.
            /// Returns true if any subaction does anything.
            /// </summary>
            internal static bool FinishCurrentAction ()
            {
                bool r = StepSubactions(int.MaxValue); // we just call StepSubactions with a very big int - because StepSubactions keeps you from stepping past the end of the array, giving it int.MaxValue causes it to just step until it can't step any further
                return r;
            }
        }

        /// <summary>
        /// Turn progression:
        /// - If battlersReadyToTakeTurns.Count > 0, we can take a turn!
        /// - Get the first battler that's ready to go fromt he list
        /// - Call into that Battler's GetAction() to get targets/secondaryTargets/action
        /// - Start executing the action
        /// - When the action has finished executing, turn is over
        /// </summary>
        private static class TurnManagementSubsystem
        {
            /// <summary>
            /// Battler that's currently taking a turn.
            /// </summary>
            internal static Battler currentTurnBattler;
            /// <summary>
            /// Battlers to give turns when that next becomes possible. Normally there should only actually be one Battler here at a time... but ties are a thing,
            /// and having multiple members in this list gives us an easy way to say "these guys need a tiebreaker."
            /// </summary>
            internal static List<Battler> battlersReadyToTakeTurns;

            /// <summary>
            /// First-run setup for turn management subsystem.
            /// </summary>
            internal static void FirstRunSetup ()
            {
                battlersReadyToTakeTurns = new List<Battler>();
            }

            /// <summary>
            /// Cleanup for turn management subsystem.
            /// </summary>
            internal static void Cleanup ()
            {
                battlersReadyToTakeTurns.Clear();
            }

            /// <summary>
            /// So long as something's taking a turn right now, gives that battler a second turn immediately after this one.
            /// </summary>
            internal static void ExtendCurrentTurn ()
            {
                if (currentTurnBattler == null) Util.Crash(new System.Exception("Can't extend current turn because there _isn't_ a current turn."));
                battlersReadyToTakeTurns.Insert(0, currentTurnBattler);
            }

            /// <summary>
            /// Checks to see if a) we have battlers waiting on their turn and b) we aren't currently taking a turn.
            /// </summary>
            /// <returns></returns>
            internal static bool ReadyToTakeATurn()
            {
                return (battlersReadyToTakeTurns.Count > 0 && currentTurnBattler == null);
            }

            /// <summary>
            /// Battler b needs to take a turn as soon as it can be allowed to do so.
            /// </summary>
            internal static void RequestTurn(Battler b)
            {
                battlersReadyToTakeTurns.Add(b);
            }

            /// <summary>
            /// Coroutine: calls b.GetAction, then sit on our ass until b gives us the action we want.
            /// (This lets us pause the battle simulation indefinitely for eg. player input. Or _really_
            /// messy AI, hypothetically, I guess.)
            /// </summary>
            private static IEnumerator<float> _WaitUntilBattlerReadyToAct (Battler b)
            {
                ChangeState(OverseerState.WaitingForInput);
                b.GetAction();
                while (b.turnActions.action == ActionDatabase.SpecialActions.defaultBattleAction) yield return 0; // wait until b decides what to do
                ActionExecutionSubsystem.BeginProcessingAction(b.turnActions.action, b, b.turnActions.targets, b.turnActions.alternateTargets);
                Timing.RunCoroutine(_EndTurnOnceActionExecutionCompleted());
            }

            /// <summary>
            /// Coroutine: waits until action execution completes, then ends turn.
            /// </summary>
            private static IEnumerator<float> _EndTurnOnceActionExecutionCompleted ()
            {
                while (ActionExecutionSubsystem.isRunning) yield return 0;
                EndTurn();
            }

            /// <summary>
            /// Starts taking a turn.
            /// </summary>
            internal static void StartTurn ()
            {
                BattleStage.instance.StartOfTurn();
                currentTurnBattler = battlersReadyToTakeTurns[0];
                battlersReadyToTakeTurns.Remove(currentTurnBattler);
                Timing.RunCoroutine(_WaitUntilBattlerReadyToAct(currentTurnBattler));
            }

            /// <summary>
            /// Finishes taking a turn.
            /// </summary>
            internal static void EndTurn()
            {
                ActionExecutionSubsystem.FinishCurrentAction();
                currentTurnBattler = null;
                ChangeState(OverseerState.BetweenTurns);
                DeriveNormalizedSpeed();
            }
        }

        public const float battleTickLength = 1 / 60;
        public const float fieldRadius = 50;
        private readonly static int layerMask = Animator.StringToHash("BATTLE_Battlers");
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
        public static Battler currentTurnBattler { get { return TurnManagementSubsystem.currentTurnBattler; } }
        public static OverseerState overseerState { get; private set; }

        public static List<Battler> allBattlers { get; private set; }
        public static Dictionary<BattlerSideFlags, List<Battler>> battlersBySide { get; private set; } // xzibit.jpg

        /// <summary>
        /// Turn order tiebreakers work as follows: every battler is a part of this stack in a random order. When multiple battlers are trying to take their turn at the same time, 
        /// or "tied" in some other sense, we just pop Battlers off the top of battlerTiebreakerStack until we get one of the ones that's trying to act, then rerandomize it.
        /// </summary>
        private static Stack<Battler> battlerTiebreakerStack;

        /// <summary>
        /// Use this to avoid instantiating new battler list instances when you just want it for a single function.
        /// Assume this needs to be cleared if you're going to use it, and that it could basically contain any random grouping of battlers.
        /// This is not safe for use in asynchronous operations.
        /// </summary>
        private static List<Battler> tmpBattlersListBuffer;


        // Communication between the battle system and "not the battle system"

        /// <summary>
        /// Sets up battle based on given formation and starts
        /// executing Battle Shit.
        /// </summary>
        public static void StartBattle (BattleFormation formation)
        {
            if (overseerState != OverseerState.Offline) Util.Crash(new System.Exception("Can't start a battle while the battle overseer isn't offline!"));
            Cleanup();
            activeFormation = formation;
            for (int b = 0; b < activeFormation.battlers.Length; b++)
            {
                GenerateBattlerFromFormationMember(activeFormation.battlers[b]);
            }
            DeriveNormalizedSpeed();
            for (int b = 0; b < allBattlers.Count; b++)
            {
                allBattlers[b].ApplyDelay(1.0f);
            }
            ChangeState(OverseerState.BetweenTurns);
            Timing.RunCoroutine(_WaitForPuppets());
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
                for (int i = 0; i < allBattlers.Count; i++)
                {
                    if (allBattlers[i].puppet == null) allPuppetsExist = false;
                }
                yield return 0;
            }
            BattleStage.instance.StartOfBattle();
        }

        /// <summary>
        /// Generates a Battler based on the given formation member and attaches a puppet gameobject to it.
        /// </summary>
        public static Battler GenerateBattlerFromFormationMember (BattleFormation.FormationMember formationMember)
        {
            Battler battler = new Battler(formationMember);
            allBattlers.Add(battler);
            battlersBySide[battler.side].Add(battler);
            BattlerPuppet puppet = BattleStage.instance.GetAPuppet();
            puppet.AttachBattler(battler);
            return battler;
        }

        /// <summary>
        /// Advances the battle simulation by one "step."
        /// The way this is intended to be used, basically, is that BattleStage calls BattleStep() whenever it doesn't have any animation events
        /// to process, goes through all of those, and then calls BattleStep() again.
        /// </summary>
        public static void BattleStep ()
        {
            if (!CheckIfBattleAlreadyOver()) switch (overseerState)
            {
                case OverseerState.Paused:
                    Util.Crash(new System.Exception("Can't advance battle state: battle is paused."));
                    break;
                case OverseerState.Offline:
                    Util.Crash(new System.Exception("Can't advance battle state: battle system is offline."));
                    break;
                case OverseerState.BetweenTurns:
                    BetweenTurns();
                    if (TurnManagementSubsystem.ReadyToTakeATurn())
                    {
                        TurnManagementSubsystem.StartTurn();
                    }
                    break;
                case OverseerState.WaitingForInput:
                    Util.Crash(new System.Exception("Can't advance battle state: waiting for player input."));
                    break;
                case OverseerState.ExecutingAction:
                    ActionExecutionSubsystem.StepSubactions(1);
                    break;
                case OverseerState.BattleWon:
                    Util.Crash(new System.Exception("Can't advance battle state: battle is already won."));
                    break;
                case OverseerState.BattleLost:
                    Util.Crash(new System.Exception("Can't advance battle state: battle is already lost."));
                    break;
                default:
                    Util.Crash(new System.Exception("Can't advance battle state: invalid overseer state " + overseerState.ToString()));
                    break;
            }
        }

        /// <summary>
        /// Passthrough to CurrentActionExecutionSubsystem.StepSubactions; keeps from exposing BattleOverseer internal structure.
        /// Executes steps subactions from the current action's subaction set, or however many are left.
        /// Returns true if any of the subactions we step through do anything at all.
        /// </summary>
        public static bool StepSubactions (int steps = 1)
        {
            return ActionExecutionSubsystem.StepSubactions(steps);
        }

        // BattleOverseer state management

        /// <summary>
        /// Change overseer state, if allowed.
        /// </summary>
        private static void ChangeState (OverseerState _state)
        {
            switch (overseerState)
            {
                case OverseerState.Offline:
                case OverseerState.BetweenTurns:
                case OverseerState.ExecutingAction:
                case OverseerState.WaitingForInput:
                    if (_state == OverseerState.Offline) Util.Crash(new System.Exception("Can't change state back to offline!"));
                    break;
                case OverseerState.BattleWon:
                case OverseerState.BattleLost:
                    if (_state != OverseerState.Offline) Util.Crash(new System.Exception("Can only change state to offline after end of battle"));
                    break;
                default:
                    break;
            }
            overseerState = _state;
        }

        /// <summary>
        /// Initializes the BattleOverseer and loads in the various datasets the battle system uses.
        /// </summary>
        public static void FirstRunSetup ()
        {
            if (allBattlers != null) Util.Crash(new System.Exception("BattleOverseer.FirstRunSetup can't be called more than once!"));
            overseerState = OverseerState.Offline;
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
            battlerTiebreakerStack = new Stack<Battler>();
            tmpBattlersListBuffer = new List<Battler>();
            ActionExecutionSubsystem.FirstRunSetup();
            TurnManagementSubsystem.FirstRunSetup();
            BattlerAISystem.FirstRunSetup();
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
            overseerState = OverseerState.Offline;
            activeFormation = null;
            normalizedSpeed = 0;
            allBattlers.Clear();
            battlersBySide[BattlerSideFlags.PlayerSide].Clear();
            battlersBySide[BattlerSideFlags.GenericAlliedSide].Clear();
            battlersBySide[BattlerSideFlags.GenericEnemySide].Clear();
            battlersBySide[BattlerSideFlags.GenericNeutralSide].Clear();
            battlerTiebreakerStack.Clear();
            tmpBattlersListBuffer.Clear();
            ActionExecutionSubsystem.Cleanup();
            TurnManagementSubsystem.Cleanup();

        }

        // Bits and pieces for use within the battle loop

        /// <summary>
        /// Returns true if we've either won or lost.
        /// </summary>
        private static bool CheckIfBattleAlreadyOver ()
        {
            if (CheckIfBattleWon())
            {
                Debug.Log("You're winner!");
                ChangeState(OverseerState.BattleWon);
                return true;
            }
            else if (CheckIfBattleLost())
            {
                Debug.Log("You're loser!");
                ChangeState(OverseerState.BattleLost);
                return true;
            }
            else return false;
        }

        /// <summary>
        /// Called between turns - handles delay logic and updates battlers so that they can request turns.
        /// </summary>
        private static void BetweenTurns ()
        {
            if (!CheckIfBattleAlreadyOver())
            {
                float lowestDelay = float.MaxValue;
                for (int i = 0; i < allBattlers.Count; i++)
                {
                    if (allBattlers[i].currentDelay < lowestDelay) lowestDelay = allBattlers[i].currentDelay;
                }
                for (int i = 0; i < allBattlers.Count; i++) allBattlers[i].BetweenTurns(lowestDelay);
            }
        }

        /// <summary>
        /// Uses BattlerTiebreakerStack to break a tie.
        /// Returns the Battler that won.
        /// </summary>
        private static Battler BreakTie (Battler[] tiedBattlers)
        {
            if (tiedBattlers.Length == 0) Util.Crash(new System.Exception("You're trying to break a tie between no battlers. Protip: ain't nobody gonna win that one.")); 
            while (battlerTiebreakerStack.Count > 0)
            {
                Battler b = battlerTiebreakerStack.Pop();
                for (int i = 0; i < tiedBattlers.Length; i++)
                {
                    if (tiedBattlers[i] == b) return b;
                }
            }
            Util.Crash(new System.Exception("Tried to break a tie, but none of the tiedBattlers were in the battlerTiebreakerStack. That... shouldn't happen."));
            return default(Battler);
        }

        /// <summary>
        /// Returns true if no enemy units are still alive.
        /// </summary>
        private static bool CheckIfBattleWon ()
        {
            int liveEnemiesCount = 0;
            tmpBattlersListBuffer.Clear();
            GetBattlersEnemiesTo(BattlerSideFlags.PlayerSide, ref tmpBattlersListBuffer);
            for (int i = 0; i < tmpBattlersListBuffer.Count; i++)
            {
                if (!tmpBattlersListBuffer[i].isDead) liveEnemiesCount++;
            }
            return liveEnemiesCount < 1;
        }

        /// <summary>
        /// Returns true if no player units are still alive.
        /// </summary>
        private static bool CheckIfBattleLost ()
        {
            int livePlayersCount = 0;
            for (int i = 0; i < battlersBySide[BattlerSideFlags.PlayerSide].Count; i++)
            {
                if (!battlersBySide[BattlerSideFlags.PlayerSide][i].isDead) livePlayersCount++;
            }
            return livePlayersCount < 1;
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
        /// Given an AOE targeting type, user, main target, and radius, returns the subset of battlersToCheck that are within range of the given AOE.
        /// </summary>
        public static Battler[] GetBattlersWithinAOERangeOf(Battler user, Battler target, ActionTargetType targetType, float radius, Battler[] battlersToCheck)
        {
            tmpBattlersListBuffer.Clear();
            switch (targetType)
            {
                case ActionTargetType.LineOfSightPiercing:
                    RaycastHit[] hits = Physics.BoxCastAll(user.capsuleCollider.center, new Vector3(radius, 1, radius), target.logicalPosition - user.logicalPosition, Quaternion.FromToRotation(Vector3.zero, target.logicalPosition - user.logicalPosition), fieldRadius, layerMask);
                    for (int h = 0; h < hits.Length; h++)
                    {
                        for (int b = 0; b < battlersToCheck.Length; b++)
                        {
                            if (hits[h].collider == battlersToCheck[b].capsuleCollider)
                            {
                                tmpBattlersListBuffer.Add(battlersToCheck[b]);
                                break;
                            }
                        }
                    }
                    break;
                case ActionTargetType.AllTargetsInRange:
                    if (radius >= fieldRadius * 2) return battlersToCheck; // if you cover a wider range than the battlefield (usually infinite range for hit-all shit) then ofc there's no point in going further
                    for (int b = 0; b < battlersToCheck.Length; b++)
                    {
                        RaycastHit hit;
                        user.capsuleCollider.Raycast(new Ray(battlersToCheck[b].capsuleCollider.center, battlersToCheck[b].logicalPosition - user.logicalPosition), out hit, fieldRadius);
                        if (hit.collider != null && hit.distance < radius + battlersToCheck[b].footprintRadius + user.footprintRadius) tmpBattlersListBuffer.Add(battlersToCheck[b]);
                    }
                    break;
                case ActionTargetType.CircularAOE:
                    if (radius >= fieldRadius * 2) return battlersToCheck; // if you cover a wider range than the battlefield (usually infinite range for hit-all shit) then ofc there's no point in going further
                    for (int b = 0; b < battlersToCheck.Length; b++)
                    {
                        tmpBattlersListBuffer.Add(battlersToCheck[b]);
                        break; // the code below this point only makes sense once movement and collision are a thing
                        RaycastHit r;
                        target.capsuleCollider.Raycast(new Ray(battlersToCheck[b].capsuleCollider.center, battlersToCheck[b].logicalPosition - target.logicalPosition), out r, fieldRadius);
                        if (r.collider != null && r.distance < radius + battlersToCheck[b].footprintRadius + target.footprintRadius) tmpBattlersListBuffer.Add(battlersToCheck[b]);
                    }
                    break;
            }
            return tmpBattlersListBuffer.ToArray();
        }

        // Interfaces to subsystem functionality

        /// <summary>
        /// Passthrough to CurrentActionExecutionSubsystem.BattlerIsDead; keeps from exposing BattleOverseer internal structure.
        /// The specified battler is dead, so if we're running an action, we
        /// need to remove it from any target lists it might be on.
        /// If it's _using_ an action, we need to stop that entirely.
        /// </summary>
        public static void BattlerIsDead(Battler b)
        {
            ActionExecutionSubsystem.BattlerIsDead(b);
        }

        /// <summary>
        /// Passthrough to TurnManagementSubsystem.ExtendCurrentTurn.
        /// So long as something's taking a turn right now, gives that battler a second turn immediately after this one.
        /// </summary>
        internal static void ExtendCurrentTurn()
        {
            TurnManagementSubsystem.ExtendCurrentTurn();
        }

        /// <summary>
        /// Passthrough to CurrentActionExecutionSubsystem.FinishCurrentAction; keeps from exposing BattleOverseer internal structure.
        /// Handles all remaining subactions, then stops executing the current action.
        /// Returns true if any subaction does anything.
        /// </summary>
        public static bool FinishCurrentAction()
        {
            return ActionExecutionSubsystem.FinishCurrentAction();
        }

        /// <summary>
        /// Passthrough to TurnManagementSubsystem.RequestTurn.
        /// Battler b needs to take a turn as soon as it can be allowed to do so.
        /// </summary>
        public static void RequestTurn(Battler b)
        {
            TurnManagementSubsystem.RequestTurn(b);
        }

        /// <summary>
        /// Gets all Battlers that are considered allies of a battler of side side.
        /// This includes those of the battler's own side - call GetBattlersAlliedTo_Strict
        /// if you want, specifically, allies of a _different_ side.
        /// </summary>
        public static void GetBattlersAlliedTo (BattlerSideFlags side, ref List<Battler> outputList)
        {
            tmpBattlersListBuffer.Clear(); // make sure this is empty before trying to use it
            switch (side)
            {
                case BattlerSideFlags.PlayerSide:
                case BattlerSideFlags.GenericAlliedSide:
                    tmpBattlersListBuffer.AddRange(battlersBySide[BattlerSideFlags.PlayerSide]);
                    tmpBattlersListBuffer.AddRange(battlersBySide[BattlerSideFlags.GenericAlliedSide]);
                    break;
                case BattlerSideFlags.GenericEnemySide:
                    tmpBattlersListBuffer.AddRange(battlersBySide[BattlerSideFlags.GenericEnemySide]);
                    break;
                case BattlerSideFlags.GenericNeutralSide:
                    tmpBattlersListBuffer.AddRange(battlersBySide[BattlerSideFlags.GenericNeutralSide]);
                    break;
                default:
                    Util.Crash(new System.Exception("Tried to find allies of side " + side + ", but it wasn't in the table."));
                    break;
            }
            for (int i = 0; i < tmpBattlersListBuffer.Count; i++) if (!outputList.Contains(tmpBattlersListBuffer[i])) outputList.Add(tmpBattlersListBuffer[i]);
        }

        /// <summary>
        /// Gets all Battlers that are considered allies of a battler of side side.
        /// This excludes those of the battler's own side.
        /// </summary>
        public static void GetBattlersAlliedTo_Strict(BattlerSideFlags side, ref List<Battler> outputList)
        {
            tmpBattlersListBuffer.Clear(); // make sure this is empty before trying to use it
            switch (side)
            {
                case BattlerSideFlags.PlayerSide:
                    tmpBattlersListBuffer.AddRange(battlersBySide[BattlerSideFlags.GenericAlliedSide]);
                    break;
                case BattlerSideFlags.GenericAlliedSide:
                    tmpBattlersListBuffer.AddRange(battlersBySide[BattlerSideFlags.PlayerSide]);
                    break;
                case BattlerSideFlags.GenericEnemySide:
                    tmpBattlersListBuffer.AddRange(battlersBySide[BattlerSideFlags.GenericEnemySide]);
                    break;
                case BattlerSideFlags.GenericNeutralSide:
                    tmpBattlersListBuffer.AddRange(battlersBySide[BattlerSideFlags.GenericNeutralSide]);
                    break;
                default:
                    Util.Crash(new System.Exception("Tried to find allies of side " + side + ", but it wasn't in the table."));
                    break;
            }
            for (int i = 0; i < tmpBattlersListBuffer.Count; i++) if (!outputList.Contains(tmpBattlersListBuffer[i])) outputList.Add(tmpBattlersListBuffer[i]);
        }

        /// <summary>
        /// Gets all battlers by the order they're going to take their turns in.
        /// This is actually just a more readable shortcut for GetBattlersBySimulatedTurnOrder(-1)
        /// </summary>
        public static Battler[] GetBattlersByTurnOrder()
        {
            return GetBattlersBySimulatedTurnOrder(-1);
        }

        /// <summary>
        /// Gets all battlers by the order they're going to take their turns in,
        /// plus an additional entry representing the next turn of the current acting battler
        /// if it has a delay of prospectiveDelay.
        /// If prospectiveDelay is less than 0, returns just the current turn order.
        /// </summary>
        public static Battler[] GetBattlersBySimulatedTurnOrder(float prospectiveDelay)
        {
            tmpBattlersListBuffer.Clear();
            if (TurnManagementSubsystem.currentTurnBattler != null) tmpBattlersListBuffer.Add(TurnManagementSubsystem.currentTurnBattler);
            else if (prospectiveDelay >= 0) Util.Crash(new System.Exception("Can't get turn order with prospective delay value: no battler is acting"));
            if (TurnManagementSubsystem.battlersReadyToTakeTurns.Count > 0)
            {
                for (int i = 0; i < TurnManagementSubsystem.battlersReadyToTakeTurns.Count; i++) tmpBattlersListBuffer.Add(TurnManagementSubsystem.battlersReadyToTakeTurns[i]);
            }
            int skippedBattlers = 0;
            int cnt = allBattlers.Count;
            if (prospectiveDelay >= 0) cnt++;
            while (tmpBattlersListBuffer.Count + skippedBattlers < cnt)
            {
                float lowestRemainingDelay = float.MaxValue;
                int thisIterationBattlerIndex = -1;
                for (int i = 0; i < allBattlers.Count; i++)
                {
                    if (tmpBattlersListBuffer.Contains(allBattlers[i])) continue; // battler already in the list
                    else if (allBattlers[i].isDead)
                    {
                        skippedBattlers++; // don't hang
                        continue; // it's dead so it ain't gonna get no more turns
                    }
                    else
                    {
                        if (allBattlers[i].currentDelay < lowestRemainingDelay)
                        {
                            lowestRemainingDelay = allBattlers[i].currentDelay;
                            thisIterationBattlerIndex = i;
                        }
                    }
                }
                if (prospectiveDelay >= 0 && prospectiveDelay < lowestRemainingDelay)
                {
                    tmpBattlersListBuffer.Add(TurnManagementSubsystem.currentTurnBattler);
                    continue;            
                }
                if (thisIterationBattlerIndex > -1) tmpBattlersListBuffer.Add(allBattlers[thisIterationBattlerIndex]);
            }
            return tmpBattlersListBuffer.ToArray();
        }

        /// <summary>
        /// Gets all Battlers that are considered enemies of a battler of side side.
        /// </summary>
        public static void GetBattlersEnemiesTo(BattlerSideFlags side, ref List<Battler> outputList)
        {
            tmpBattlersListBuffer.Clear();
            switch (side)
            {
                case BattlerSideFlags.PlayerSide:
                case BattlerSideFlags.GenericAlliedSide:
                    tmpBattlersListBuffer.AddRange(battlersBySide[BattlerSideFlags.GenericEnemySide]);
                    break;
                case BattlerSideFlags.GenericEnemySide:
                    tmpBattlersListBuffer.AddRange(battlersBySide[BattlerSideFlags.PlayerSide]);
                    tmpBattlersListBuffer.AddRange(battlersBySide[BattlerSideFlags.GenericAlliedSide]);
                    break;
                case BattlerSideFlags.GenericNeutralSide:
                    break;
                default:
                    Util.Crash(new System.Exception("Tried to find enemies of side " + side + ", but it wasn't in the table."));
                    break;
            }
            for (int i = 0; i < tmpBattlersListBuffer.Count; i++) if (!outputList.Contains(tmpBattlersListBuffer[i])) outputList.Add(tmpBattlersListBuffer[i]);
        }

        /// <summary>
        /// Gets all Battlers that are considered neutral to a battler of side side.
        /// </summary>
        public static void GetBattlersNeutralTo (BattlerSideFlags side, ref List<Battler> outputList)
        {
            tmpBattlersListBuffer.Clear(); // make sure this is empty before trying to use it
            switch (side)
            {
                case BattlerSideFlags.PlayerSide:
                case BattlerSideFlags.GenericAlliedSide:
                case BattlerSideFlags.GenericEnemySide:
                    tmpBattlersListBuffer.AddRange(battlersBySide[BattlerSideFlags.GenericNeutralSide]);
                    break;
                case BattlerSideFlags.GenericNeutralSide:
                    tmpBattlersListBuffer.AddRange(battlersBySide[BattlerSideFlags.PlayerSide]);
                    tmpBattlersListBuffer.AddRange(battlersBySide[BattlerSideFlags.GenericAlliedSide]);
                    tmpBattlersListBuffer.AddRange(battlersBySide[BattlerSideFlags.GenericEnemySide]);
                    break;
                default:
                    Util.Crash(new System.Exception("Tried to find neutrals for side " + side + ", but it wasn't in the table."));
                    break;
            }
            for (int i = 0; i < tmpBattlersListBuffer.Count; i++) if (!outputList.Contains(tmpBattlersListBuffer[i])) outputList.Add(tmpBattlersListBuffer[i]);
        }

        /// <summary>
        /// Gets all Battlers that are the same side as a battler of side side.
        /// Side. Side side side sidddeeee.
        /// Whose side is this side? My side, your side, side's side, side side side.
        /// </summary>
        public static void GetBattlersSameSideAs (BattlerSideFlags side, ref List<Battler> outputList)
        {
            tmpBattlersListBuffer.Clear();
            tmpBattlersListBuffer.AddRange(battlersBySide[side]);
            for (int i = 0; i < tmpBattlersListBuffer.Count; i++) if (!outputList.Contains(tmpBattlersListBuffer[i])) outputList.Add(tmpBattlersListBuffer[i]);
        }
    }
}