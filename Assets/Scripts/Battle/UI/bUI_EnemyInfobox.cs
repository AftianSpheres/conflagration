using UnityEngine;
using TMPro;
using CnfBattleSys;

/// <summary>
/// An enemy infobox for the battle UI
/// </summary>
public class bUI_EnemyInfobox : MonoBehaviour
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
    public Camera battleCam;
    public TextMeshProUGUI guiText_EnemyName;
    public TextMeshProUGUI guiText_Level;
    public BattlerPuppet puppet;
    public bUI_ResourceBar hpBar;
    public bUI_ResourceBar staminaBar;
    private TextBank enemyNamesBank;
    private TextBank enemyInfoboxBank;

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
    /// Gets the real enemy name from the textbank and displays it.
    /// </summary>
    private void DisplayEnemyName ()
    {
        if (enemyNamesBank == null) enemyNamesBank = TextBankManager.Instance.GetCommonTextBank(typeof(BattlerType));
        guiText_EnemyName.SetText(enemyNamesBank.GetPage(puppet.battler.battlerType).text);
    }

    /// <summary>
    /// Sets level label text based on battler level.
    /// </summary>
    private void DisplayLevel ()
    {
        if (enemyInfoboxBank == null) enemyInfoboxBank = TextBankManager.Instance.GetTextBank("Battle/EnemyInfobox");
        guiText_Level.SetText(enemyInfoboxBank.GetPage("level_abbr").text + puppet.battler.level.ToString());
    }

    /// <summary>
    /// Associates this infobox and its child widgets with the specified puppet.
    /// </summary>
    public void AttachPuppet (BattlerPuppet puppet)
    {
        this.puppet = puppet;
        hpBar.AttachBattlerPuppet(puppet);
        staminaBar.AttachBattlerPuppet(puppet);
        SyncPositionWithPuppet();
        DisplayEnemyName();
        localState = LocalState.Initialized;
    }

    /// <summary>
    /// Positions the infobox in screen space to keep up with the puppet.
    /// </summary>
    public void SyncPositionWithPuppet ()
    {
        transform.position = battleCam.WorldToScreenPoint(puppet.transform.position);
    }
}
