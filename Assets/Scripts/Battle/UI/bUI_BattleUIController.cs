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
    /// <summary>
    /// Commands that other parts of the battle UI can pass to BattleUIController.
    /// </summary>
    public enum Command
    {
        None,
        Back,
        Decide_AttackPrimary,
        Decide_AttackSecondary,
        Break,
        Move,
        Run,
        CloseWheel,
        Decide_Stance,
        WheelFromTopLevel
    }

    public static bUI_BattleUIController instance { get; private set; }
    public bUI_ActionInfoArea actionInfoArea { get; private set; }
    public bUI_ActionWheel actionWheel { get; private set; }
    public bUI_CameraHarness cameraHarness { get; private set; }
    public bUI_ElementsGenerator elementsGen { get; private set; }
    public bUI_TurnOrderArea turnOrderArea { get; private set; }
    public GameObject enemyInfoboxGroup { get; private set; }
    public GameObject playerInfoboxGroup { get; private set; }

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
    /// Submit a command to the battle UI controller.
    /// </summary>
    public void SubmitCommand (Command command)
    {
        switch (command)
        {
            case Command.Decide_AttackPrimary:
                actionWheel.DecideAttacks(false);
                break;
            case Command.Decide_AttackSecondary:
                actionWheel.DecideAttacks(true);
                break;
            case Command.Break:
                break;
            case Command.Back:
                if (actionWheel.isOpen) actionWheel.DisposeOfTopDecision();
                break;
            case Command.CloseWheel:
                actionWheel.Close();
                break;
            case Command.Move:
                Debug.Log("Movement: not implemented");
                break;
            case Command.Run:
                Debug.Log("Running: not implemented");
                break;
            case Command.Decide_Stance:
                actionWheel.DecideStances();
                break;
            case Command.WheelFromTopLevel:
                if (AIModule_PlayerSide_ManualControl.WaitingForStanceInput()) SubmitCommand(Command.Decide_Stance);
                //else (actionWheel.)
                break;
        }
    }
    
    private void PushStanceBreakTurnPacketToWaitingBattler ()
    {

    }
}
