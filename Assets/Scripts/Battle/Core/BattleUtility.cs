namespace CnfBattleSys
{
    public static class BattleUtility
    {
        /// <summary>
        /// "Yo, are these guys allies or enemies or what?"
        /// </summary>
        public static TargetSideFlags GetRelativeSidesFor (BattlerSideFlags checker, BattlerSideFlags target)
        {
            switch (checker)
            {
                case BattlerSideFlags.PlayerSide:
                    switch (target)
                    {
                        case BattlerSideFlags.PlayerSide:
                            return TargetSideFlags.MySide;
                        case BattlerSideFlags.GenericEnemySide:
                            return TargetSideFlags.MyEnemies;
                        case BattlerSideFlags.GenericAlliedSide:
                            return TargetSideFlags.MyFriends;
                        case BattlerSideFlags.GenericNeutralSide:
                            return TargetSideFlags.Neutral;
                        default:
                            return TargetSideFlags.None;
                    }
                case BattlerSideFlags.GenericEnemySide:
                    switch (target)
                    {
                        case BattlerSideFlags.PlayerSide:
                        case BattlerSideFlags.GenericAlliedSide:
                            return TargetSideFlags.MyEnemies;
                        case BattlerSideFlags.GenericEnemySide:
                            return TargetSideFlags.MySide;
                        case BattlerSideFlags.GenericNeutralSide:
                            return TargetSideFlags.Neutral;
                        default:
                            return TargetSideFlags.None;
                    }
                case BattlerSideFlags.GenericNeutralSide:
                    return TargetSideFlags.Neutral;
                default:
                    return TargetSideFlags.None;
            }
        }

        /// <summary>
        /// Gets modified accuracy value for a single FX package based on attacker/target combination.
        /// </summary>
        public static float GetModifiedAccuracyFor (BattleAction.Subaction.EffectPackage fxPackage, Battler attacker, Battler target)
        {
            int evadeStat = -1;
            if (fxPackage.evadeStat != LogicalStatType.None) evadeStat = target.GetLogicalStatValue(fxPackage.evadeStat);
            int hitStat = -1;
            if (fxPackage.hitStat != LogicalStatType.None) hitStat = attacker.GetLogicalStatValue(fxPackage.hitStat);
            float adjustedSuccessRate = fxPackage.baseSuccessRate;
            if (evadeStat != -1 && hitStat != -1) // this is contested, so let's work out the contest
            {
                adjustedSuccessRate *= (hitStat / (float)evadeStat);
            }
            // It should also be possible for uncontested hit/evade stats to provide hit/evade bonuses on FX packages, but that requires me to have some idea of what the numbers look like
            if (adjustedSuccessRate > 1) adjustedSuccessRate = 1;
            return adjustedSuccessRate;
        }

        /// <summary>
        /// Gets modified accuracy value for a single subaction based on attacker/target combination.
        /// </summary>
        public static float GetModifiedAccuracyFor (BattleAction.Subaction subaction, Battler attacker, Battler target)
        {
            int evadeStat = -1;
            int hitStat = -1;
            if (subaction.evadeStat != LogicalStatType.None) evadeStat = target.GetLogicalStatValue(subaction.evadeStat);
            if (subaction.hitStat != LogicalStatType.None) hitStat = attacker.GetLogicalStatValue(subaction.hitStat);
            float modifiedAccuracy = subaction.baseAccuracy;
            if (evadeStat != -1 && hitStat != -1)
            {
                modifiedAccuracy *= (hitStat / (float)evadeStat);
            }
            if (modifiedAccuracy > 1) modifiedAccuracy = 1; // can't have a >100% hit rate
            return modifiedAccuracy;
        }

        /// <summary>
        /// Makes sure BattlerSideFlags doesn't have more than one bit set,
        /// because unless we're _trying_ to use it for bitflaggy things
        /// that would be bad.
        /// </summary>
        public static bool IsSingleSide (BattlerSideFlags side)
        {
            switch (side)
            {
                case BattlerSideFlags.None:
                case BattlerSideFlags.PlayerSide:
                case BattlerSideFlags.GenericNeutralSide:
                case BattlerSideFlags.GenericEnemySide:
                case BattlerSideFlags.GenericAlliedSide:
                    return true;
            }
            return false; // and I'll never find my allies again, oh nooooooooooooooooooooo
        }
    }
}