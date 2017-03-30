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