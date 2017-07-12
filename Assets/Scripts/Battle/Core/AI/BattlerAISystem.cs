using System;
using System.Collections.Generic;
using UnityEngine;
using CnfBattleSys.AI;
using MovementEffects;

namespace CnfBattleSys
{
    /// <summary>
    /// Static class that contains all of the different functions that Battlers can (depending on their AI type)
    /// use to determine what they're gonna do.
    /// If changeStances is true, we decide on a stance, too.
    /// AI-driven units select stance and actions at the same time; player-controlled units
    /// have to select a stance before we can talk about what they're gonna do, since
    /// we're waiting on player input in that case and the options we give the player
    /// are a function of the stance the unit is in.
    /// </summary>
    public static class BattlerAISystem
    {
        /// <summary>
        /// Data structure containing the final targets (and all associated scores) for a specific action.
        /// </summary>
        public struct ScoredActionTargets
        {
            public readonly BattleAction action;
            public readonly Battler[] primaryTargets;
            public readonly Battler[] secondaryTargets;
            public readonly float[] primaryScores;
            public readonly float[] secondaryScores;
            public readonly float totalScore;

            public ScoredActionTargets (BattleAction _action, Battler[] _primaryTargets, Battler[] _secondaryTargets, float[] _primaryScores, float[] _secondaryScores)
            {
                action = _action;
                primaryTargets = _primaryTargets;
                secondaryTargets = _secondaryTargets;
                primaryScores = _primaryScores;
                secondaryScores = _secondaryScores;
                float ts = 0;
                int q = 0;
                if (primaryScores.Length > 0)
                {
                    for (int i = 0; i < primaryScores.Length; i++)
                    {
                        ts += primaryScores[i];
                        q++;
                    }
                    for (int i = 0; i < secondaryScores.Length; i++)
                    {
                        ts += secondaryScores[i];
                        q++;
                    }
                    ts /= q;
                    totalScore = ts;
                }
                else totalScore = 0;
            }
        }

        /// <summary>
        /// Starts the AI on deciding what the unit should do.
        /// Thia is basically just an intermediary that goes between the Battler and
        /// the individual AI modules, so as to keep the (already pretty gnarly) Battler
        /// object from having to deal with the additional headache of managing this
        /// huge-ass jump table. (Well it's not huge _yet_ but, architecturally: this
        /// table is fat af.)
        /// </summary>
        public static void StartThinking (Battler b, bool changeStances, Action callback)
        {
            Battler.TurnActions turnActions = Battler.defaultTurnActions;
            BattlerAIMessageFlags messageFlags = BattlerAIMessageFlags.None;
            bool outputIsDelayed = false; // if the module we're running can't return output immediately, either because it requires player input or because we're doing something inane and unsafe with the AI itself
            switch (b.aiType)
            {
                case BattlerAIType.None:
                    callback();
                    break; // if the battler says it doesn't have any AI, we dutifully decline to provide the battler with AI
                case BattlerAIType.PlayerSide_ManualControl:
                    outputIsDelayed = true;
                    AIModule_PlayerSide_ManualControl.GetTurnActionsFromPlayer(b, changeStances, callback);
                    break;
                case BattlerAIType.TestAI:
                    AIModule_TestAI.DecideTurnActions_AndStanceIfApplicable(b, changeStances, out turnActions, out messageFlags);
                    callback();
                    break;
                default:
                    Util.Crash(new Exception("No entry in AI function jump table for AI type of: " + b.aiType.ToString()));
                    break;
            }
            if (!outputIsDelayed) b.ReceiveAThought(turnActions, messageFlags);
            // If the AI module delays output, it needs to assume responsibility for passing a thought onto the Battler itself; keep that in mind.
        }

        /// <summary>
        /// Gets optimum actions for a turn given a set of AI flags, an acting battler, and stance change difficulty.
        /// stanceChangeDifficulty functions as a multiplier applied to the score of the action associated with the battler's current stance.
        /// Think of it as representing the extend to which the battler prefers not to change stances. Since you can't act on the turn that you change stances,
        /// stanceChangeDifficulty should normally be pretty high - you need to be talking about something substantially better than anything you can do in this stance.
        /// </summary>
        public static Battler.TurnActions GetOptimumActionsForTurn (BattlerAIFlags flags, Battler user, float stanceChangeDifficulty)
        {
            float highestScore = float.MinValue;
            BattleAction actionDecidedUpon = ActionDatabase.SpecialActions.defaultBattleAction;
            BattleStance stance = user.currentStance;
            Battler[] primaryTargets = new Battler[0];
            Battler[] secondaryTargets = new Battler[0];
            bool changeStances = true;
            ScoredActionTargets[] optimalActionsPerStance = GetOptimumActionsForEachStance(flags, user);
            for (int i = 0; i < optimalActionsPerStance.Length; i++)
            {
                if (user.stances[i] == user.lockedStance) break; // break immediately if stance is forbidden
                float score = optimalActionsPerStance[i].totalScore;
                if (user.stances[i] == user.currentStance) score *= stanceChangeDifficulty;
                if (score > highestScore || (score == highestScore && user.stances[i] == user.currentStance))
                {
                    highestScore = score;
                    actionDecidedUpon = optimalActionsPerStance[i].action;
                    primaryTargets = optimalActionsPerStance[i].primaryTargets;
                    secondaryTargets = optimalActionsPerStance[i].secondaryTargets;
                    changeStances = (user.stances[i] != user.currentStance);
                    stance = user.stances[i];
                }
            }
            return new Battler.TurnActions(changeStances, 0.0f, primaryTargets, secondaryTargets, actionDecidedUpon, stance);
        }

        /// <summary>
        /// Returns an array of ScoredActionTargets (index-matched to the battler's stances) containing the most optimum action and target set for it to use in each stance.
        /// </summary>
        private static ScoredActionTargets[] GetOptimumActionsForEachStance (BattlerAIFlags flags, Battler user)
        {
            ScoredActionTargets[] output = new ScoredActionTargets[user.stances.Length];
            BattleStance originalStance = user.currentStance;
            BattleAction[] currentStanceActions = new BattleAction[user.currentStance.actionSet.Length + user.metaStance.actionSet.Length];
            for (int i = 0; i < user.currentStance.actionSet.Length; i++) currentStanceActions[i] = user.currentStance.actionSet[i];
            for (int i = 0; i < user.metaStance.actionSet.Length; i++) currentStanceActions[user.currentStance.actionSet.Length + i] = user.metaStance.actionSet[i];
            for (int s = 0; s < user.stances.Length; s++)
            {
                ScoredActionTargets[] scoresForStance;
                user.ChangeStance_ImmediateProvisional(user.stances[s]); // guve the battker a provisional stance so its stats are right when we're running damage calcs, etc.
                if (user.stances[s] == originalStance) scoresForStance = GetScoresAndOptimumTargetSets(flags, user, currentStanceActions);
                else scoresForStance = GetScoresAndOptimumTargetSets(flags, user, user.stances[s].actionSet);
                float thisStanceHighestScore = float.MinValue;
                ScoredActionTargets thisStanceBestAction = new ScoredActionTargets(ActionDatabase.SpecialActions.defaultBattleAction, new Battler[0], new Battler[0], new float[0], new float[0]);
                for (int t = 0; t < scoresForStance.Length; t++)
                {
                    if (scoresForStance[t].totalScore > thisStanceHighestScore)
                    {
                        thisStanceHighestScore = scoresForStance[t].totalScore;
                        thisStanceBestAction = scoresForStance[t];
                    }
                }
                if (thisStanceBestAction.action != ActionDatabase.SpecialActions.defaultBattleAction) output[s] = thisStanceBestAction;
            }
            user.ChangeStance_ImmediateProvisional(originalStance);
            return output;
        }

        /// <summary>
        /// Given AI flags, user, and an array of actions, returns final score and optimum target sets for each of those actions.
        /// </summary>
        private static ScoredActionTargets[] GetScoresAndOptimumTargetSets (BattlerAIFlags flags, Battler user, BattleAction[] actions)
        {
            ScoredActionTargets[] output = new ScoredActionTargets[actions.Length];
            for (int a = 0; a < actions.Length; a++)
            {
                Battler[][] potentialTargets = FindLegalTargetsForAction(user, actions[a]);
                output[a] = GetOptimumTargets(flags, user, actions[a], potentialTargets);
            }
            return output;
        }

        /// <summary>
        /// Gets optimum primary and secondary targets (and total action score) for the given user, action, and set of potential targets.
        /// </summary>
        private static ScoredActionTargets GetOptimumTargets (BattlerAIFlags flags, Battler user, BattleAction action, Battler[][] jointPotentialTargets)
        {
            float[] scores = new float[0];
            Func<ActionTargetType, Battler[], Battler[]> forNone = (targetingType, potentialTargets) =>
            {
                return new Battler[0];
            };
            Func<ActionTargetType, Battler[], Battler[]> forSingle = (targetingType, potentialTargets) =>
            {
                if (potentialTargets.Length < 1) Util.Crash(new Exception("Can't pick optimum target unless you actually provide some targets."));
                scores = ScoreTargets(flags, user, action, potentialTargets);
                // Since this is a single-target action, we don't need to do any additional processing to the scores
                float highestScoreBuffer = float.MinValue;
                Battler optimumTarget = null;
                for (int i = 0; i < scores.Length; i++)
                {
                    if (scores[i] > highestScoreBuffer)
                    {
                        highestScoreBuffer = scores[i];
                        optimumTarget = potentialTargets[i];
                    }
                }
                return new Battler[] { optimumTarget };
            };
            Func<ActionTargetType, Battler[], Battler[]> forAOE = (targetingType, potentialTargets) =>
            {
                if (potentialTargets.Length < 1) Util.Crash(new Exception("Can't pick optimum target unless you actually provide some targets."));
                scores = ScoreTargets(flags, user, action, potentialTargets);
                Battler[][] subtargetsForAOE = new Battler[potentialTargets.Length][];
                for (int t = 0; t < potentialTargets.Length; t++)
                {
                    subtargetsForAOE[t] = BattleOverseer.currentBattle.GetBattlersWithinAOERangeOf(user, potentialTargets[t], targetingType, action.baseAOERadius, potentialTargets);
                    for (int inAOERadIndex = 0; inAOERadIndex < subtargetsForAOE[t].Length; inAOERadIndex++)
                    {
                        float foundScore = float.NaN;
                        for (int pt = 0; pt < potentialTargets.Length; pt++)
                        {
                            if (subtargetsForAOE[t][inAOERadIndex] == potentialTargets[pt])
                            {
                                foundScore = scores[pt];
                                break;
                            }
                        }
                        scores[t] += foundScore;
                    }
                }
                float highestScoreBuffer = float.MinValue;
                int optimumTargetIndex = int.MinValue;
                for (int i = 0; i < scores.Length; i++)
                {
                    if (scores[i] > highestScoreBuffer)
                    {
                        highestScoreBuffer = scores[i];
                        optimumTargetIndex = i;
                    }
                }
                Battler[] output = new Battler[subtargetsForAOE[optimumTargetIndex].Length + 1];
                output[0] = potentialTargets[optimumTargetIndex];
                for (int i = 1; i < output.Length; i++)
                {
                    output[i] = subtargetsForAOE[optimumTargetIndex][i - 1];
                }
                return output;
            };
            Func<ActionTargetType, Battler[], Battler[]> forAll = (targetingType, potentialTargets) =>
            {
                return potentialTargets; // if we're acting on all potential targets, we don't need to do any processing, we just immediately spit the potentialTargets back
            };
            Func<ActionTargetType, bool, Func<ActionTargetType, Battler[], Battler[]>> setTargetAcquisitionFunc = (targetingType, failGracefully) =>
            {
                switch (targetingType)
                {
                    case ActionTargetType.SingleTarget:
                    case ActionTargetType.Self:
                    case ActionTargetType.LineOfSightSingle:
                        return forSingle;
                    case ActionTargetType.AllTargetsInRange:
                        return forAll;
                    case ActionTargetType.LineOfSightPiercing:
                    case ActionTargetType.CircularAOE:
                        return forAOE;
                    case ActionTargetType.None:
                        if (failGracefully) return forNone;
                        else Util.Crash(new Exception("Tried to find targets for an action that doesn't take targets. Wut factor: at least 8 or 9."));
                        return default(Func<ActionTargetType, Battler[], Battler[]>);
                    default:
                        Util.Crash(new Exception("Tried to acquire targets for invalid targeting type: " + targetingType));
                        return default(Func<ActionTargetType, Battler[], Battler[]>);
                }
            };
            Func<ActionTargetType, Battler[], Battler[]> primaryTargetsAcquisition = setTargetAcquisitionFunc(action.targetingType, false);
            Func<ActionTargetType, Battler[], Battler[]> secondaryTargetsAcquisition = setTargetAcquisitionFunc(action.alternateTargetType, true);
            Battler[] primaryTargets = primaryTargetsAcquisition(action.targetingType, jointPotentialTargets[0]);
            float[] primaryScores = scores;
            Battler[] secondaryTarget = secondaryTargetsAcquisition(action.alternateTargetType, jointPotentialTargets[1]);
            float[] secondaryScores = scores;
            return new ScoredActionTargets(action, primaryTargets, secondaryTarget, primaryScores, secondaryScores);
        }

        /// <summary>
        /// Determines total efficacy scores for a single action on each potential target it could be used against.
        /// Scores each potential target for the action on the criteria of each of that action's category flags.
        /// That is to say - for example - if an action is both an attack and a debuff or both a heal and a buff,
        /// we evaluate it in both of those contexts, determine what the most optimal overall target is (if a heal and a buff, the friendly unit that gets the most benefit from either of those angles)
        /// and target that.
        /// Returns an array of floats, index-matched to the potentialTargets input array, containing the total score for each of those targets.
        /// Single-target attacks can go on to use those scores directly; AOE attacks
        /// will need to so further calculations to determine the scores for each _set_ of targets.
        /// </summary>
        private static float[] ScoreTargets (BattlerAIFlags flags, Battler user, BattleAction action, Battler[] potentialTargets)
        {
            float[] attackTargetsScores;
            float[] healTargetsScores;
            float[] buffTargetsScores;
            float[] debuffTargetsScores;
            Func<float[]> populateZeroedScoreArray = () =>
            {
                float[] scoreArray = new float[potentialTargets.Length];
                for (int i = 0; i < scoreArray.Length; i++) scoreArray[i] = 0;
                return scoreArray;
            };
            if ((action.categoryFlags & BattleActionCategoryFlags.Attack) == BattleActionCategoryFlags.Attack) attackTargetsScores = ScoreTargets_Damaging(flags, user, action, potentialTargets);
            else attackTargetsScores = populateZeroedScoreArray();
            if ((action.categoryFlags & BattleActionCategoryFlags.Heal) == BattleActionCategoryFlags.Heal) healTargetsScores = ScoreTargets_Damaging(flags, user, action, potentialTargets, true);
            else healTargetsScores = populateZeroedScoreArray();
            if ((action.categoryFlags & BattleActionCategoryFlags.Buff) == BattleActionCategoryFlags.Buff) buffTargetsScores = ScoreTargets_BuffDebuff(flags, user, action, potentialTargets);
            else buffTargetsScores = populateZeroedScoreArray();
            if ((action.categoryFlags & BattleActionCategoryFlags.Debuff) == BattleActionCategoryFlags.Debuff) debuffTargetsScores = ScoreTargets_BuffDebuff(flags, user, action, potentialTargets, true);
            else debuffTargetsScores = populateZeroedScoreArray();
            float[] finalScores = new float[potentialTargets.Length];
            for (int i = 0; i < finalScores.Length; i++) finalScores[i] = attackTargetsScores[i] + healTargetsScores[i] + buffTargetsScores[i] + debuffTargetsScores[i];
            return finalScores;
        }

        /// <summary>
        /// Score potential targets on the criterion of a damaging attack.
        /// If asHeal = true, we score targets on the criteria of a heal instead, since that's _broadly_ similar.
        /// </summary>
        private static float[] ScoreTargets_Damaging (BattlerAIFlags flags, Battler user, BattleAction action, Battler[] potentialTargets, bool asHeal = false)
        {
            const float killConfirmBonus = 1f;
            BattleActionCategoryFlags category;
            if (asHeal) category = BattleActionCategoryFlags.Heal;
            else category = BattleActionCategoryFlags.Attack;
            float[] scores = new float[potentialTargets.Length]; // dmgScore is typically just damage / maxHP, but if you want to eg. prioritize specific units, apply score penalties if the attack is likely to miss, etc., you apply those to dmgScores
            for (int i = 0; i < potentialTargets.Length; i++)
            {
                float score = 0;
                int countedSubactions = 0;
                for (int s = 0; s < action.subactions.Length; s++)
                {
                    if ((action.subactions[s].categoryFlags & category) == category)
                    {
                        countedSubactions++;
                        // Float imprecision is fine because scoring doesn't need to have _exact_ integral damage values, it just needs to indicate the general power of attacks relative to each other
                        float thisSubActionDmg = potentialTargets[i].CalcDamageAgainstMe(user, action.subactions[s], false, (flags & BattlerAIFlags.WeaknessAware) == BattlerAIFlags.WeaknessAware, (flags & BattlerAIFlags.ResistanceAware) == BattlerAIFlags.ResistanceAware);
                        if ((flags & BattlerAIFlags.EvadeAware) == BattlerAIFlags.EvadeAware && action.subactions[s].evadeStat != LogicalStatType.None)
                        {
                            thisSubActionDmg = Mathf.FloorToInt(BattleUtility.GetModifiedAccuracyFor(action.subactions[s], user, potentialTargets[i]));
                        }
                        score += thisSubActionDmg;
                    }
                }
                score = Mathf.FloorToInt(score / countedSubactions);
                // Damage calculation can overheal/overkill things, but we don't want to score based on damage that doesn't "matter"
                if (asHeal && score < potentialTargets[i].currentHP - potentialTargets[i].stats.maxHP) score = potentialTargets[i].currentHP - potentialTargets[i].stats.maxHP;
                else if (score > potentialTargets[i].currentHP) score = potentialTargets[i].currentHP;
                if (potentialTargets[i].currentHP <= score) score += killConfirmBonus; // kill confirm bonus never applies when figuring heals because raw damage is always < 0
                TargetSideFlags relativeSideToTarget = BattleUtility.GetRelativeSidesFor(user.side, potentialTargets[i].side);
                if (relativeSideToTarget == TargetSideFlags.Neutral) score /= 2;
                // If we're using an attack, flipping positive values here gives us negative scores (very low) against allied units. 
                // If we're using a heal, the exact same behavior gives us positive scores for healing friends and negative scores for healing foes.
                else if (relativeSideToTarget == TargetSideFlags.MyFriends || relativeSideToTarget == TargetSideFlags.MySide) score *= -1;
                scores[i] = (score / potentialTargets[i].stats.maxHP);

            }
            return scores;
        }

        /// <summary>
        /// Scores potential targets on the criteria of a buff or debuff.
        /// </summary>
        private static float[] ScoreTargets_BuffDebuff (BattlerAIFlags flags, Battler user, BattleAction action, Battler[] potentialTargets, bool asDebuff = false)
        {
            BattleActionCategoryFlags category;
            if (asDebuff) category = BattleActionCategoryFlags.Debuff;
            else category = BattleActionCategoryFlags.Buff;
            float[] scores = new float[potentialTargets.Length];
            for (int i = 0; i < potentialTargets.Length; i++)
            {
                float score = 0;
                float accuracyMod = 0;
                int countedSubactions = 0;
                for (int s = 0; s < action.subactions.Length; s++)
                {
                    if ((action.subactions[s].categoryFlags & category) == category)
                    {
                        float subactionAccMod = 0;
                        int f;
                        for (f = 0; f < action.subactions[s].effectPackages.Length; f++)
                        {
                            float fxAccMod = 1;
                            score += action.subactions[s].effectPackages[f].baseAIScoreValue;
                            if ((flags & BattlerAIFlags.EvadeAware) == BattlerAIFlags.EvadeAware)
                            {
                                if (action.subactions[s].effectPackages[f].evadeStat != LogicalStatType.None) fxAccMod = BattleUtility.GetModifiedAccuracyFor(action.subactions[s].effectPackages[f], user, potentialTargets[i]);
                                if (!action.subactions[s].effectPackages[f].applyEvenIfSubactionMisses && action.subactions[s].evadeStat != LogicalStatType.None) fxAccMod *= BattleUtility.GetModifiedAccuracyFor(action.subactions[s], user, potentialTargets[i]);
                            }
                            // Eventually I need to add status resistances, at which point the difficulty or ease of landing that status on that target needs to be factored into the accuracy mod.
                            subactionAccMod += fxAccMod;
                        }
                        subactionAccMod /= f;
                        accuracyMod += subactionAccMod;
                        countedSubactions++;
                    }
                }
                accuracyMod /= countedSubactions;
                score *= accuracyMod;
                TargetSideFlags relativeSideToTarget = BattleUtility.GetRelativeSidesFor(user.side, potentialTargets[i].side);
                if (relativeSideToTarget == TargetSideFlags.Neutral) score /= 2;
                // buffs have negative base values, debuffs have positive ones, they're literally the exact same besides
                else if (relativeSideToTarget == TargetSideFlags.MyFriends || relativeSideToTarget == TargetSideFlags.MySide) score *= -1;
                scores[i] = score;
            }
            return scores;
        }

        /// <summary>
        /// Returns a 2D array of battlers.
        /// The first array contains all valid primary targets for the specified battler/action combination.
        /// The second array contains all valid secondary targets for the specified battler/action combination.
        /// If there are no valid targets for either targeting type, the array will be empty.
        /// (TO DO: once movement is a thing this gets more complex, because a target can be "legal only if you move first."
        /// It'll probably be necessary to create a struct that associates a target with the required move vector to target it?
        /// Luckily it's just a maneuvering map vs. having terrain and shit, so there's not any real _pathfinding_ to speak of.)
        /// </summary>
        public static Battler[][] FindLegalTargetsForAction (Battler b, BattleAction battleAction)
        {
            Battler[][] output = new Battler[2][];
            Func<TargetSideFlags, Battler[]> populateList = (targetSideFlags) =>
            {
                Battler[] t0 = new Battler[0];
                Battler[] t1 = new Battler[0];
                Battler[] t2 = new Battler[0];
                Battler[] t3 = new Battler[0];
                if (targetSideFlags == TargetSideFlags.None) Util.Crash(new Exception("Can't find legal targets for action " + battleAction.actionID.ToString() + " because it doesn't _have_ legal targets. This is either a special case that shouldn't go through normal target acquisition, something that just shouldn't _be_ executed, or completely broken."));
                if ((targetSideFlags & TargetSideFlags.MySide) == TargetSideFlags.MySide)
                {
                    t0 = BattleOverseer.currentBattle.GetBattlersSameSideAs(b.side);
                }
                if ((targetSideFlags & TargetSideFlags.MyFriends) == TargetSideFlags.MyFriends)
                {
                    t1 = BattleOverseer.currentBattle.GetBattlersAlliedTo(b.side, true);
                }
                if ((targetSideFlags & TargetSideFlags.MyEnemies) == TargetSideFlags.MyEnemies)
                {
                    t2 = BattleOverseer.currentBattle.GetBattlersEnemiesTo(b.side);
                }
                if ((targetSideFlags & TargetSideFlags.Neutral) == TargetSideFlags.Neutral)
                {
                    t3 = BattleOverseer.currentBattle.GetBattlersEnemiesTo(b.side);
                }
                Battler[] targets = new Battler[t0.Length + t1.Length + t2.Length + t3.Length];
                t0.CopyTo(targets, 0);
                t1.CopyTo(targets, t0.Length);
                t2.CopyTo(targets, t0.Length + t1.Length);
                t3.CopyTo(targets, t0.Length + t1.Length + t2.Length);
                int validLen = targets.Length;
                bool[] validity = new bool[targets.Length];
                for (int i = 0; i < targets.Length; i++)
                {
                    validity[i] = targets[i].IsValidTargetFor(b, battleAction);
                    if (!validity[i]) validLen--;
                }
                Battler[] validTargets = new Battler[validLen];
                int vti = 0;
                for (int i = 0; i < targets.Length; i++)
                {
                    if (validity[i])
                    {
                        validTargets[vti] = targets[i];
                        vti++;
                    }
                }
                return validTargets;
            };      
            output[0] = populateList(battleAction.targetingSideFlags);
            if (battleAction.alternateTargetSideFlags != TargetSideFlags.None) output[1] = populateList(battleAction.alternateTargetSideFlags);
            else output[1] = new Battler[0];
            return output;
        }    
    }
}