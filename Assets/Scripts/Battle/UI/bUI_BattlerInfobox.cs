using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using CnfBattleSys;

/// <summary>
/// A battler infobox for the battle UI
/// </summary>
public class bUI_BattlerInfobox : MonoBehaviour
{
    /// <summary>
    /// bUI_EnemyInfobox states
    /// </summary>
    public enum LocalState
    {
        None,
        Uninitialized,
        Initialized
    }

    public LocalState localState = LocalState.Uninitialized;
    public Image mugshot;
    public TextMeshProUGUI guiText_BattlerName;
    public TextMeshProUGUI guiText_BattlerLevel;
    public TextMeshProUGUI guiText_StanceName;
    public bUI_ResourceBar hpBar;
    public bUI_ResourceBar staminaBar;
    public bool lockPosition;
    private BattlerPuppet puppet;
    private static TextBank battlerNamesBank;
    private static TextBank battlerInfoboxBank;
    private static TextBank stanceNamesBank;
    private const string mugshotsResourcePath = "Battle/2D/UI/BattlerMugshot/";

    /// <summary>
    /// This is just a blob of nasty debugging stuff!!!
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (localState == LocalState.Uninitialized) AttachPuppet(puppet);
            else
            {
                puppet.battler.DealOrHealDamage(puppet.battler.currentHP / 2);
            }
        }
        
    }

    /// <summary>
    /// Sets level label text based on battler level.
    /// </summary>
    public void DisplayBattlerLevel()
    {
        if (battlerInfoboxBank == null) battlerInfoboxBank = TextBankManager.Instance.GetTextBank("Battle/EnemyInfobox");
        guiText_BattlerLevel.SetText(battlerInfoboxBank.GetPage("level_abbr").text + puppet.battler.level.ToString());
    }

    /// <summary>
    /// Gets the real name from the textbank and displays it.
    /// </summary>
    public void DisplayBattlerName ()
    {
        if (battlerNamesBank == null) battlerNamesBank = TextBankManager.Instance.GetCommonTextBank(typeof(BattlerType));
        guiText_BattlerName.SetText(battlerNamesBank.GetPage(puppet.battler.battlerType).text);
    }

    /// <summary>
    /// Gets the mugshot associated with the battler and displays it.
    /// </summary>
    public void DisplayMugshot ()
    {
        Sprite mugshotSprite = Resources.Load<Sprite>(mugshotsResourcePath + puppet.battler.battlerType.ToString());
        if (mugshotSprite == null)
        {
            Debug.Log("Warning: No mugshot for " + puppet.battler.battlerType.ToString());
            mugshotSprite = Resources.Load<Sprite>(mugshotsResourcePath + "badMugshot");
            if (mugshotSprite == null) Util.Crash(new Exception("...and the invalid mugshot placeholder couldn't be found, so we're gonna crash now."));
        }
        mugshot.sprite = mugshotSprite;
    }

    /// <summary>
    /// Gets the stance name from the textbank. Displays it.
    /// </summary>
    public void DisplayStanceName ()
    {
        if (stanceNamesBank == null) stanceNamesBank = TextBankManager.Instance.GetCommonTextBank(typeof(StanceType));
        guiText_StanceName.SetText(stanceNamesBank.GetPage(puppet.battler.currentStance.stanceID).text);
    }

    /// <summary>
    /// Associates this infobox and its child widgets with the specified puppet.
    /// </summary>
    public void AttachPuppet (BattlerPuppet puppet)
    {
        this.puppet = puppet;
        hpBar.AttachBattlerPuppet(puppet);
        staminaBar.AttachBattlerPuppet(puppet);
        if (!lockPosition) SyncPositionWithPuppet();
        DisplayBattlerName();
        DisplayBattlerLevel();
        if (guiText_StanceName != null) DisplayStanceName();
        if (mugshot != null) DisplayMugshot();
        localState = LocalState.Initialized;
    }

    /// <summary>
    /// Positions the infobox in screen space to keep up with the puppet.
    /// </summary>
    public void SyncPositionWithPuppet ()
    {
        transform.position = bUI_BattleUIController.instance.cameraHarness.viewportCam.WorldToScreenPoint(puppet.transform.position);
    }
}
