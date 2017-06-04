using UnityEngine;
using CnfBattleSys;
using TMPro;

/// <summary>
/// UI element that displays info on an action.
/// </summary>
public class bUI_ActionWheelInfobox : MonoBehaviour
{
    private bUI_ActionWheel wheel;
    private TextMeshProUGUI infoboxContents;
    private static TextBank actionDescsBank;
    private static TextBank commandDescsBank;
    private static TextBank stanceDescsBank;

    /// <summary>
    /// MonoBehaviour.Awake ()
    /// </summary>
    void Awake ()
    {
        infoboxContents = GetComponentInChildren<TextMeshProUGUI>();
    }

    /// <summary>
    /// Clears the infobox.
    /// </summary>
    public void Clear()
    {
        infoboxContents.text = string.Empty;
    }

    /// <summary>
    /// Updates infobox based on wheel position.
    /// </summary>
    public void OnWheelPositionChange ()
    {
        switch (wheel.decisionType)
        {
            case bUI_ActionWheel.DecisionType.ActionSelect:
                Display(wheel.selectedAction);
                break;
            case bUI_ActionWheel.DecisionType.StanceSelect:
                Display(wheel.selectedStance);
                break;
            case bUI_ActionWheel.DecisionType.BattleUI:
                Display(wheel.selectedCommand);
                break;
            default:
                Util.Crash(wheel.decisionType, this, gameObject);
                break;
        }
    }

    /// <summary>
    /// Pairs with the given action wheel.
    /// </summary>
    public void PairWithWheel (bUI_ActionWheel _wheel)
    {
        wheel = _wheel;
        Clear();
    }

    /// <summary>
    /// Makes the infobox visible and updates it to display info on
    /// the specified action.
    /// </summary>
    private void Display (BattleAction battleAction)
    {
        // Don't display SP cost or delay here - update those seamlessly when changing wheel selections!
        if (actionDescsBank == null) actionDescsBank = TextBankManager.Instance.GetTextBank("System/ActionDesc_Short");
        infoboxContents.text = actionDescsBank.GetPage(battleAction.actionID.ToString()).text;
    }

    /// <summary>
    /// Makes the infobox visible and updates it to display info on
    /// the specified stance
    /// </summary>
    private void Display(BattleStance stance)
    {
        if (stanceDescsBank == null) stanceDescsBank = TextBankManager.Instance.GetTextBank("System/StanceDesc_Short");
        infoboxContents.text = commandDescsBank.GetPage(stance.stanceID.ToString()).text;
    }

    /// <summary>
    /// Makes the infobox visible and updates it to display info on
    /// the specified command.
    /// </summary>
    private void Display (bUI_Command command)
    {
        if (commandDescsBank == null) commandDescsBank = TextBankManager.Instance.GetTextBank("Battle/bUI_CommandDesc_Short");
        infoboxContents.text = commandDescsBank.GetPage(command.ToString()).text;
    }
}
