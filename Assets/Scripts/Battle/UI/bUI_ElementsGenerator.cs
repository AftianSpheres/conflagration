using UnityEngine;
using CnfBattleSys;

/// <summary>
/// MonoBehaviour that generates battle UI element widgets.
/// </summary>
public class bUI_ElementsGenerator : MonoBehaviour
{
    public bUI_InfoboxShell enemeInfoboxPrefab;
    public Transform enemyInfoboxesParent;
    public Transform playerInfoboxesParent;
    private bUI_InfoboxShell[] playerPartyInfoboxes;
    const int expectedNumberOfPlayerPartyInfoboxes = 4;

    /// <summary>
    /// MonoBehaviour.Awake ()
    /// </summary>
    void Awake ()
    {
        bUI_InfoboxShell[] _tmp_playerPartyInfoboxes = playerInfoboxesParent.GetComponentsInChildren<bUI_InfoboxShell>(); // Since no order can be promised here, we have to sort this before storing it
        playerPartyInfoboxes = new bUI_InfoboxShell[_tmp_playerPartyInfoboxes.Length];
        for (int i = 0; i < _tmp_playerPartyInfoboxes.Length; i++)
        {
            int index = _tmp_playerPartyInfoboxes[i].index;
            if (playerPartyInfoboxes[index] != null) Util.Crash("Multiple player party infoboxes of index " + i);
            else playerPartyInfoboxes[index] = _tmp_playerPartyInfoboxes[i];
        }
    }

    /// <summary>
    /// MonoBehaviour.Start ()
    /// </summary>
    void Start()
    {
        bUI_BattleUIController.instance.RegisterElementsGenerator(this);
        bUI_BattleUIController.instance.RegisterEnemyInfoboxGroup(enemyInfoboxesParent.gameObject);
        bUI_BattleUIController.instance.RegisterPlayerInfoboxGroup(playerInfoboxesParent.gameObject);
    }

    /// <summary>
    /// Generates an enemy infobox and attaches it to the given puppet.
    /// TO-DO: Way down the line this should probably actually distinguish friendly/neutral units from hostile ones.
    /// </summary>
    private void GetNPCInfoboxenFor (BattlerPuppet puppet)
    {
        bUI_InfoboxShell infoboxShell = Instantiate(enemeInfoboxPrefab, enemyInfoboxesParent);
        infoboxShell.gameObject.name = "Enemy infobox " + puppet.battler.side.ToString() + " " + puppet.battler.asSideIndex.ToString();
        puppet.AttachInfoboxShell(infoboxShell);
        infoboxShell.index = puppet.battler.index;
    }

    /// <summary>
    /// Gets one of the pre-baked player party infoboxes corresponding to given puppet's party-slot index and associates the two.
    /// </summary>
    private void GetPlayerInfoboxenFor(BattlerPuppet puppet)
    {
        if (puppet.battler.asSideIndex >= playerPartyInfoboxes.Length)
        {
            Debug.Log(puppet.gameObject.name + " is a higher party mbmber index than the bUI_ElementsGenerator's party infobox array can support, so we can't give it an infobox!");
        }
        else puppet.AttachInfoboxShell(playerPartyInfoboxes[puppet.battler.asSideIndex]);
    }

    /// <summary>
    /// Hides party infoboxes that aren't associated with party members.
    /// </summary>
    private void HideUnusedPlayerPartyInfoboxes ()
    {
        for (int i = 0; i < playerPartyInfoboxes.Length; i++)
        {
            if (playerPartyInfoboxes[i].state == bUI_InfoboxShell.State.Uninitialized) playerPartyInfoboxes[i].gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Assigns infoboxes to all battlers, then hides unused party infoboxes.
    /// </summary>
    public void AssignInfoboxesToBattlers ()
    {
        for (int b = 0; b < BattleOverseer.currentBattle.allBattlers.Length; b++)
        {
            if (BattleOverseer.currentBattle.allBattlers[b].side == BattlerSideFlags.PlayerSide) GetPlayerInfoboxenFor(BattleOverseer.currentBattle.allBattlers[b].puppet);
            else GetNPCInfoboxenFor(BattleOverseer.currentBattle.allBattlers[b].puppet);
        }
        HideUnusedPlayerPartyInfoboxes();
    }
}
