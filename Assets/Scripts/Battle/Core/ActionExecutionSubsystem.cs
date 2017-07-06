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
        /// <summary>
        /// Stores a subaction reference, plus metadata for tracking if it's been fired off.
        /// </summary>
        protected class RuntimeSubaction
        {
            public readonly ActionExecutionSubsystem parent;
            public readonly BattleAction.Subaction subaction;
            public bool fired { get; private set; }
            public int[] finalDamageFigures { get; private set; }
            public bool[] targetsHit { get; private set; }
            public bool[] effectPackagesNonFailures { get; private set; }
            public bool[][] effectPackagesSuccesses { get; private set; }
            public bool anySucceeded { get; private set; }
            public int finalAvgDamage { get; private set; }
            private int fxExecutionIndex;
            private BattleAction.Subaction damageDeterminant;
            private BattleAction.Subaction predicate;
            private BattleAction.Subaction successDeterminant;

            public RuntimeSubaction (ActionExecutionSubsystem _parent, BattleAction.Subaction _subaction)
            {
                parent = _parent;
                subaction = _subaction;
                fired = false;
                effectPackagesNonFailures = new bool[subaction.effectPackages.Length];
                effectPackagesSuccesses = new bool[subaction.effectPackages.Length][];
                if (subaction.useAlternateTargetSet)
                {
                    finalDamageFigures = new int[parent.alternateTargets.Count];
                    targetsHit = new bool[parent.alternateTargets.Count];             
                    for (int i = 0; i < subaction.effectPackages.Length; i++) effectPackagesSuccesses[i] = new bool[parent.alternateTargets.Count];
                }
                else
                {
                    finalDamageFigures = new int[parent.targets.Count];
                    targetsHit = new bool[parent.targets.Count];
                    for (int i = 0; i < subaction.effectPackages.Length; i++) effectPackagesSuccesses[i] = new bool[parent.targets.Count];
                }
                if (subaction.damageDeterminantName != string.Empty && parent.actionInExecution.subactions.ContainsKey(subaction.damageDeterminantName))
                {
                    damageDeterminant = parent.actionInExecution.subactions[subaction.damageDeterminantName];
                }
                if (subaction.predicateName != string.Empty && parent.actionInExecution.subactions.ContainsKey(subaction.predicateName))
                {
                    predicate = parent.actionInExecution.subactions[subaction.predicateName];
                }
                if (subaction.successDeterminantName != string.Empty && parent.actionInExecution.subactions.ContainsKey(subaction.successDeterminantName))
                {
                    successDeterminant = parent.actionInExecution.subactions[subaction.successDeterminantName];
                } 
            }

            /// <summary>
            /// Fire off this subaction
            /// </summary>
            public bool Fire ()
            {
                if (predicate != null)
                {
                    RuntimeSubaction predicateRuntime = parent.GetRuntimeDataFor(predicate);
                    if (!predicateRuntime.fired) predicateRuntime.Fire();
                }
                if (fired) Util.Crash("Can't fire a single subaction more than once during action execution");
                fired = true;
                List<Battler> t;
                anySucceeded = false;
                if (subaction.useAlternateTargetSet) t = parent.alternateTargets;
                else t = parent.targets;
                for (int targetIndex = 0; targetIndex < t.Count; targetIndex++)
                {
                    targetsHit[targetIndex] = HandleSubactionForTargetIndex(targetIndex, t);
                    if (targetsHit[targetIndex] == true) anySucceeded = true;
                }
                for (fxExecutionIndex = 0; fxExecutionIndex < subaction.effectPackages.Length; fxExecutionIndex++)
                {
                    effectPackagesNonFailures[fxExecutionIndex] = HandleEffectPackage(subaction.effectPackages[fxExecutionIndex], t);
                    if (effectPackagesNonFailures[fxExecutionIndex]) anySucceeded = true;
                }
                finalAvgDamage = 0;
                for (int i = 0; i < finalDamageFigures.Length; i++) finalAvgDamage += finalDamageFigures[i];
                finalAvgDamage = Mathf.RoundToInt((float)finalAvgDamage / finalDamageFigures.Length);
                return anySucceeded;
            }

            /// <summary>
            /// Handles the specified fx package.
            /// Since fx packages can apply effects even in the event that the subaction failed to inflict/heal damage,
            /// we have to iterate over all targets and check them individually, instead of doing that within the subaction success/fail check
            /// loop.
            /// </summary>
            private bool HandleEffectPackage(BattleAction.Subaction.EffectPackage effectPackage, List<Battler> t)
            {
                bool atLeastOneSuccess = false;
                for (int targetIndex = 0; targetIndex < t.Count; targetIndex++)
                {
                    effectPackagesSuccesses[fxExecutionIndex][targetIndex] = HandleEffectPackageForTargetIndex(effectPackage, targetIndex, t);
                    if (effectPackagesSuccesses[fxExecutionIndex][targetIndex]) atLeastOneSuccess = true;
                }
                return atLeastOneSuccess;
            }

            /// <summary>
            /// Checks to see if the fx package should be applied to target at the given index, and does that if so.
            /// Returns true if this succeeds, false otherwise.
            /// </summary>
            private bool HandleEffectPackageForTargetIndex(BattleAction.Subaction.EffectPackage effectPackage, int targetIndex, List<Battler> t)
            {
                bool executionSuccess = true;
                if (effectPackage.tieSuccessToEffectIndex > -1) executionSuccess = (effectPackagesSuccesses[effectPackage.tieSuccessToEffectIndex][targetIndex] == true);
                else if (!effectPackage.applyEvenIfSubactionMisses && !targetsHit[targetIndex]) executionSuccess = false;
                else if (effectPackage.baseSuccessRate < 1.0f) executionSuccess = t[targetIndex].TryToLandFXAgainstMe(parent.currentActingBattler, effectPackage);
                if (executionSuccess) t[targetIndex].ApplyFXPackage(effectPackage);
                return executionSuccess;
            }

            /// <summary>
            /// Checks to see if the subaction should succeed on specified target, and tells target to apply subaction if true.
            /// Returns subaction success/fail value.
            /// </summary>
            private bool HandleSubactionForTargetIndex(int targetIndex, List<Battler> t)
            {
                bool executionSuccess = false;
                if (successDeterminant != null)
                {
                    RuntimeSubaction runtimeSuccessDeterminant = parent.GetRuntimeDataFor(successDeterminant);
                    if (subaction.useAlternateTargetSet && parent.actionInExecution.alternateTargetType == ActionTargetType.Self || !subaction.useAlternateTargetSet && parent.actionInExecution.targetingType == ActionTargetType.Self ||
                        successDeterminant.useAlternateTargetSet && parent.actionInExecution.alternateTargetType == ActionTargetType.Self || !successDeterminant.useAlternateTargetSet && parent.actionInExecution.targetingType == ActionTargetType.Self)
                    {
                        executionSuccess = runtimeSuccessDeterminant.anySucceeded;
                    }
                    else executionSuccess = targetsHit[targetIndex];
                }
                else executionSuccess = t[targetIndex].TryToLandAttackAgainstMe(parent.currentActingBattler, subaction);
                if (executionSuccess)
                {
                    if (damageDeterminant != null)
                    {
                        RuntimeSubaction runtimeDamageDeterminant = parent.GetRuntimeDataFor(damageDeterminant);
                        if (subaction.useAlternateTargetSet != damageDeterminant.useAlternateTargetSet) finalDamageFigures[targetIndex] = runtimeDamageDeterminant.finalAvgDamage; // can't map exactly if the two subactions don't use the same target set, so average it
                        else finalDamageFigures[targetIndex] = runtimeDamageDeterminant.finalDamageFigures[targetIndex];
                    }
                    else finalDamageFigures[targetIndex] = t[targetIndex].CalcDamageAgainstMe(parent.currentActingBattler, subaction, true, true, true);
                    t[targetIndex].DealOrHealDamage(finalDamageFigures[targetIndex]);
                }
                return executionSuccess;
            }
        }
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
        private int currentSubactionIndex;
        /// <summary>
        /// Array of subaction references+metadata
        /// </summary>
        private RuntimeSubaction[] runtimeSubactions;
        /// <summary>
        /// Where are we in the current subaction's FX packages?
        /// </summary>
        private int subactionFXExecutionIndex;
        /// <summary>
        /// List of primary-type targets.
        /// Subactions will apply to these Battlers if they aren't using alternate targets.
        /// </summary>
        public List<Battler> targets { get; private set; }
        /// <summary>
        /// List of alternate-type targets.
        /// If subactions apply to alternate targets, these are those.
        /// </summary>
        public List<Battler> alternateTargets { get; private set; }

        /// <summary>
        /// Constructor for CurrentActionExecutionSubsystem.
        /// </summary>
        public ActionExecutionSubsystem(BattleData _battle)
        {
            battle = _battle;
            targets = new List<Battler>();
            alternateTargets = new List<Battler>();
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
                subactionFXExecutionIndex = 0;
                currentSubactionIndex = 0;
                runtimeSubactions = new RuntimeSubaction[action.subactions.Count];
                string[] keys = new string[action.subactions.Keys.Count];
                action.subactions.Keys.CopyTo(keys, 0);
                for (int i = 0; i < runtimeSubactions.Length; i++)
                {
                    runtimeSubactions[i] = new RuntimeSubaction(this, action.subactions[keys[i]]);
                }
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
            runtimeSubactions = null;
            targets.Clear();
            alternateTargets.Clear();
            currentSubactionIndex = 0;
        }

        /// <summary>
        /// Get runtime info for subaction.
        /// </summary>
        protected RuntimeSubaction GetRuntimeDataFor (BattleAction.Subaction subaction)
        {
            for (int i = 0; i < runtimeSubactions.Length; i++)
            {
                if (runtimeSubactions[i].subaction == subaction) return runtimeSubactions[i];
            }
            return null;
        }

        /// <summary>
        /// Fire all unfired subactions.
        /// </summary>
        public bool FireRemainingSubactions ()
        {
            bool r = false;
            for (int i = 0; i < runtimeSubactions.Length; i++)
            {
                if (!runtimeSubactions[i].fired)
                {
                    if (runtimeSubactions[i].Fire()) r = true;
                }
            }
            return r;
        }

        /// <summary>
        /// Fire off this subaction by name.
        /// </summary>
        public bool FireSubaction (string subactionName)
        {
            bool r = false;
            if (actionInExecution.subactions.ContainsKey(subactionName)) r = GetRuntimeDataFor(actionInExecution.subactions[subactionName]).Fire();
            return r;
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
    }
}
