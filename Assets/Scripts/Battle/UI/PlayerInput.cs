using UnityEngine;
using System;
using System.Collections.Generic;
using MovementEffects;
using CnfBattleSys;
using CnfBattleSys.AI;

/// <summary>
/// MonoBehaviour that listens to the player-side psuedo AI,
/// presents the player their choices, and gets their responses.
/// </summary>
public class PlayerInput : MonoBehaviour
{
    private enum LocalState
    {
        Offline,
        Online
    }
    private LocalState localState;
    private List<Battler> battlersListBuffer;
    private List<BattleAction> selectableBattleActions;
    private List<int> originalIndicesForBattleActions;
    private BattleAction selectedBattleAction;
    private int selectedBattleActionIndex;
    private Battler selectedMainPrimaryTarget;
    private Battler selectedMainSecondaryTarget;
    private bool selectedActionIsOnMetaStance;

    /// <summary>
    /// MonoBehaviour.Awake()
    /// </summary>
	void Awake ()
    {
        selectableBattleActions = new List<BattleAction>(10);
        originalIndicesForBattleActions = new List<int>(10);
        battlersListBuffer = new List<Battler>();
	}
	
    /// <summary>
    /// MonoBehaviour.Update()
    /// </summary>
	void Update ()
    {
	    switch (localState)
        {
            case LocalState.Offline:
                if (AIModule_PlayerSide_ManualControl.waitingBattler != null) Debug.Log("Your turn, battler " + AIModule_PlayerSide_ManualControl.waitingBattler.battlerType.ToString());
                if (AIModule_PlayerSide_ManualControl.WaitingForStanceInput())
                {
                    PresentStances();
                }
                else if (AIModule_PlayerSide_ManualControl.WaitingForActionInput())
                {
                    if (selectedBattleAction == null) PresentActions();
                    else if (!AIModule_PlayerSide_ManualControl.WaitingForMainTargets() && !AIModule_PlayerSide_ManualControl.WaitingForSecondaryTargets())
                    {
                        AIModule_PlayerSide_ManualControl.InputAction(selectedBattleAction);
                    }
                }
                break;
            case LocalState.Online:
                if (AIModule_PlayerSide_ManualControl.waitingBattler == null)
                {
                    Debug.Log("Done getting input");
                    localState = LocalState.Offline;
                    selectedBattleAction = null;
                    selectedMainPrimaryTarget = null;
                    selectedMainSecondaryTarget = null;
                }
                break;
            default:
                throw new Exception("Invalid player input system state: " + localState.ToString());
        }
	}

    /// <summary>
    /// Opens action select menu for the player.
    /// By which I mean binds up to 10 actions to numerical keys.
    /// </summary>
    private void PresentActions ()
    {
        localState = LocalState.Online;
        KeyCode[] alphaKeyCodes = { KeyCode.Alpha0, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9 };
        KeyCode[] numpadKeyCodes = { KeyCode.Keypad0, KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3, KeyCode.Keypad4, KeyCode.Keypad5, KeyCode.Keypad6, KeyCode.Keypad7, KeyCode.Keypad8, KeyCode.Keypad9 };
        selectableBattleActions.Clear();
        originalIndicesForBattleActions.Clear();
        for (int i = 0; i < AIModule_PlayerSide_ManualControl.waitingActionSet_ForStance.Length; i++)
        {
            if (selectableBattleActions.Count == 10) throw new Exception("You can't give the shitty console UI more than 10 actions, don't be a shithead");
            selectableBattleActions.Add(AIModule_PlayerSide_ManualControl.waitingActionSet_ForStance[i]);
            originalIndicesForBattleActions.Add(i);
        }
        int metaStanceSplitIndex = selectableBattleActions.Count;
        for (int i = 0; i < AIModule_PlayerSide_ManualControl.waitingActionSet_ForMetaStance.Length; i++)
        {
            if (selectableBattleActions.Count == 10) throw new Exception("You can't give the shitty console UI more than 10 actions, don't be a shithead");
            selectableBattleActions.Add(AIModule_PlayerSide_ManualControl.waitingActionSet_ForMetaStance[i]);
            originalIndicesForBattleActions.Add(i);
        }
        string s = "Select an action using an alphanumeric or number key: ";
        for (int i = 0; i < selectableBattleActions.Count; i++)
        {
            s += Environment.NewLine + i.ToString() + " " + selectableBattleActions[i].actionID.ToString();
        }
        Debug.Log(s);
        Timing.RunCoroutine(_WaitForPlayerToSelectActionFrom(alphaKeyCodes, numpadKeyCodes, metaStanceSplitIndex));
    }

    /// <summary>
    /// Coroutine: Waits for player to hit a number key, then assigns that action as the tentative action selection.
    /// </summary>
    private IEnumerator<float> _WaitForPlayerToSelectActionFrom (KeyCode[] alphaKeyCodes, KeyCode[] numpadKeyCodes, int metaStanceSplitIndex)
    {
        while (selectedBattleAction == null)
        {
            for (int i = 0; i < selectableBattleActions.Count; i++)
            {
                if (Input.GetKeyDown(alphaKeyCodes[i]) || Input.GetKeyDown(numpadKeyCodes[i]))
                {
                    Debug.Log("Selected action: " + selectableBattleActions[i].actionID);
                    selectedBattleAction = selectableBattleActions[i];
                    selectedBattleActionIndex = i;
                    selectedActionIsOnMetaStance = i >= metaStanceSplitIndex;
                    PresentTargets();
                    break;
                }
            }
            yield return 0;
        }
    }

    /// <summary>
    /// Opens stance select menu for the player.
    /// Well, no, actually we just dump some stuff to the log but shhhhh
    /// </summary>
    private void PresentStances ()
    {
        localState = LocalState.Online;
        BattleStance[] stances = AIModule_PlayerSide_ManualControl.waitingStanceSet;
        KeyCode[] keyCodes = { KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow };
        Debug.Log("Select a stance." + Environment.NewLine + "UP: " + stances[0].ToString() + Environment.NewLine + "DOWN: " + stances[1].ToString() +
            Environment.NewLine + "LEFT: " + stances[2].ToString() + Environment.NewLine + "RIGHT: " + stances[3].ToString());
        Timing.RunCoroutine(_WaitForPlayerToSelectStanceFrom(stances, keyCodes));
    }

    /// <summary>
    /// Coroutine: Given an index-matched pair of arrays of stances/keycodes, wait until keycode for any of these stances
    /// has been input.
    /// </summary>
    private IEnumerator<float> _WaitForPlayerToSelectStanceFrom (BattleStance[] stances, KeyCode[] keyCodes)
    {
        while (AIModule_PlayerSide_ManualControl.waitingStanceSet != null)
        {
            for (int i = 0; i < stances.Length; i++)
            {
                if (Input.GetKeyDown(keyCodes[i]))
                {
                    Debug.Log("Selected stance: " + stances[i]);
                    AIModule_PlayerSide_ManualControl.InputStance(stances[i]);
                    break;
                }
            }
            yield return 0;
        }
    }

    /// <summary>
    /// Opens target select whatsit for the player.
    /// (except not really)
    /// </summary>
    private void PresentTargets (bool asSecondary = false)
    {
        int index = originalIndicesForBattleActions[selectedBattleActionIndex];
        KeyCode[] alphaKeyCodes = { KeyCode.Alpha0, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9 };
        KeyCode[] numpadKeyCodes = { KeyCode.Keypad0, KeyCode.Keypad1, KeyCode.Keypad2, KeyCode.Keypad3, KeyCode.Keypad4, KeyCode.Keypad5, KeyCode.Keypad6, KeyCode.Keypad7, KeyCode.Keypad8, KeyCode.Keypad9 };
        Battler[] mainTargets;
        BattleOverseer.GetBattlersEnemiesTo(BattlerSideFlags.PlayerSide, ref battlersListBuffer);
        Battler[] enemies = battlersListBuffer.ToArray();
        if (enemies.Length > 10) throw new Exception("Please don't ask me to target more than 10 enemies. I am not a real UI.");
        if (asSecondary)
        {
            Debug.Log("Secondary targets: ");
            if (selectedActionIsOnMetaStance) mainTargets = AIModule_PlayerSide_ManualControl.waitingMainSecondaryTargets_ForMetaStanceActions[index];
            else mainTargets = AIModule_PlayerSide_ManualControl.waitingMainSecondaryTargets_ForStanceActions[index];
        }
        else
        {
            Debug.Log("Primary targets: ");
            if (selectedActionIsOnMetaStance) mainTargets = AIModule_PlayerSide_ManualControl.waitingMainPrimaryTargets_ForMetaStanceActions[index];
            else mainTargets = AIModule_PlayerSide_ManualControl.waitingMainPrimaryTargets_ForStanceActions[index];
        }
        Battler[][] subtargets = new Battler[mainTargets.Length][];
        for (int i = 0; i < subtargets.Length; i++)
        {
            subtargets[i] = BattleOverseer.GetBattlersWithinAOERangeOf(AIModule_PlayerSide_ManualControl.waitingBattler, mainTargets[i], selectedBattleAction.targetingType, selectedBattleAction.baseAOERadius, enemies);
            string s = i.ToString() + " " + mainTargets[i].battlerType + ": ";
            for (int st = 0; st < subtargets[i].Length; st++)
            {
                s += Environment.NewLine + subtargets[i][st].battlerType;
            }
            s += "=-=-=-=";
            Debug.Log(s);
        }
        Debug.Log("Proceeding to target acquisition. (Hit backspace to select a different action.)");
        Timing.RunCoroutine(_WaitForPlayerToSelectMainTargetFrom(mainTargets, enemies, alphaKeyCodes, numpadKeyCodes, asSecondary));
    }

    /// <summary>
    /// Coroutine: This is a goddamn mess, but it gets target selections from the player, lets them backtrack to action select if they want, and inputs the targets once we're ready.
    /// </summary>
    private IEnumerator<float> _WaitForPlayerToSelectMainTargetFrom (Battler[] mainTargets, Battler[] enemies, KeyCode[] alphaKeyCodes, KeyCode[] numpadKeyCodes, bool asSecondary)
    {
        if (asSecondary) Debug.Log("Secondary targets:");
        else Debug.Log("Primary targets:");
        bool targetAcquired = false;
        while (!targetAcquired)
        {
            if ((asSecondary == false && selectedMainPrimaryTarget != null) || (asSecondary == true && selectedMainSecondaryTarget != null))
            {
                if (Input.GetKeyDown(KeyCode.Return)) targetAcquired = true;
                else if (Input.GetKeyDown(KeyCode.Backspace))
                {
                    if (asSecondary) selectedMainSecondaryTarget = null;
                    else selectedMainPrimaryTarget = null;
                    Debug.Log("Select a different target, then.");
                }
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.Backspace))
                {
                    selectedBattleAction = null;
                    break;
                }
                for (int i = 0; i < mainTargets.Length; i++)
                {
                    if (Input.GetKeyDown(alphaKeyCodes[i]) || Input.GetKeyDown(numpadKeyCodes[i]))
                    {
                        Debug.Log("Select target set " + i.ToString() + "? Enter to confirm, Backspace to cancel.");
                        if (asSecondary) selectedMainSecondaryTarget = mainTargets[i];
                        else selectedMainPrimaryTarget = mainTargets[i];
                    }
                }
            }
            yield return 0;
        }
        if (targetAcquired)
        {
            Debug.Log("Confirmed target set");
            battlersListBuffer.Clear();
            Battler[] subtargets;
            if (asSecondary) 
            {
                battlersListBuffer.Add(selectedMainSecondaryTarget);
                subtargets = BattleOverseer.GetBattlersWithinAOERangeOf(AIModule_PlayerSide_ManualControl.waitingBattler, selectedMainSecondaryTarget, selectedBattleAction.targetingType, selectedBattleAction.baseAOERadius, enemies);
            }
            else
            {
                battlersListBuffer.Add(selectedMainPrimaryTarget);
                subtargets = BattleOverseer.GetBattlersWithinAOERangeOf(AIModule_PlayerSide_ManualControl.waitingBattler, selectedMainPrimaryTarget, selectedBattleAction.targetingType, selectedBattleAction.baseAOERadius, enemies);
            }
            for (int i = 0; i < subtargets.Length; i++) battlersListBuffer.Add(subtargets[i]);
            AIModule_PlayerSide_ManualControl.InputTargets(battlersListBuffer.ToArray(), asSecondary);
        }

    }
}