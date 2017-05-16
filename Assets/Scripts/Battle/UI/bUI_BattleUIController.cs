using UnityEngine;
using System.Collections;

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
        Attack,
        Break,
        Move,
        Run,
        CloseAttackMenu
    }

    public static bUI_BattleUIController instance { get; private set; }
    public bUI_ActionInfoArea actionInfoArea { get; private set; }
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
    /// Registers an action info area with the battle UI controller.
    /// </summary>
    public void RegisterActionInfoArea (bUI_ActionInfoArea _actionInfoArea)
    {
        actionInfoArea = _actionInfoArea;
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
}
