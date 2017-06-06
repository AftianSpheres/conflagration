using UnityEngine;
using TMPro;

/// <summary>
/// Displays infostring in the test menu.
/// </summary>
public class TestMenu_InfoString : MonoBehaviour
{
    private TextMeshProUGUI guiText_InfoString;
    private TextBank testMenuBank;

    /// <summary>
    /// MonoBehaviour.Awake ()
    /// </summary>
    void Awake ()
    {
        guiText_InfoString = GetComponentInChildren<TextMeshProUGUI>();
        TextBankManager.DoOnceOnline(UpdateInfoString);
    }

    /// <summary>
    /// Updates the infostring as soon as possible.
    /// Because the test menu is designed to be opened frequently in editor play mode,
    /// we engineer in safeties for events that can't actually happen during gameplay.
    /// </summary>
    private void UpdateInfoString ()
    {
        testMenuBank = TextBankManager.Instance.GetTextBank("TestMenu/common");
        guiText_InfoString.text = testMenuBank.GetPage("infoString").text.Replace("!buildstring!", Util.buildNumber.ToString());
    }

}
