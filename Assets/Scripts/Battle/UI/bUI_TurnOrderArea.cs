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
    public bUI_TurnOrderPanel panelsPrototype;
    private Battler[] primaryTurnOrderBattlers;
    private bUI_TurnOrderPanel[] panels;
    private RectTransform rectTransform;
    private bool realOrderIsStale { get { return realOrderGenTurn < BattleOverseer.currentBattle.turnManagementSubsystem.elapsedTurns; } }
    private int realOrderGenTurn = -1;

    /// <summary>
    /// MonoBehaviour.Awake ()
    /// </summary>
    void Awake ()
    {
        rectTransform = GetComponent<RectTransform>();
        GeneratePanels();
    }

    /// <summary>
    /// MonoBehaviour.Start ()
    /// </summary>
    void Start ()
    {
        bUI_BattleUIController.instance.RegisterTurnOrderArea(this);
    }

    /// <summary>
    /// Sets real turn order battler set and displays it.
    /// </summary>
    public void ConformToTurnOrder ()
    {
        if (realOrderIsStale) primaryTurnOrderBattlers = BattleOverseer.currentBattle.GetBattlersByTurnOrder();
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
        DisplayWith(BattleOverseer.currentBattle.GetBattlersBySimulatedTurnOrder(delay));
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
    private void DisplayWith (Battler[] battlersArray)
    {
        for (int i = 0; i < panels.Length; i++)
        {
            if (i < battlersArray.Length) panels[i].PairWithBattler(battlersArray[i]);
            else panels[i].Hide();
        }
    }

    /// <summary>
    /// Generate the turn order panels.
    /// </summary>
    private void GeneratePanels ()
    {
        RectTransform panelsRectTransform = panelsPrototype.GetComponent<RectTransform>();
        int panelCount = Mathf.FloorToInt(rectTransform.sizeDelta.y / panelsRectTransform.sizeDelta.y);
        panels = new bUI_TurnOrderPanel[panelCount];
        for (int i = 0; i < panelCount; i++)
        {
            bUI_TurnOrderPanel newPanel = Instantiate(panelsPrototype, rectTransform);
            if (i == 0)
            {
                newPanel.transform.localPosition = new Vector3((panelsRectTransform.sizeDelta.x / 2) * 1.5f, -(panelsRectTransform.sizeDelta.y * i), 0);
            }
            else newPanel.transform.localPosition = new Vector3(panelsRectTransform.sizeDelta.x / 2, -(panelsRectTransform.sizeDelta.y * i), 0);
            newPanel.gameObject.name = "Turn Order Panel " + i;
            newPanel.SetIndex(i);
            panels[i] = newPanel;
        }
        panelsPrototype.gameObject.SetActive(false);
        Destroy(panelsPrototype);
    }
}