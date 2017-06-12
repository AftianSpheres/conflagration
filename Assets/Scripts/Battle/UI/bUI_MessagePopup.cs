using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CnfBattleSys;

/// <summary>
/// The message popup whatsit that appears during attack animations.
/// </summary>
public class bUI_MessagePopup : MonoBehaviour
{
    public bool isOpen { get { return gameObject.activeSelf; } }
    private Image bgImage;
    private TextMeshProUGUI contents;
    private static TextBank actionNamesBank;

    /// <summary>
    /// MonoBehaviour.Awake ()
    /// </summary>
    void Awake ()
    {
        bgImage = GetComponent<Image>();
        contents = GetComponentInChildren<TextMeshProUGUI>();
        Close();
    }

    /// <summary>
    /// MonoBehaviour.Start ()
    /// </summary>
    void Start ()
    {
        bUI_BattleUIController.instance.RegisterMessagePopup(this);
    }

    /// <summary>
    /// Closes the message popup.
    /// </summary>
    public void Close ()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Opens the message popup on the given action type.
    /// </summary>
    public void OpenOn (ActionType actionType, Battler user)
    {
        gameObject.SetActive(true);
        if (actionNamesBank == null) actionNamesBank = TextBankManager.Instance.GetCommonTextBank(typeof(ActionType));
        contents.text = actionNamesBank.GetPage(actionType).text;
        bgImage.color = bUI_BattleUIController.instance.GetPanelColorFor(user);
    }
}
