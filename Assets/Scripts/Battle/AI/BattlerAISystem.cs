using System;
using System.Collections.Generic;
using UnityEngine;
using CnfBattleSys.AI;


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
            public readonly Battler[] primaryTargets;
            public readonly Battler[] secondaryTargets;
            public readonly float[] primaryScores;
            public readonly float[] secondaryScores;
            public readonly float totalScore;

            public ScoredActionTargets (Battler[] _primaryTargets, Battler[] _secondaryTargets, float[] _primaryScores, float[] _secondaryScores)
            {
                primaryTargets = _primaryTargets;
                secondaryTargets = _secondaryTargets;
                primaryScores = _primaryScores;
                secondaryScores = _secondaryScores;
                float ts = 0;
                int q = 0;
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
        }

        private static List<Battler> battlersListBuffer;
        private static List<Battler> alsoBattlersListBuffer; // I am probably way too worried about unnecessary allocations to be doing things this way

        /// <summary>
        /// First-run setup for BattlerAISystem.
        /// </summary>
        public static void FirstRunSetup ()
        {
            battlersListBuffer = new List<Battler>();
            alsoBattlersListBuffer = new List<Battler>();
        }

        /// <summary>
        /// Starts the AI on deciding what the unit should do.
        /// Thia is basically just an intermediary that goes between the Battler and
        /// the individual AI modules, so as to keep the (already pretty gnarly) Battler
        /// object from having to deal with the additional headache of managing this
        /// huge-ass jump table. (Well it's not huge _yet_ but, architecturally: this
        /// table is fat af.)
        /// </summary>
        public static void StartThinking (Battler b, bool changeStances)
        {
            Battler.TurnActions turnActions = Battler.defaultTurnActions;
            BattlerAIMessageFlags messageFlags = BattlerAIMessageFlags.None;
            bool outputIsDelayed = false; // if the module we're running can't return output immediately, either because it requires player input or because we're doing something inane and unsafe with the AI itself
            switch (b.aiType)
            {
                case BattlerAIType.None:
                    break; // if the battler says it doesn't have any AI, we dutifully decline to provide the battler with AI
                case BattlerAIType.PlayerSide_ManualControl:
                    outputIsDelayed = true;
                    throw new NotImplementedException();
                case BattlerAIType.TestAI:
                    AIModule_TestAI.DecideTurnActions_AndStanceIfApplicable(b, changeStances, out turnActions, out messageFlags);
                    break;
                default:
                    throw new Exception("No entry in AI function jump table for AI type of: " + b.aiType.ToString());
            }
            if (outputIsDelayed) throw new NotImplementedException(); // this should start a coroutine that waits for the AI or player "AI" to finish, then calls b.ReceiveAThought whenever that's done
            else b.ReceiveAThought(turnActions, messageFlags);
        }

        /// <summary>
        /// Gets optimum primary and secondary targets (and total action score) for the given user, action, and set of potential targets.
        /// </summary>
        private static ScoredActionTargets GetOptimumTargets (BattlerAIFlags flags, Battler user, BattleAction action, Battler[][] jointPotentialTargets)
        {
            float[] scores = new float[0];
            Func<ActionTargetType, Battler[], Battler[]> forSingle = (targetingType, potentialTargets) =>
            {
                if (potentialTargets.Length < 1) throw new Exception("Can't pick optimum target unless you actually provide some targets.");
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
                if (potentialTargets.Length < 1) throw new Exception("Can't pick optimum target unless you actually provide some targets.");
                scores = ScoreTargets(flags, user, action, potentialTargets);
                Battler[][] subtargetsForAOE = new Battler[potentialTargets.Length][];
                for (int t = 0; t < potentialTargets.Length; t++)
                {
                    subtargetsForAOE[t] = BattleOverseer.GetBattlersWithinAOERangeOf(user, potentialTargets[t], targetingType, action.baseAOERadius, potentialTargets);
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
                battlersListBuffer.Clear();
                battlersListBuffer.Add(potentialTargets[optimumTargetIndex]);
                for (int i = 0; i < subtargetsForAOE[optimumTargetIndex].Length; i++)
                {
                    battlersListBuffer.Add(subtargetsForAOE[optimumTargetIndex][i]);
                }
                return battlersListBuffer.ToArray();
            };
            Func<ActionTargetType, Battler[], Battler[]> forAll = (targetingType, potentialTargets) =>
            {
                return potentialTargets; // if we're acting on all potential targets, we don't need to do any processing, we just immediately spit the potentialTargets back
            };
            Func<ActionTargetType, Func<ActionTargetType, Battler[], Battler[]>> setTargetAcquisitionFunc = (targetingType) =>
            {
                switch (targetingType)
                {
                    case ActionTargetType.SingleTarget:
                    case ActionTargetType.Self:
                        return forSingle;
                    case ActionTargetType.AllTargetsInRange:
                        return forAll;
                    case ActionTargetType.AllTargetsAlongLinearCorridor:
                    case ActionTargetType.CircularAOE:
                        return forAOE;
                    default:
                        throw new Exception("Tried to acquire targets for invalid targeting type: " + targetingType);
                }
            };
            Func<ActionTargetType, Battler[], Battler[]> primaryTargetsAcquisition = setTargetAcquisitionFunc(action.targetingType);
            Func<ActionTargetType, Battler[], Battler[]> secondaryTargetsAcquisition = setTargetAcquisitionFunc(action.alternateTargetType);
            Battler[] primaryTargets = primaryTargetsAcquisition(action.targetingType, jointPotentialTargets[0]);
            float[] primaryScores = scores;
            Battler[] secondaryTarget = secondaryTargetsAcquisition(action.alternateTargetType, jointPotentialTargets[1]);
            float[] secondaryScores = scores;
            return new ScoredActionTargets(primaryTargets, secondaryTarget, primaryScores, secondaryScores);
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
                for (int s = 0; s < action.Subactions.Length; s++)
                {
                    if ((action.Subactions[s].categoryFlags & category) == category)
                    {
                        // Float imprecision is fine because scoring doesn't need to have _exact_ integral damage values, it just needs to indicate the general power of attacks relative to each other
                        float thisSubActionDmg = potentialTargets[i].CalcDamageAgainstMe(user, action.Subactions[s], false, (flags & BattlerAIFlags.WeaknessAware) == BattlerAIFlags.WeaknessAware, (flags & BattlerAIFlags.ResistanceAware) == BattlerAIFlags.ResistanceAware);
                        if ((flags & BattlerAIFlags.EvadeAware) == BattlerAIFlags.EvadeAware && action.Subactions[s].evadeStat != LogicalStatType.None)
                        {
                            thisSubActionDmg = Mathf.FloorToInt(BattleUtility.GetModifiedAccuracyFor(action.Subactions[s], user, potentialTargets[i]));
                        }
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
                for (int s = 0; s < action.Subactions.Length; s++)
                {    
                    if ((action.Subactions[s].categoryFlags & category) == category)
                    {
                        float subactionAccMod = 0;
                        int f;
                        for (f = 0; f < action.Subactions[s].fx.Length; f++)
                        {
                            float fxAccMod = 1;
                            score += action.Subactions[s].fx[f].baseAIScoreValue;
                            if ((flags & BattlerAIFlags.EvadeAware) == BattlerAIFlags.EvadeAware)
                            {
                                if (action.Subactions[s].fx[f].fxEvadeStat != LogicalStatType.None) fxAccMod = BattleUtility.GetModifiedAccuracyFor(action.Subactions[s].fx[f], user, potentialTargets[i]);
                                if (!action.Subactions[s].fx[f].applyEvenIfSubactionMisses && action.Subactions[s].evadeStat != LogicalStatType.None) fxAccMod *= BattleUtility.GetModifiedAccuracyFor(action.Subactions[s], user, potentialTargets[i]);
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
        /// </summary>
        public static Battler[][] FindLegalTargetsForAction (Battler b, BattleAction battleAction)
        {
            Battler[][] output = new Battler[2][];
            Action<TargetSideFlags> populateList = (targetSideFlags) =>
            {
                battlersListBuffer.Clear();
                alsoBattlersListBuffer.Clear();
                if (targetSideFlags == TargetSideFlags.None) throw new Exception("Can't find legal targets for action " + battleAction.actionID.ToString() + " because it doesn't _have_ legal targets. This is either a special case that shouldn't go through normal target acquisition, something that just shouldn't _be_ executed, or completely broken.");
                if ((targetSideFlags & TargetSideFlags.MySide) == TargetSideFlags.MySide)
                {
                    BattleOverseer.GetBattlersSameSideAs(b.side, ref battlersListBuffer);
                }
                if ((targetSideFlags & TargetSideFlags.MyFriends) == TargetSideFlags.MyFriends)
                {
                    BattleOverseer.GetBattlersAlliedTo_Strict(b.side, ref battlersListBuffer);
                }
                if ((targetSideFlags & TargetSideFlags.MyEnemies) == TargetSideFlags.MyEnemies)
                {
                    BattleOverseer.GetBattlersEnemiesTo(b.side, ref battlersListBuffer);
                }
                if ((targetSideFlags & TargetSideFlags.Neutral) == TargetSideFlags.Neutral)
                {
                    BattleOverseer.GetBattlersEnemiesTo(b.side, ref battlersListBuffer);
                }
                for (int i = 0; i < battlersListBuffer.Count; i++) if (battlersListBuffer[i].IsValidTargetFor(b, battleAction)) alsoBattlersListBuffer.Add(battlersListBuffer[i]);
            };
            populateList(battleAction.targetingSideFlags);
            output[0] = alsoBattlersListBuffer.ToArray();
            if (battleAction.alternateTargetSideFlags != TargetSideFlags.None)
            {
                populateList(battleAction.alternateTargetSideFlags);
                output[1] = alsoBattlersListBuffer.ToArray();
            }
            else output[1] = new Battler[0];
            return output;
        }    
    }
}