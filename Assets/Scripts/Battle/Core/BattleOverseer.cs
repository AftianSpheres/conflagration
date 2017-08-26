namespace CnfBattleSys
{
    /// <summary>
    /// Static class that forms the "brains" of the battle system.
    /// Handles turn order, command execution, inter-battler communication,
    /// win/loss, etc.
    /// </summary>
    public static class BattleOverseer
    {
        public const float battleTickLength = 1 / 60;
        public const float fieldRadius = 50;
        public static BattleData currentBattle { get; private set; }
        public static bool online { get { return currentBattle != null && allPuppetsExist; } }
        private static bool allPuppetsExist = false;

        /// <summary>
        /// Sets up battle data for the given formation.
        /// This doesn't start battle execution!
        /// Always call PrepareBattle, do whatever battle scene setup you need to do, and
        /// _then_ call StartBattle.
        /// </summary>
        public static void PrepareBattle (BattleFormation formation)
        {
            currentBattle = new BattleData(formation);
        }

        /// <summary>
        /// Starts battle processing.
        /// </summary>
        public static void StartBattle ()
        {
            BattleStage.instance.StartOfBattle();
            currentBattle.BetweenTurns();
        }

        /// <summary>
        /// Ends the current battle.
        /// </summary>
        public static void EndBattle ()
        {
            currentBattle = null;
        }
    }
}