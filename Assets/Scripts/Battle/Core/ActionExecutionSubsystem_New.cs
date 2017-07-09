using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CnfBattleSys
{
    /// <summary>
    /// Battle overseer subsystem processing that handles the action we're currently executing.
    /// This is sort of fiddly, so I've taken the liberty of docstringing the hell out of it.
    /// </summary>
    public class ActionExecutionSubsystem_New
    {
        /// <summary>
        /// Object created when a BattleAction is processed.
        /// </summary>
        public class ActionHandle
        {
            /// <summary>
            /// The BattleAction that this handle belongs to.
            /// </summary>
            public readonly BattleAction battleAction;
            /// <summary>
            /// The Battler using this action.
            /// </summary>
            public readonly Battler user;
            /// <summary>
            /// A dictionary containing subaction handles for each of this action's subactions.
            /// </summary>
            public readonly Dictionary<string, SubactionHandle> subactionsDict;
            /// <summary>
            /// The primary targets acquired for this action.
            /// </summary>
            public readonly List<Battler> primaryTargetSet;
            /// <summary>
            /// The secondary targets acquired for this action.
            /// </summary>
            public readonly List<Battler> alternateTargetSet;
            /// <summary>
            /// The callback that'll run after all of this action's subactions have been executed and its event blocks have finished processing.
            /// </summary>
            private readonly Action callback;

            public ActionHandle (BattleAction _battleAction, Battler _user, List<Battler> _primaryTargetSet, List<Battler> _alternateTargetSet, Action _callback)
            {
                // actually kill attackanimcontroller.
                // battleAction includes a string[] sequence to call subactions in.
                // ...and we don't even need dicts at runtime, hurr durr!
                // compile to an array.
                // oh great I'm going back to fucking BattleActionTool please kill me
                // Each event block should also include a camera script of some sort although
                // idk what that looks like...
                battleAction = _battleAction;
                user = _user;
                primaryTargetSet = _primaryTargetSet;
                alternateTargetSet = _alternateTargetSet;
                callback = _callback;
                subactionsDict = new Dictionary<string, SubactionHandle>(battleAction.subactions.Count);
                string[] keys = new string[battleAction.subactions.Keys.Count];
                battleAction.subactions.Keys.CopyTo(keys, 0);
                for (int i = 0; i < keys.Length; i++) subactionsDict[keys[i]] = new SubactionHandle(this, battleAction.subactions[keys[i]], SubactionFinished);
            }

            /// <summary>
            /// Called by each of this action's subactions
            /// as a callback, after they finish processing.
            /// </summary>
            private void SubactionFinished ()
            {

            }
        }

        /// <summary>
        /// Object created when an effect package is processed.
        /// Keeps track of the state of any event blocks
        /// this effect dispatched, and the results of
        /// processing it.
        /// </summary>
        public class EffectPackageHandle
        {
            /// <summary>
            /// The outcome of processing this effect package.
            /// </summary>
            public enum Result
            {
                /// <summary>
                /// The effect hasn't actually been processed yet.
                /// </summary>
                Undetermined,
                /// <summary>
                /// Didn't do shit.
                /// </summary>
                Failure,
                /// <summary>
                /// The effects this package handles were applied
                /// to some targets, but not all of them.
                /// (This should never be used as the result for
                /// a single target!)
                /// </summary>
                PartialSuccess,
                /// <summary>
                /// The effect was applied successfully to all targets.
                /// </summary>
                Success
            }

            /// <summary>
            /// The base handle for tha BattleAction this effect package is attached to.
            /// </summary>
            public readonly ActionHandle actionHandle;
            /// <summary>
            /// The handle for the subaction this effect package is attached to.
            /// </summary>
            public readonly SubactionHandle subactionHandle;
            /// <summary>
            /// The effect package that this handle is attached to.
            /// </summary>
            public readonly BattleAction.Subaction.EffectPackage effectPackage;
            /// <summary>
            /// The overall result of processing the effect package.
            /// </summary>
            public readonly Result result;
            /// <summary>
            /// The individual results of processing the effect package,
            /// matched to indices in parent.targets.
            /// </summary>
            public readonly Result[] resultsByTarget;

            /// <summary>
            /// Constructor: Fires off the effect package and sets up the handle's state based on that.
            /// Fires off callback when the event block tied to this effect package has been handled,
            /// or immediately if there's no event block.
            /// </summary>
            public EffectPackageHandle (SubactionHandle _subactionHandle, BattleAction.Subaction.EffectPackage _effectPackage)
            {
                subactionHandle = _subactionHandle;
                actionHandle = subactionHandle.actionHandle;
                effectPackage = _effectPackage;
                resultsByTarget = new Result[subactionHandle.targets.Count];
                // We don't worry about whether or not to handle event blocks based on anim skipping or anything
                // until we actually dispatch them to the BattleStage. BattleStage.Dispatch does the lifting there.
                // This is a minor efficiency loss but keeps this code much simpler.
                if (effectPackage.eventBlock != null) subactionHandle.eventBlocksQueue.Enqueue(effectPackage.eventBlock);
                if (effectPackage.tieSuccessToEffectIndex > -1)
                {
                    result = subactionHandle.effectPackageHandles[effectPackage.tieSuccessToEffectIndex].result;
                    resultsByTarget = subactionHandle.effectPackageHandles[effectPackage.tieSuccessToEffectIndex].resultsByTarget;
                }
                else
                {
                    bool overallSucceeded = true;
                    bool overallFailed = true;
                    for (int i = 0; i < subactionHandle.targets.Count; i++)
                    {
                        resultsByTarget[i] = ApplyToTargetIndex(i);
                        if (resultsByTarget[i] == Result.Success) overallFailed = false;
                        else if (resultsByTarget[i] == Result.Failure) overallSucceeded = false;
                    }
                    // Either these are both false or there are no targets, in which case our result doesn't matter because this is just a battle scripting thing.
                    if (overallSucceeded == overallFailed) result = Result.PartialSuccess; 
                    else if (overallSucceeded) result = Result.Success;
                    else result = Result.Failure;
                }      
            }

            /// <summary>
            /// Applies this effect package to target at index.
            /// </summary>
            private Result ApplyToTargetIndex (int targetIndex)
            {
                Func<Result> hitConfirmed = () =>
                {
                    subactionHandle.targets[targetIndex].ApplyFXPackage(effectPackage);
                    return Result.Success;
                };
                if (!effectPackage.applyEvenIfSubactionMisses && subactionHandle.resultsByTarget[targetIndex] == SubactionHandle.TargetResult.Miss) return Result.Failure;
                else
                {
                    if (effectPackage.baseSuccessRate < 1.0f)
                    {
                        if (subactionHandle.targets[targetIndex].TryToLandFXAgainstMe(actionHandle.user, effectPackage)) return hitConfirmed();
                        else return Result.Failure;
                    }
                    else return hitConfirmed();
                }
            }
        }

        /// <summary>
        /// Object created when a subaction is processed.
        /// Keeps track of the state of any effect packages
        /// or event blocks that this subaction dispatched,
        /// and the results of processing it.
        public class SubactionHandle
        {
            /// <summary>
            /// The outcome of processing this subaction.
            /// </summary>
            public enum Result
            {
                /// <summary>
                /// The subaction hasn't actually been processed yet.
                /// </summary>
                Undetermined,
                /// <summary>
                /// Didn't do shit.
                /// </summary>
                Failure,
                /// <summary>
                /// This subaction applied to a subset of its targets.
                /// </summary>
                PartialSuccess,
                /// <summary>
                /// The subaction was applied successfully to all targets.
                /// </summary>
                Success
            }
            /// <summary>
            /// The outcome of processing this subaction
            /// for a single target.
            /// </summary>
            public enum TargetResult
            {
                /// <summary>
                /// The subaction hasn't actually been processed yet.
                /// </summary>
                Undetermined,
                /// <summary>
                /// This subaction doesn't do damage or perform
                /// standard accuracy calculations.
                /// </summary>
                NotApplicable,
                /// <summary>
                /// Failed to hit.
                /// </summary>
                Miss,
                /// <summary>
                /// Landed a hit, touched target HP.
                /// (Whether this is considered hit or
                /// heal is based on whether we did
                /// positive or negative dmg.)
                /// </summary>
                HitOrHealed,
                /// <summary>
                /// Landed a hit but dealt no damage.
                /// </summary>
                NoSell
            }

            /// <summary>
            /// The base handle for tha BattleAction this subaction is attached to.
            /// </summary>
            public readonly ActionHandle actionHandle;
            /// <summary>
            /// Handles for each of this subaction's effect packages.
            /// </summary>
            public readonly EffectPackageHandle[] effectPackageHandles;
            /// <summary>
            /// The subaction that this handle is attached to.
            /// </summary>
            public readonly BattleAction.Subaction subaction;
            /// <summary>
            /// The target list this handle should point to.
            /// </summary>
            public readonly List<Battler> targets;
            /// <summary>
            /// Queue of event blocks tied to this subaction that it should
            /// dispatch before firing callback.
            /// </summary>
            public readonly Queue<EventBlock> eventBlocksQueue = new Queue<EventBlock>(16);
            /// <summary>
            /// The overall result of processing the subaction.
            /// </summary>
            public Result result { get; private set; }
            /// <summary>
            /// The individual results of processing the subaction,
            /// matched to indices in the target list.
            /// </summary>
            public readonly TargetResult[] resultsByTarget;
            /// <summary>
            /// Individual final damage figures, matched
            /// to indices in the target list.
            /// </summary>
            public readonly int[] damageFiguresByTarget;
            /// <summary>
            /// Handle for the subaction that determines
            /// damage figures for this one.
            /// </summary>
            private readonly SubactionHandle damageDeterminant;
            /// <summary>
            /// Handle for the subaction that determines
            /// success/failure for this one.
            /// </summary>
            private readonly SubactionHandle successDeterminant;
            /// <summary>
            /// Callback that will be fired after the subaction has finished and
            /// all event blocks are done.
            /// </summary>
            private Action callback;

            /// <summary>
            /// Constructor: Prepare a subaction handle. This doesn't execute the subaction until Process() is called.
            /// </summary>
            public SubactionHandle (ActionHandle _actionHandle, BattleAction.Subaction _subaction, Action _callback)
            {
                actionHandle = _actionHandle;
                subaction = _subaction;
                if (subaction.useAlternateTargetSet) targets = actionHandle.alternateTargetSet;
                else targets = actionHandle.primaryTargetSet;
                if (subaction.damageDeterminantName != string.Empty) damageDeterminant = actionHandle.subactionsDict[subaction.damageDeterminantName];
                if (subaction.successDeterminantName != string.Empty) successDeterminant = actionHandle.subactionsDict[subaction.successDeterminantName];
                callback = _callback;
                effectPackageHandles = new EffectPackageHandle[subaction.effectPackages.Length];
                damageFiguresByTarget = new int[targets.Count];
                resultsByTarget = new TargetResult[targets.Count];
            }

            /// <summary>
            /// Process the subaction tied to this handle.
            /// </summary>
            public void Process ()
            {
                if (subaction.eventBlock != null) eventBlocksQueue.Enqueue(subaction.eventBlock);
                if (subaction.predicateName != string.Empty && actionHandle.subactionsDict[subaction.predicateName].result == Result.Undetermined) Util.Crash(actionHandle.battleAction.actionID + " executes a subaction before its predicate");
                else
                {
                    if (damageDeterminant != null)
                    {
                        if (targets == damageDeterminant.targets)
                        {
                            for (int i = 0; i < damageFiguresByTarget.Length; i++)
                            {
                                damageFiguresByTarget[i] = damageDeterminant.damageFiguresByTarget[i];
                                if (subaction.baseDamage < 0 && damageDeterminant.subaction.baseDamage > 0 || subaction.baseDamage > 0 && damageDeterminant.subaction.baseDamage < 0) damageFiguresByTarget[i] = -damageFiguresByTarget[i];
                            }
                        }
                        else
                        {
                            int d = Util.Mean(damageDeterminant.damageFiguresByTarget);
                            if (subaction.baseDamage < 0 && damageDeterminant.subaction.baseDamage > 0 || subaction.baseDamage > 0 && damageDeterminant.subaction.baseDamage < 0) d = -d;
                            for (int i = 0; i < damageFiguresByTarget.Length; i++) damageFiguresByTarget[i] = d;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < targets.Count; i++) damageFiguresByTarget[i] = targets[i].CalcDamageAgainstMe(actionHandle.user, subaction, true, true, true);
                    }
                    if (successDeterminant != null)
                    {
                        if (targets == successDeterminant.targets)
                        {
                            for (int i = 0; i < resultsByTarget.Length; i++) resultsByTarget[i] = successDeterminant.resultsByTarget[i];
                        }
                        else
                        {
                            TargetResult bestTargetResult = TargetResult.NoSell;
                            for (int i = 0; i < successDeterminant.resultsByTarget.Length; i++)
                            {
                                if (successDeterminant.resultsByTarget[i] == TargetResult.NotApplicable)
                                {
                                    bestTargetResult = TargetResult.NotApplicable;
                                    break;
                                }
                                else if (successDeterminant.resultsByTarget[i] == TargetResult.Miss)
                                {
                                    if (bestTargetResult != TargetResult.HitOrHealed) bestTargetResult = TargetResult.Miss;
                                }
                                else if (successDeterminant.resultsByTarget[i] == TargetResult.HitOrHealed) bestTargetResult = TargetResult.HitOrHealed;
                            }
                            for (int i = 0; i < resultsByTarget.Length; i++) resultsByTarget[i] = bestTargetResult;
                        }
                        result = successDeterminant.result;
                    }
                    else
                    {
                        bool anySucceeded = false;
                        bool anyFailed = false;
                        for (int i = 0; i < resultsByTarget.Length; i++)
                        {
                            if (subaction.baseDamage == 0 && subaction.baseAccuracy == 0)
                            {
                                resultsByTarget[i] = TargetResult.NotApplicable;
                                result = Result.Success;
                            }
                            else if (targets[i].TryToLandAttackAgainstMe(actionHandle.user, subaction))
                            {
                                if (damageFiguresByTarget[i] != 0)
                                {
                                    resultsByTarget[i] = TargetResult.HitOrHealed;
                                    anySucceeded = true;
                                }
                                else
                                {
                                    resultsByTarget[i] = TargetResult.NoSell;
                                    anyFailed = true;
                                }
                            }
                            else
                            {
                                resultsByTarget[i] = TargetResult.Miss;
                                anyFailed = true;
                                damageFiguresByTarget[i] = 0; // Lose whatever figure you had determined if you miss.
                            }
                        }
                        if (result != Result.Success)
                        {
                            if (anySucceeded && !anyFailed) result = Result.Success;
                            else if (anySucceeded && anyFailed) result = Result.PartialSuccess;
                            else result = Result.Failure;
                        }
                    }
                    for (int i = 0; i < targets.Count; i++) targets[i].DealOrHealDamage(damageFiguresByTarget[i]);
                    for (int i = 0; i < effectPackageHandles.Length; i++) effectPackageHandles[i] = new EffectPackageHandle(this, subaction.effectPackages[i]);
                    if (eventBlocksQueue.Count > 0)
                    {
                        while (eventBlocksQueue.Count > 0) BattleStage.instance.Dispatch(eventBlocksQueue.Dequeue());
                        BattleStage.instance.onAllEventBlocksFinished += callback;
                    }
                    else callback();
                }
            }
        }
    }
}