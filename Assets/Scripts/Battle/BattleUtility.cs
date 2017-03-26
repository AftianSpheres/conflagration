namespace CnfBattleSys
{
    public static class BattleUtility
    {
        public const int numberOfActionEntries = 5;
        public const int numberOfStanceEntries = 1;

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
    }
}