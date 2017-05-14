using UnityEngine;
using TMPro;

/// <summary>
/// Displays infostring in the test menu.
/// </summary>
public class TestMenu_InfoString : MonoBehaviour
{
    public TextMeshProUGUI guiText_InfoString;
    private TextBank testMenuBank;

    /// <summary>
    /// MonoBehaviour.Awake ()
    /// </summary>
    void Awake ()
    {
        TryToUpdateInfoString();
    }

    /// <summary>
    /// MonoBehaviour.Update ()
    /// </summary>
    void Update()
    {
        TryToUpdateInfoString();
    }

    /// <summary>
    /// Updates the infostring as soon as possible.
    /// Because the test menu is designed to be opened frequently in editor play mode,
    /// we engineer in safeties for events that can't actually happen during gameplay.
    /// </summary>
    private void TryToUpdateInfoString ()
    {
        if (TextBankManager.Instance != null)
        {
            testMenuBank = TextBankManager.Instance.GetTextBank("TestMenu/battle");
            guiText_InfoString.text = testMenuBank.GetPage("infoString").text.Replace("[#buildstring]", Util.buildNumber.ToString());
            Destroy(this);
        }
    }

}
