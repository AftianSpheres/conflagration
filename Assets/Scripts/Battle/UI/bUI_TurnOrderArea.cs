using UnityEngine;
using UnityEngine.UI;
using System;
using CnfBattleSys;
using TMPro;

/// <summary>
/// The turn order display for the battle UI.
/// </summary>
public class bUI_TurnOrderArea : MonoBehaviour
{
    public Color bgColor_Enemy;
    public Color bgColor_Friend;
    public Color bgColor_Neutral;
    public Color bgColor_Player;
    private Battler[] primaryTurnOrderBattlers;
    private Battler[] prospectiveTurnOrderBattlers;
    private bool realOrderIsStale { get { return realOrderGenTurn < BattleOverseer.currentBattle.turnManagementSubsystem.elapsedTurns; } }
    private int realOrderGenTurn = -1;

    private const string iconsResourcePath = "Battle/2D/UI/BattlerIcon/Cutin";

    /// <summary>
    /// MonoBehaviour.Awake ()
    /// </summary>
    void Awake()
    {
    
    }

    /// <summary>
    /// MonoBehaviour.Start ()
    /// </summary>
    void Start()
    {
        
    }

    /// <summary>
    /// Resets display based on last real turn order battler set.
    /// </summary>
    public void CancelPreview ()
    {
        DisplayWith(primaryTurnOrderBattlers);
    }

    /// <summary>
    /// Sets real turn order battler set and displays it.
    /// </summary>
    public void ConformToTurnOrder ()
    {
        primaryTurnOrderBattlers = BattleOverseer.currentBattle.GetBattlersByTurnOrder();
        DisplayWith(primaryTurnOrderBattlers);
    }

    /// <summary>
    /// Hides the turn info area.
    /// </summary>
    public void Hide ()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Sets prospective turn order battler set and displays it.
    /// </summary>
    public void PreviewTurnOrderForDelayOf (float delay)
    {
        prospectiveTurnOrderBattlers = BattleOverseer.currentBattle.GetBattlersBySimulatedTurnOrder(delay);
        DisplayWith(prospectiveTurnOrderBattlers);
    }

    /// <summary>
    /// Unhides the turn info area.
    /// </summary>
    public void Unhide ()
    {
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Updates the turn order display based on the given list of battlers.
    /// This runs the exact same code path whether we're working with real or prospective turn order,
    /// because we can just detect a tentative turn order placement by checking units after position 0
    /// to see if they're actually the same unit that's at position 0.
    /// </summary>
    private void DisplayWith(Battler[] battlersArray)
    {

    }

}