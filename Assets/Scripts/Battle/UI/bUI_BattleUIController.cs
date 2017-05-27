using System;
using System.Collections.Generic;
using UnityEngine;
using CnfBattleSys;
using CnfBattleSys.AI;

/// <summary>
/// Handles storing references to various battle UI elements and (eventually) communication between them.
/// </summary>
public class bUI_BattleUIController : MonoBehaviour
{
    private enum State
    {
        None,
        Wheel_Stances,
        Wheel_TopLevel,
        Wheel_Stance,
        Wheel_MetaStance,
        AcquiringTargets
    }

    public static bUI_BattleUIController instance { get; private set; }
    public bUI_ActionInfoArea actionInfoArea { get; private set; }
    public bUI_ActionWheel actionWheel { get; private set; }
    public bUI_CameraHarness cameraHarness { get; private set; }
    public bUI_ElementsGenerator elementsGen { get; private set; }
    public bUI_TurnOrderArea turnOrderArea { get; private set; }
    public GameObject enemyInfoboxGroup { get; private set; }
    public GameObject playerInfoboxGroup { get; private set; }
    public BattleAction displayAction { get; private set; }
    public Battler displayBattler { get; private set; }
    public BattleStance displayStance { get; private set; }
    public BattleStance[] displayStanceSet { get; private set; }
    private State state;
    protected bool skip = false;

    /// <summary>
    /// MonoBehaviour.Awake()
    /// </summary>
    void Awake ()
    {
        instance = this;
    }

    /// <summary>
    /// Clean up UI state after a player turn finishes.
    /// </summary>
    public void Cleanup ()
    {
        actionWheel.ClearDecisionStack();
    }

    /// <summary>
    /// Registers an action info area with the battle UI controller.
    /// </summary>
    public void RegisterActionInfoArea (bUI_ActionInfoArea _actionInfoArea)
    {
        actionInfoArea = _actionInfoArea;
    }

    /// <summary>
    /// Registers action wheel w/ battle ui controller.
    /// </summary>
    public void RegisterActionWheel (bUI_ActionWheel _actionWheel)
    {
        actionWheel = _actionWheel;
    }

    /// <summary>
    /// Registers camera harness w/ battle ui controller
    /// </summary>
    public void RegisterCameraHarness (bUI_CameraHarness _cameraHarness)
    {
        cameraHarness = _cameraHarness;
    }

    /// <summary>
    /// Registers elements generator with battle ui controller
    /// </summary>
    public void RegisterElementsGenerator (bUI_ElementsGenerator _elementsGen)
    {
        elementsGen = _elementsGen;
    }

    /// <summary>
    /// Registers enemy infobox group w/ battle ui controller
    /// </summary>
    public void RegisterEnemyInfoboxGroup (GameObject _enemyInfoboxGroup)
    {
        enemyInfoboxGroup = _enemyInfoboxGroup;
    }

    /// <summary>
    /// Registers player infobox group w/ battle ui controller
    /// </summary>
    public void RegisterPlayerInfoboxGroup (GameObject _playerInfoboxGroup)
    {
        playerInfoboxGroup = _playerInfoboxGroup;
    }

    /// <summary>
    /// Registers turn order area with battle ui controller
    /// </summary>
    public void RegisterTurnOrderArea (bUI_TurnOrderArea _turnOrderArea)
    {
        turnOrderArea = _turnOrderArea;
    }

    /// <summary>
    /// Submit an action to the battle UI controller,
    /// which dispatches it to the fake-AI module.
    /// </summary>
    public void SubmitBattleAction (BattleAction action)
    {
        if (AIModule_PlayerSide_ManualControl.WaitingForActionInput()) AIModule_PlayerSide_ManualControl.InputAction(action);
        else Util.Crash("Tried to submit action, but psuedo-AI module wasn't waiting for action");
    }

    /// <summary>
    /// Submit a action to the battle UI controller,
    /// which dispatches it to the fake-AI module.
    /// </summary>
    public void SubmitBattleStance (BattleStance stance)
    {
        skip = true;
        Debug.Log(stance.stanceID);
        return;
        if (AIModule_PlayerSide_ManualControl.WaitingForStanceInput()) AIModule_PlayerSide_ManualControl.InputStance(stance);
        else Util.Crash("Tried to submit stance, but psuedo-AI module wasn't waiting for stance");
    }

    /// <summary>
    /// Submit a command to the battle UI controller.
    /// </summary>
    public void SubmitCommand (bUI_Command command)
    {
        switch (command)
        {
            case bUI_Command.Decide_AttackPrimary:
                state = State.Wheel_Stance;
                SetDisplayBattlerData();
                actionWheel.DecideAttacks();
                break;
            case bUI_Command.Decide_AttackSecondary:
                state = State.Wheel_MetaStance;
                SetDisplayBattlerData();
                actionWheel.DecideAttacks();
                break;
            case bUI_Command.Break:
                Debug.Log("Breaking: not implemented");
                break;
            case bUI_Command.Back:
                if (state == State.Wheel_Stance || state == State.Wheel_MetaStance) state = State.Wheel_TopLevel;
                else Util.Crash("Can't go back now");
                // ...
                break;
            case bUI_Command.CloseWheel:
                actionWheel.Close();
                break;
            case bUI_Command.Move:
                Debug.Log("Movement: not implemented");
                break;
            case bUI_Command.Run:
                Debug.Log("Running: not implemented");
                break;
            case bUI_Command.Decide_Stance:
                state = State.Wheel_Stances;
                SetDisplayBattlerData();
                actionWheel.DecideStances();
                break;
            case bUI_Command.WheelFromTopLevel:
                if (AIModule_PlayerSide_ManualControl.WaitingForStanceInput() && !skip) SubmitCommand(bUI_Command.Decide_Stance);
                else
                {
                    state = State.Wheel_TopLevel;
                    SetDisplayBattlerData();
                    actionWheel.DecideTopLevel();
                }
                break;
        }
    }
    
    private void SetDisplayBattlerData ()
    {
        displayBattler = AIModule_PlayerSide_ManualControl.waitingBattler;
        switch (state)
        {
            case State.Wheel_TopLevel:
            case State.Wheel_Stances:
                displayAction = null;
                displayStance = displayBattler.currentStance;
                displayStanceSet = displayBattler.stances;
                break;
            case State.Wheel_Stance:
                displayAction = null;
                displayStance = displayBattler.currentStance;
                displayStanceSet = null;
                break;
            case State.Wheel_MetaStance:
                displayAction = null;
                displayStance = displayBattler.metaStance;
                displayStanceSet = null;
                break;
            case State.AcquiringTargets:
                displayAction = AIModule_PlayerSide_ManualControl.waitingAction;
                displayStance = displayBattler.currentStance;
                displayStanceSet = null;
                break;
        }
    }
}
