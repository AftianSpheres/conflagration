using System.Collections.Generic;
using MovementEffects;

namespace CnfBattleSys
{
    /// <summary>
    /// Turn progression:
    /// - If battlersReadyToTakeTurns.Count > 0, we can take a turn!
    /// - Get the first battler that's ready to go fromt he list
    /// - Call into that Battler's GetAction() to get targets/secondaryTargets/action
    /// - Start executing the action
    /// - When the action has finished executing, turn is over
    /// </summary>
    public class TurnManagementSubsystem
    {
        private BattleData battle;
        /// <summary>
        /// Battler that's currently taking a turn.
        /// </summary>
        public Battler currentTurnBattler { get; private set; }
        /// <summary>
        /// Battlers to give turns when that next becomes possible. Normally there should only actually be one Battler here at a time... but ties are a thing,
        /// and having multiple members in this list gives us an easy way to say "these guys need a tiebreaker."
        /// </summary>
        public List<Battler> battlersReadyToTakeTurns { get; private set; }
        public int elapsedTurns { get; private set; }

        /// <summary>
        /// First-run setup for turn management subsystem.
        /// </summary>
        public TurnManagementSubsystem (BattleData _battle)
        {
            battle = _battle;
            battlersReadyToTakeTurns = new List<Battler>();
            elapsedTurns = 0;
        }

        /// <summary>
        /// Cleanup for turn management subsystem.
        /// </summary>
        public void Cleanup()
        {
            battlersReadyToTakeTurns.Clear();
        }

        /// <summary>
        /// So long as something's taking a turn right now, gives that battler a second turn immediately after this one.
        /// </summary>
        public void ExtendCurrentTurn()
        {
            if (currentTurnBattler == null) Util.Crash(new System.Exception("Can't extend current turn because there _isn't_ a current turn."));
            battlersReadyToTakeTurns.Insert(0, currentTurnBattler);
        }

        /// <summary>
        /// Checks to see if a) we have battlers waiting on their turn and b) we aren't currently taking a turn.
        /// </summary>
        /// <returns></returns>
        public bool ReadyToTakeATurn()
        {
            return (battlersReadyToTakeTurns.Count > 0 && currentTurnBattler == null);
        }

        /// <summary>
        /// Battler b needs to take a turn as soon as it can be allowed to do so.
        /// </summary>
        public void RequestTurn(Battler b)
        {
            battlersReadyToTakeTurns.Add(b);
        }

        /// <summary>
        /// Coroutine: calls b.GetAction, then sit on our ass until b gives us the action we want.
        /// (This lets us pause the battle simulation indefinitely for eg. player input. Or _really_
        /// messy AI, hypothetically, I guess.)
        /// </summary>
        private IEnumerator<float> _WaitUntilBattlerReadyToAct(Battler b)
        {
            BattleOverseer.currentBattle.ChangeState(BattleData.State.WaitingForInput);
            b.GetAction();
            while (b.turnActions.action == ActionDatabase.SpecialActions.defaultBattleAction) yield return 0; // wait until b decides what to do
            battle.actionExecutionSubsystem.BeginProcessingAction(b.turnActions.action, b, b.turnActions.targets, b.turnActions.alternateTargets);
            Timing.RunCoroutine(_EndTurnOnceActionExecutionCompleted());
        }

        /// <summary>
        /// Coroutine: waits until action execution completes, then ends turn.
        /// </summary>
        private IEnumerator<float> _EndTurnOnceActionExecutionCompleted()
        {
            while (battle.actionExecutionSubsystem.isRunning) yield return 0;
            EndTurn();
        }

        /// <summary>
        /// Starts taking a turn.
        /// </summary>
        public void StartTurn()
        {
            BattleStage.instance.StartOfTurn();
            currentTurnBattler = battlersReadyToTakeTurns[0];
            battlersReadyToTakeTurns.Remove(currentTurnBattler);
            Timing.RunCoroutine(_WaitUntilBattlerReadyToAct(currentTurnBattler));
        }

        /// <summary>
        /// Finishes taking a turn.
        /// </summary>
        public void EndTurn()
        {
            battle.actionExecutionSubsystem.FireRemainingSubactions();
            currentTurnBattler = null;
            battle.ChangeState(BattleData.State.BetweenTurns);
            battle.DeriveNormalizedSpeed();
            elapsedTurns++;
        }
    }
}