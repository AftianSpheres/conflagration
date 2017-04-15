using System;
using System.Collections.Generic;
using MovementEffects;

namespace CnfBattleSys.AI
{
    /// <summary>
    /// "Fake" AI module used for player-controlled units.
    /// Dispatches commands to the player-facing UI and waits for results.
    /// TO-DO: This has way too much global state and needs to be re-architected as something sane.
    /// </summary>
    public static class AIModule_PlayerSide_ManualControl
    {
        /// <summary>
        /// Flags for player side psuedo-AI state info.
        /// </summary>
        [Flags]
        private enum StateFlags
        {
            None = 0,
            Online = 1 << 0,
            WaitingForStance = 1 << 1,
            WaitingForAction = 1 << 2,
            WaitingForMainTargets = 1 << 3,
            WaitingForSecondaryTargets = 1 << 4
        }

        // Internal state

        private static BattleAction selectedAction;
        private static BattleStance selectedStance;
        private static Battler[] selectedPrimaryTargets;
        private static Battler[] selectedSecondaryTargets;
        private static BattlerAIMessageFlags messageFlags;
        private static StateFlags stateFlags;
        private static bool stillWaiting { get
            {
                return (stateFlags & StateFlags.WaitingForAction) == StateFlags.WaitingForAction || (stateFlags & StateFlags.WaitingForStance) == StateFlags.WaitingForStance ||
                   (stateFlags & StateFlags.WaitingForMainTargets) == StateFlags.WaitingForMainTargets || (stateFlags & StateFlags.WaitingForSecondaryTargets) == StateFlags.WaitingForSecondaryTargets;
            }}

        // This is the data the psuedo-AI exposes for use by the battle UI.

        public static Battler waitingBattler { get; private set; }
        public static BattleStance[] waitingStanceSet { get; private set; }
        public static BattleAction[] waitingActionSet_ForStance { get; private set; }
        public static BattleAction[] waitingActionSet_ForMetaStance { get; private set; }
        public static Battler[][] waitingMainPrimaryTargets_ForStanceActions { get; private set; } // These are all specifically just the "main/central" target even in case of AOE.
        public static Battler[][] waitingMainPrimaryTargets_ForMetaStanceActions { get; private set; }
        public static Battler[][] waitingMainSecondaryTargets_ForStanceActions { get; private set; }
        public static Battler[][] waitingMainSecondaryTargets_ForMetaStanceActions { get; private set; }


        /// <summary>
        /// Resets player side psuedo-AI module state.
        /// As a general rule, AI modules "should" be stateless, because they make their decisions immediately...
        /// ...but the player-side psuedo-AI doesn't quite fit cleanly into that design, which means it has a lot
        /// of global state variables used to track the options available to the player, what they've already decided, etc.
        /// This resets all of those, and you should _always_ call it as part of any public methods that "start"
        /// this module.
        /// </summary>
        private static void Cleanup ()
        {
            stateFlags = StateFlags.None;
            messageFlags = BattlerAIMessageFlags.None;
            selectedAction = null;
            selectedStance = null;
            selectedPrimaryTargets = null;
            selectedSecondaryTargets = null;
            waitingBattler = null;
            waitingStanceSet = null;
            waitingActionSet_ForStance = null;
            waitingActionSet_ForMetaStance = null;
            waitingMainPrimaryTargets_ForStanceActions = null;
            waitingMainPrimaryTargets_ForMetaStanceActions = null;
            waitingMainSecondaryTargets_ForStanceActions = null;
            waitingMainSecondaryTargets_ForMetaStanceActions = null;
        }

        /// <summary>
        /// Sets up the public fields that the UI can use to show the player their options
        /// and, based on their input, generate the data it passes into this module through
        /// InputAction/InputTargets/InputStance calls.
        /// </summary>
        private static void EstablishPlayerPresentedData (Battler b)
        {
            waitingBattler = b;
            waitingMainPrimaryTargets_ForStanceActions = new Battler[b.currentStance.actionSet.Length][];
            waitingMainSecondaryTargets_ForStanceActions = new Battler[b.currentStance.actionSet.Length][];
            waitingActionSet_ForStance = b.currentStance.actionSet;
            for (int i = 0; i < b.currentStance.actionSet.Length; i++)
            {
                Battler[][] thisActionLegalTargets = BattlerAISystem.FindLegalTargetsForAction(b, b.currentStance.actionSet[i]);
                waitingMainPrimaryTargets_ForStanceActions[i] = thisActionLegalTargets[0];
                waitingMainSecondaryTargets_ForStanceActions[i] = thisActionLegalTargets[1];
            }
            waitingMainPrimaryTargets_ForMetaStanceActions = new Battler[b.currentStance.actionSet.Length][];
            waitingMainSecondaryTargets_ForMetaStanceActions = new Battler[b.currentStance.actionSet.Length][];
            waitingActionSet_ForMetaStance = b.metaStance.actionSet;
            for (int i = 0; i < b.metaStance.actionSet.Length; i++)
            {
                Battler[][] thisActionLegalTargets = BattlerAISystem.FindLegalTargetsForAction(b, b.metaStance.actionSet[i]);
                waitingMainPrimaryTargets_ForMetaStanceActions[i] = thisActionLegalTargets[0];
                waitingMainSecondaryTargets_ForMetaStanceActions[i] = thisActionLegalTargets[1];
            }
            waitingStanceSet = b.stances;
        }

        /// <summary>
        /// Starts the psuedo-AI module for the specified battler, and waits until we have 
        /// all the info needed to build the TurnActions struct that we finally give
        /// to it via ReceiveAThought.
        /// </summary>
        public static void GetTurnActionsFromPlayer (Battler b, bool changeStances)
        {
            if ((stateFlags & StateFlags.Online) == StateFlags.Online) throw new Exception("Tried to get input for a second player-controlled unit while still waiting on the first!");
            Cleanup();
            stateFlags |= StateFlags.Online;
            EstablishPlayerPresentedData(b);
            if (changeStances) PlayerSelectsStanceFor(b);
            else
            {
                PlayerSelectsActionFor(b);
            }
            Timing.RunCoroutine(_GenerateTurnActionsAsSoonAsPosible(b));
        }

        /// <summary>
        /// Coroutine: waits until we've gotten everything we need to build the TurnActions struct, then does that.
        /// </summary>
        private static IEnumerator<float> _GenerateTurnActionsAsSoonAsPosible(Battler b)
        {
            while (stillWaiting)
            {
                yield return 0;
            }
            bool stanceChanged;
            if (selectedStance == null)
            {
                stanceChanged = false;
                selectedStance = b.currentStance;
            }
            else // Build the "fake" TurnActions struct that changes battler stance and extends the turn
            {
                stanceChanged = true;
                selectedAction = ActionDatabase.SpecialActions.noneBattleAction;
                selectedPrimaryTargets = new Battler[0];
                selectedSecondaryTargets = new Battler[0];
                messageFlags |= BattlerAIMessageFlags.ExtendTurn;
            }
            Battler.TurnActions turnActions = new Battler.TurnActions(stanceChanged, 0.0f, selectedPrimaryTargets, selectedSecondaryTargets, selectedAction, selectedStance);
            b.ReceiveAThought(turnActions, messageFlags);
            stateFlags ^= StateFlags.Online;
        }

        /// <summary>
        /// Sets the pseudo-AI module up to receive an action selection from the player.
        /// </summary>
        private static void PlayerSelectsActionFor (Battler b)
        {
            Timing.RunCoroutine(_WaitForActionSelection_ThenGetTargets(b));
        }

        /// <summary>
        /// Coroutine: waits until the player submits an action, then gets targets
        /// for that action.
        /// </summary>
        private static IEnumerator<float> _WaitForActionSelection_ThenGetTargets(Battler b)
        {
            stateFlags |= StateFlags.WaitingForAction;
            while (selectedAction == null) yield return 0;
            stateFlags ^= StateFlags.WaitingForAction;
            PlayerSelectsTargetsFor(b, selectedAction, false);
            PlayerSelectsTargetsFor(b, selectedAction, true);
        }

        /// <summary>
        /// Sets the psuedo-AI module up to receive a stance selection from the player.
        /// We want to commit player-side stance changes before the player gets
        /// to choose an action to use, so once we get a stance, we
        /// build a TurnActions that doesn't do anything _but_
        /// change into the desired stance and extend our turn.
        /// </summary>
        private static void PlayerSelectsStanceFor(Battler b)
        {
            Timing.RunCoroutine(_WaitForStanceSelection());
        }

        /// <summary>
        /// Coroutine: waits until the player submits a stance.
        /// </summary>
        private static IEnumerator<float> _WaitForStanceSelection()
        {
            stateFlags |= StateFlags.WaitingForStance;
            while (selectedStance == null) yield return 0;
            stateFlags ^= StateFlags.WaitingForStance;
        }

        /// <summary>
        /// Sets the psuedo-AI module up to receive a set of targets from the player.
        /// </summary>
        private static void PlayerSelectsTargetsFor (Battler b, BattleAction a, bool asSecondary)
        {
            Timing.RunCoroutine(_WaitForTargetSelection(asSecondary));
        }

        /// <summary>
        /// Coroutine: waits until the player submits target set.
        /// </summary>
        private static IEnumerator<float> _WaitForTargetSelection (bool asSecondary)
        {
            StateFlags f;
            if (asSecondary) f = StateFlags.WaitingForSecondaryTargets;
            else f = StateFlags.WaitingForMainTargets;
            stateFlags |= f;
            if (asSecondary) while (selectedSecondaryTargets == null) yield return 0;
            else while (selectedPrimaryTargets == null) yield return 0;
            stateFlags ^= f;
        }

        /// <summary>
        /// Passes an action selection into the player-side psuedo-AI.
        /// </summary>
        public static void InputAction (BattleAction action)
        {
            if (selectedAction != null) throw new Exception("Input another action after obtaining action input!");
            selectedAction = action;
        }

        /// <summary>
        /// Passes a stance selection into the player-side psuedo-AI.
        /// </summary>
        public static void InputStance (BattleStance stance)
        {
            if (selectedStance != null) throw new Exception("Input another stance after obtaining stance input!");
            selectedStance = stance;
        }

        /// <summary>
        /// Passes a target set into the player-side psuedo-AI.
        /// </summary>
        public static void InputTargets (Battler[] targets, bool asSecondary)
        {
            if (asSecondary)
            {
                if (selectedSecondaryTargets != null) throw new Exception("Input another secondary target set after obtaining secondary targets!");
                selectedSecondaryTargets = targets;
            }
            else
            {
                if (selectedPrimaryTargets != null) throw new Exception("Input another primary target set after obtaining primary targets!");
                selectedPrimaryTargets = targets;
            }
        }
    }
}