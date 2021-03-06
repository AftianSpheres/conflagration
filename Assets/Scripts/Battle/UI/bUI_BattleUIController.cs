﻿using System;
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
    public Color bgColor_Enemy;
    public Color bgColor_Friend;
    public Color bgColor_Neutral;
    public Color bgColor_Player;
    public static bUI_BattleUIController instance { get; private set; }
    public bUI_ActionWheel actionWheel { get; private set; }
    public bUI_CameraHarness cameraHarness { get; private set; }
    public bUI_ElementsGenerator elementsGen { get; private set; }
    public bUI_MessagePopup messagePopup { get; private set; }
    public bUI_TurnOrderArea turnOrderArea { get; private set; }
    public GameObject enemyInfoboxGroup { get; private set; }
    public GameObject playerInfoboxGroup { get; private set; }
    public BattleAction displayAction { get; private set; }
    public Battler displayBattler { get; private set; }
    public BattleStance displayStance { get; private set; }
    public BattleStance[] displayStanceSet { get; private set; }
    private State state;

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
    /// Get the color of the background for a panel associated with the given battler.
    /// </summary>
    public Color GetPanelColorFor(Battler battler)
    {
        switch (BattleUtility.GetRelativeSidesFor(battler.side, BattlerSideFlags.PlayerSide))
        {
            case TargetSideFlags.MySide:
                return bgColor_Player;
            case TargetSideFlags.MyFriends:
                return bgColor_Friend;
            case TargetSideFlags.MyEnemies:
                return bgColor_Enemy;
            case TargetSideFlags.Neutral:
                return bgColor_Neutral;
            default:
                Util.Crash(BattleUtility.GetRelativeSidesFor(battler.side, BattlerSideFlags.PlayerSide), this, gameObject);
                return Color.clear;
        }
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
    /// Registers message popup w/ battle ui controller
    /// </summary>
    public void RegisterMessagePopup (bUI_MessagePopup _messagePopup)
    {
        messagePopup = _messagePopup;
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
                else Util.Crash("Can't go back now: " + state);
                SetDisplayBattlerData();
                actionWheel.DisposeOfTopDecision();
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
                state = State.Wheel_TopLevel;
                if (AIModule_PlayerSide_ManualControl.WaitingForStanceInput()) goto case bUI_Command.Decide_Stance;
                else
                {
                    SetDisplayBattlerData();
                    actionWheel.DecideTopLevel();
                }
                break;
        }
    }
    
    /// <summary>
    /// Sets the data that other parts of the UI refer to when presenting information to the player.
    /// </summary>
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
