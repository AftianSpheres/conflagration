using UnityEngine;
using CnfBattleSys;
using TMPro;

/// <summary>
/// UI element that displays info on an action.
/// </summary>
public class bUI_ActionInfoArea : MonoBehaviour
{
    /// <summary>
    /// Possible action info area states.
    /// </summary>
    private enum LocalState
    {
        None,
        Closed,
        Open
    }

    private LocalState localState = LocalState.Closed;
    public TextMeshProUGUI actionName;
    public TextMeshProUGUI actionDescription;
    public TextMeshProUGUI actionDetails;
    public TextMeshProUGUI staminaWarning;
    private static TextBank actionNamesBank;
    private static TextBank actionDescsBank;
    private static TextBank localBank;
    private static TextLangType textLangType = TextLangType.None;
    const string delayPlaceholder = "[delay]";
    const string staminaCostPlaceholder = "[spCost]";

    /// <summary>
    /// MonoBehaviour.Awake ()
    /// </summary>
    void Awake ()
    {
        bUI_BattleUIController.instance.RegisterActionInfoArea(this);
    }

    /// <summary>
    /// MonoBehaviour.Update()
    /// Make sure object status is conformed to local state.
    /// </summary>
    void Update ()
    {
        if (localState == LocalState.Closed && gameObject.activeSelf == true) gameObject.SetActive(false);
        else if (localState == LocalState.Open && gameObject.activeSelf == false) gameObject.SetActive(true); 
    }

    /// <summary>
    /// Makes the ActionInfoArea visible and updates it to display info on
    /// the specified action, for the given user.
    /// </summary>
    public void Display (ActionType actionType, Battler user)
    {
        localState = LocalState.Open;
        gameObject.SetActive(true);
        BattleAction battleAction = ActionDatabase.Get(actionType);
        if (actionNamesBank == null) actionNamesBank = TextBankManager.Instance.GetCommonTextBank(typeof(ActionType));
        if (actionDescsBank == null) actionDescsBank = TextBankManager.Instance.GetTextBank("System/ActionDesc_Short");
        if (localBank == null) localBank = TextBankManager.Instance.GetTextBank("Battle/ActionInfoArea");
        if (textLangType != TextBankManager.Instance.textLangType)
        {
            textLangType = TextBankManager.Instance.textLangType;
            staminaWarning.text = localBank.GetPage("warning").text;
        }
        actionName.text = actionNamesBank.GetPage(actionType).text;
        actionDescription.text = actionDescsBank.GetPage(actionType.ToString()).text;
        string detailsString = localBank.GetPage("details").text;
        int finalStaminaCost = user.CalcActionStaminaCost(battleAction.baseSPCost);
        detailsString = detailsString.Replace(delayPlaceholder, (battleAction.baseDelay * user.speedFactor).ToString());
        detailsString = detailsString.Replace(staminaCostPlaceholder, finalStaminaCost.ToString());
        actionDetails.text = detailsString;
        staminaWarning.gameObject.SetActive(user.currentStamina <= finalStaminaCost);
    }

    /// <summary>
    /// Hides the ActionInfoArea.
    /// </summary>
    public void Undisplay ()
    {
        localState = LocalState.Closed;
        gameObject.SetActive(false);
    }
}
