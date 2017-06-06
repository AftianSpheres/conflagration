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
    public InfoboxType infoboxType;
    public BattlerPuppet puppet { get; private set; }
    public Image bgImage;
    public Image mugshot;
    public TextMeshProUGUI guiText_BattlerName;
    public TextMeshProUGUI guiText_BattlerLevel;
    public TextMeshProUGUI guiText_StanceName;
    public bool lockPosition;
    private bUI_ResourceBar hpBar;
    private bUI_ResourceBar staminaBar;
    //private bUI_ResourceBar subweaponsChargeBar; (this mechanic doesn't exist yet)
    //private bUI_InfoboxShell infoboxShell;
    private bUI_BattlerStatusBar statusBar;
    private static TextBank battlerNamesBank;
    private static TextBank battlerInfoboxBank;
    private static TextBank stanceNamesBank;
    private const string mugshotsResourcePath = "Battle/2D/UI/BattlerMugshot/";

    /// <summary>
    /// MonoBehaviour.Awake ()
    /// </summary>
    void Awake()
    {
        bUI_ResourceBar[] resourceBars = GetComponentsInChildren<bUI_ResourceBar>();
        for (int i = 0; i < resourceBars.Length; i++)
        {
            switch (resourceBars[i].resourceType)
            {
                case bUI_ResourceBar.ResourceType.HP:
                    if (hpBar != null) Util.Crash("Multiple HP bars as children of infobox " + gameObject.name);
                    else hpBar = resourceBars[i];
                    break;
                case bUI_ResourceBar.ResourceType.Stamina:
                    if (staminaBar != null) Util.Crash("Multiple stamina bars as children of infobox " + gameObject.name);
                    else staminaBar = resourceBars[i];
                    break;
                default:
                    Util.Crash(resourceBars[i].resourceType, this, gameObject);
                    break;
            }
        }
        //infoboxShell = transform.parent.GetComponent<bUI_InfoboxShell>();
        statusBar = GetComponentInChildren<bUI_BattlerStatusBar>();
    }

    /// <summary>
    /// Associates this infobox and its child widgets with the specified puppet.
    /// </summary>
    public void AttachPuppet(BattlerPuppet _puppet)
    {
        puppet = _puppet;
        if (hpBar != null) hpBar.AttachBattlerPuppet(_puppet);
        if (staminaBar != null) staminaBar.AttachBattlerPuppet(_puppet);
        if (statusBar != null) statusBar.PairWithPuppet(puppet);
        if (!lockPosition) SyncPositionWithPuppet();
        if (bgImage != null) bgImage.color = bUI_BattleUIController.instance.GetPanelColorFor(_puppet.battler);
        if (guiText_BattlerName != null) DisplayBattlerName();
        if (guiText_BattlerLevel != null) DisplayBattlerLevel();
        if (guiText_StanceName != null) DisplayStanceName();
        if (mugshot != null) DisplayMugshot();
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
    /// Handle value changes on HP bar.
    /// </summary>
    public void HandleHPValueChanges ()
    {
        if (hpBar != null) hpBar.HandleValueChanges();
    }

    /// <summary>
    /// Handles previews on resource bars for given action.
    /// </summary>
    public void HandleResourceBarPreviews (BattleAction action)
    {
        int finalStamina = puppet.battler.currentStamina - puppet.battler.CalcActionStaminaCost(action.baseSPCost);
        if (action.actionID == ActionType.INTERNAL_BreakOwnStance) finalStamina = 0; // breaking your stance automatically empties your stamina bar
        else if (finalStamina < 0) finalStamina = 0;
        else if (finalStamina > puppet.battler.currentStance.maxStamina) finalStamina = puppet.battler.currentStance.maxStamina;
        staminaBar.PreviewValue(finalStamina);
    }

    /// <summary>
    /// Handle value changes on stamina bar.
    /// </summary>
    public void HandleStaminaValueChanges ()
    {
        if (staminaBar != null) staminaBar.HandleValueChanges();
    }

    /// <summary>
    /// Cleans up existing resource bar previews.
    /// </summary>
    public void NullifyResourceBarPreviews ()
    {
        staminaBar.UnpreviewValue();
    }

    /// <summary>
    /// Positions the infobox in screen space to keep up with the puppet.
    /// </summary>
    public void SyncPositionWithPuppet ()
    {
        transform.position = bUI_BattleUIController.instance.cameraHarness.viewportCam.WorldToScreenPoint(puppet.transform.position);
    }
}
