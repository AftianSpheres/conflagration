using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CnfBattleSys
{
    /// <summary>
    /// Battle overseer subsystem processing that handles the action we're currently executing.
    /// This is sort of fiddly, so I've taken the liberty of docstringing the hell out of it.
    /// </summary>
    public class ActionExecutionSubsystem
    {
        private BattleData battle;
        public bool isRunning { get { return currentActingBattler != null; } }
        /// <summary>
        /// The Battler that's using the action we're executing.
        /// </summary>
        public Battler currentActingBattler { get; private set; }
        /// <summary>
        /// The action that we're executing.
        /// </summary>
        public BattleAction actionInExecution { get; private set; }
        /// <summary>
        /// Where are we in the current action's subactions?
        /// </summary>
        private int subactionExecutionIndex;
        /// <summary>
        /// Where are we in the current subaction's FX packages?
        /// </summary>
        private int subactionFXExecutionIndex;
        /// <summary>
        /// List of primary-type targets.
        /// Subactions will apply to these Battlers if they aren't using alternate targets.
        /// </summary>
        private List<Battler> targets;
        /// <summary>
        /// List of alternate-type targets.
        /// If subactions apply to alternate targets, these are those.
        /// </summary>
        private List<Battler> alternateTargets;
        /// <summary>
        /// List of integer arrays. Indices within this list correspond to the current action's subactions array.
        /// The sub-arrays store the damage figures of the individual subactions, per target index.
        /// </summary>
        private List<int[]> subactions_FinalDamageFigures;
        /// <summary>
        /// List of boolean arrays. Indices within this list correspond to the current action's subactions array.
        /// The sub-arrays store the success/failure values of the individual subactions, per target index.
        /// </summary>
        private List<bool[]> subactions_TargetHitArrays;
        /// <summary>
        /// List of booleans. Indices correspond to current subaction's fx array.
        /// True for each fx package that at least did _something._
        /// </summary>
        private List<bool> currentSubaction_FX_NonFailures;
        /// <summary>
        /// List of boolean arrays. Indices within this list correspond to the current subaction's fx array.
        /// The sub-arrays store the success/failure values of the individual fx packages, per target index.
        /// </summary>
        private List<bool[]> currentSubaction_FX_SuccessArrays;

        /// <summary>
        /// Constructor for CurrentActionExecutionSubsystem.
        /// </summary>
        public ActionExecutionSubsystem(BattleData _battle)
        {
            battle = _battle;
            subactionExecutionIndex = 0;
            targets = new List<Battler>();
            alternateTargets = new List<Battler>();
            subactions_FinalDamageFigures = new List<int[]>();
            subactions_TargetHitArrays = new List<bool[]>();
            currentSubaction_FX_SuccessArrays = new List<bool[]>();
            currentSubaction_FX_NonFailures = new List<bool>();
        }

        /// <summary>
        /// Sets the action execution subsystem up to execute the given action.
        /// </summary>
        public void BeginProcessingAction(BattleAction action, Battler user, Battler[] _targets, Battler[] _alternateTargets)
        {
            string targetStr = string.Empty;
            for (int i = 0; i < _targets.Length; i++)
            {
                targetStr += _targets[i].puppet.gameObject.name;
                if (i + 1 < _targets.Length) targetStr += ", ";
            }
            Debug.Log(user.puppet.gameObject.name + " is executing action " + action.actionID + " against " + targetStr);
            battle.ChangeState(BattleData.State.ExecutingAction);
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
        /// Handles cleanup for CurrentActionExecutionSubsystem.
        /// </summary>
        public void Cleanup()
        {
            currentActingBattler = null;
            actionInExecution = ActionDatabase.SpecialActions.noneBattleAction;
            subactionExecutionIndex = subactionFXExecutionIndex = 0;
            targets.Clear();
            alternateTargets.Clear();
            subactions_FinalDamageFigures.Clear();
            subactions_TargetHitArrays.Clear();
            currentSubaction_FX_SuccessArrays.Clear();
            currentSubaction_FX_NonFailures.Clear();
        }


        /// <summary>
        /// Handles the specified fx package.
        /// Since fx packages can apply effects even in the event that the subaction failed to inflict/heal damage,
        /// we have to iterate over all targets and check them individually, instead of doing that within the subaction success/fail check
        /// loop.
        /// </summary>
        private bool HandleFXPackage(BattleAction.Subaction.EffectPackage fxPackage, List<Battler> t)
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
        private bool HandleFXPackage_ForTargetIndex(BattleAction.Subaction.EffectPackage fxPackage, int targetIndex, List<Battler> t)
        {
            bool executionSuccess = true;
            if (fxPackage.tieSuccessToEffectIndex > -1) executionSuccess = (currentSubaction_FX_SuccessArrays[fxPackage.tieSuccessToEffectIndex][targetIndex] == true);
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
        private bool HandleSubaction(BattleAction.Subaction subaction)
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
            for (subactionFXExecutionIndex = 0; subactionFXExecutionIndex < subaction.effectPackages.Length; subactionFXExecutionIndex++)
            {
                currentSubaction_FX_NonFailures.Add(HandleFXPackage(subaction.effectPackages[subactionFXExecutionIndex], t));
                if (currentSubaction_FX_NonFailures[subactionExecutionIndex] == true) atLeastOneSuccess = true;
            }
            return atLeastOneSuccess;
        }

        /// <summary>
        /// Checks to see if the subaction should succeed on specified target, and tells target to apply subaction if true.
        /// Returns subaction success/fail value.
        /// </summary>
        private bool HandleSubaction_ForTargetIndex(BattleAction.Subaction subaction, int targetIndex, List<Battler> t)
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
                    executionSuccess = subactions_TargetHitArrays[subaction.thisSubactionSuccessTiedToSubactionAtIndex][0];
                    // if we're _tied_ to a self-targeting subaction, that's always index 0
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
        public bool StepSubactions(int steps)
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
        public void StopExecutingAction()
        {
            Cleanup();
        }

        /// <summary>
        /// The specified battler is dead, so if we're running an action, we
        /// need to remove it from any target lists it might be on.
        /// If it's _using_ an action, we need to stop that entirely.
        /// </summary>
        public void BattlerIsDead(Battler b)
        {
            targets.Remove(b);
            alternateTargets.Remove(b);
            if (currentActingBattler == b) StopExecutingAction();
        }

        /// <summary>
        /// Handles all remaining subactions, then stops executing the current action.
        /// Returns true if any subaction does anything.
        /// </summary>
        public bool FinishCurrentAction()
        {
            bool r = StepSubactions(int.MaxValue); // we just call StepSubactions with a very big int - because StepSubactions keeps you from stepping past the end of the array, giving it int.MaxValue causes it to just step until it can't step any further
            return r;
        }
    }
}
