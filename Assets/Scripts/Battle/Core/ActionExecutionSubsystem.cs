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
    public class ActionExecutionSubsystem
    {
        /// <summary>
        /// Object created when a BattleAction is processed.
        /// </summary>
        public class ActionHandle
        {
            /// <summary>
            /// The handle of the subaction that we're currently working on.
            /// </summary>
            public SubactionHandle currentSubactionHandle { get { if (currentSubactionIndex < subactionHandles.Length) return subactionHandles[currentSubactionIndex]; else return null; } }
            /// <summary>
            /// The BattleAction that this handle belongs to.
            /// </summary>
            public readonly BattleAction battleAction;
            /// <summary>
            /// The Battler using this action.
            /// </summary>
            public readonly Battler user;
            /// <summary>
            /// An array containing subaction handles for each of this action's subactions.
            /// </summary>
            public readonly SubactionHandle[] subactionHandles;
            /// <summary>
            /// The primary targets acquired for this action.
            /// </summary>
            public readonly List<Battler> primaryTargetSet;
            /// <summary>
            /// The secondary targets acquired for this action.
            /// </summary>
            public readonly List<Battler> alternateTargetSet;
            /// <summary>
            /// If true, we skip all subaction or effect package
            /// event blocks. We'll handle only the animSkip block.
            /// </summary>
            public bool skipMostEventBlocks { get; private set; }
            /// <summary>
            /// The callback that'll run after all of this action's subactions have been executed and its event blocks have finished processing.
            /// </summary>
            private readonly Action callback;
            /// <summary>
            /// Index that tracks which subaction handle we're currently processing.
            /// </summary>
            private int currentSubactionIndex;

            public ActionHandle (BattleAction _battleAction, Battler _user, List<Battler> _primaryTargetSet, List<Battler> _alternateTargetSet, Action _callback)
            {
                battleAction = _battleAction;
                user = _user;
                primaryTargetSet = _primaryTargetSet;
                alternateTargetSet = _alternateTargetSet;
                callback = _callback;
                subactionHandles = new SubactionHandle[battleAction.subactions.Length];
                for (int i = 0; i < battleAction.subactions.Length; i++) subactionHandles[i] = new SubactionHandle(this, battleAction.subactions[i], SubactionFinished);
                if (battleAction.onStart != null && !skipMostEventBlocks)
                {
                    BattleStage.instance.Dispatch(battleAction.onStart);
                    BattleStage.instance.onAllEventBlocksFinished += currentSubactionHandle.Process;
                }
                else currentSubactionHandle.Process(); // Kickstart action processing
            }

            /// <summary>
            /// Called by each of this action's subactions
            /// as a callback, after they finish processing.
            /// </summary>
            private void SubactionFinished ()
            {
                currentSubactionIndex++;
                // There's a much faster way to do dead target pruning but it's really, really ugly and kinda shits all over any pretensions to object-oriented design you have.
                for (int i = primaryTargetSet.Count; i > -1; i--) if (primaryTargetSet[i].isDead) primaryTargetSet.RemoveAt(i);
                for (int i = alternateTargetSet.Count; i > -1; i--) if (alternateTargetSet[i].isDead) alternateTargetSet.RemoveAt(i);
                for (int i = 0; i < subactionHandles.Length; i++) subactionHandles[i].PruneDeadTargets();
                if (currentSubactionIndex < subactionHandles.Length && !user.isDead) currentSubactionHandle.Process();
                else OutOfSubactions();
            }

            /// <summary>
            /// Skip any remaining event blocks and finish this action immediately.
            /// </summary>
            public void EndPrematurely ()
            {
                // Each time you EndPrematurely with subactions left to process, you immedaitely get the next handle in line.
                // So this loop will continue aborting those until you run out of subactions.
                while (currentSubactionHandle != null) currentSubactionHandle.EndPrematurely();
            }

            /// <summary>
            /// Gets the handle corresponding to the given subaction.
            /// </summary>
            public SubactionHandle GetHandleFor (BattleAction.Subaction subaction)
            {
                for (int i = 0; i < subactionHandles.Length; i++)
                {
                    if (subactionHandles[i].subaction == subaction) return subactionHandles[i];
                }
                return null;
            }

            /// <summary>
            /// Called when we finish our subactions.
            /// </summary>
            private void OutOfSubactions ()
            {
                if (!user.isDead)
                {
                    EventBlockHandle eventBlockHandle;
                    if (!skipMostEventBlocks) eventBlockHandle = BattleStage.instance.Dispatch(battleAction.onConclusion, callback);
                    else eventBlockHandle = BattleStage.instance.Dispatch(battleAction.animSkip, callback);
                    if (eventBlockHandle == null) callback();
                }
                else callback();
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
                damageDeterminant = actionHandle.GetHandleFor(subaction.damageDeterminant);
                successDeterminant = actionHandle.GetHandleFor(subaction.successDeterminant);
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

            /// <summary>
            /// Remove dead targets from target list.
            /// </summary>
            public void PruneDeadTargets ()
            {
                for (int i = targets.Count; i > -1; i--) if (targets[i].isDead) targets.RemoveAt(i);
            }

            /// <summary>
            /// End processing of this subaction early.
            /// </summary>
            public void EndPrematurely ()
            {
                BattleStage.instance.CancelEventBlocks();
            }
        }

        /// <summary>
        /// The BattleData object to which this action execution subsystem belongs.
        /// </summary>
        private readonly BattleData battleData;
        /// <summary>
        /// The action that we're working on right now.
        /// </summary>
        public ActionHandle currentAction { get; private set; }

        /// <summary>
        /// Constructor: This ActionExecutionSystem belongs to the BattleData object given.
        /// </summary>
        public ActionExecutionSubsystem (BattleData _battleData)
        {
            battleData = _battleData;
        }

        /// <summary>
        /// Start handling the given BattleActiom, given the selected user and target sets.
        /// Callback will be executed upon completion of the action.
        /// </summary>
        public void BeginAction (BattleAction _battleAction, Battler _user, Battler[] _primaryTargetSet, Battler[] _alternateTargetSet, Action _callback)
        {
            if (currentAction != null) Util.Crash("Attempted to start executing " + _battleAction.actionID + " while holding a live handle for " + currentAction.battleAction.actionID);
            else
            {
                Action callback = () =>
                {
                    currentAction = null;
                    _callback();
                };
                currentAction = new ActionHandle(_battleAction, _user, new List<Battler>(_primaryTargetSet), new List<Battler>(_alternateTargetSet), callback);
            }
        }
    }
}