using System;
using System.Collections.Generic;

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
        /// Starts taking a turn.
        /// </summary>
        public void StartTurn ()
        {
            BattleStage.instance.StartOfTurn();
            currentTurnBattler = battlersReadyToTakeTurns[0];
            battlersReadyToTakeTurns.Remove(currentTurnBattler);
            BattleOverseer.currentBattle.ChangeState(BattleData.State.WaitingForInput);
            Action callback = () =>
            {
                battle.actionExecutionSubsystem.BeginAction(currentTurnBattler.turnActions.action, currentTurnBattler, currentTurnBattler.turnActions.targets, currentTurnBattler.turnActions.alternateTargets, EndTurn);
            };
            currentTurnBattler.GetAction(callback);
        }

        /// <summary>
        /// Finishes taking a turn.
        /// </summary>
        public void EndTurn ()
        {
            currentTurnBattler = null;
            battle.ChangeState(BattleData.State.BetweenTurns);
            battle.DeriveNormalizedSpeed();
            BattleStage.instance.SetBattlersIdle();
            elapsedTurns++;
        }
    }
}