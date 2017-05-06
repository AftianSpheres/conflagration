using UnityEngine;
using System;
using System.Collections.Generic;
using CnfBattleSys;

/// <summary>
/// MonoBehaviour that generates battle UI element widgets.
/// </summary>
public class bUI_ElementsGenerator : MonoBehaviour
{  
    public GameObject enemyInfoboxPrefab;
    public Transform enemyInfoboxesParent;
    public Transform playerInfoboxesParent;
    private List<bUI_BattlerInfobox> playerPartyInfoboxes;
    const int expectedNumberOfPlayerPartyInfoboxes = 4;

    /// <summary>
    /// MonoBehaviour.Awake()
    /// </summary>
    void Awake()
    {
        bUI_BattleUIController.instance.RegisterElementsGenerator(this);
        bUI_BattleUIController.instance.RegisterEnemyInfoboxGroup(enemyInfoboxesParent.gameObject);
        bUI_BattleUIController.instance.RegisterPlayerInfoboxGroup(playerInfoboxesParent.gameObject);
        playerPartyInfoboxes = new List<bUI_BattlerInfobox>(expectedNumberOfPlayerPartyInfoboxes);
        for (int i = 0; i < playerInfoboxesParent.childCount; i++)
        {
            GameObject go = playerInfoboxesParent.transform.Find("Player Infobox " + i.ToString()).gameObject;
            if (go == null) break; // if there are more children than player infoboxes, it's because there are ui widgets or whatever, so we can quit looking instead of going through all of those
            bUI_BattlerInfobox playerInfobox = go.GetComponent<bUI_BattlerInfobox>();
            if (playerInfobox == null) throw new Exception("No battler infobox behavior on player infobox no. " + i.ToString());
            playerPartyInfoboxes.Add(playerInfobox);
        }
    }

    /// <summary>
    /// Generates an enemy infobox and attaches it to the given puppet.
    /// TO-DO: Way down the line this should probably actually distinguish friendly/neutral units from hostile ones.
    /// </summary>
    private void GetEnemyInfoboxFor (BattlerPuppet puppet)
    {
        bUI_BattlerInfobox infobox = Instantiate(enemyInfoboxPrefab).GetComponent<bUI_BattlerInfobox>();
        infobox.gameObject.name = "Enemy infobox " + puppet.battler.side.ToString() + " " + puppet.battler.asSideIndex.ToString();
        infobox.transform.SetParent(enemyInfoboxesParent);
        infobox.AttachPuppet(puppet);
    }

    /// <summary>
    /// Gets one of the pre-baked player party infoboxes corresponding to given puppet's party-slot index and associates the two.
    /// </summary>
    private void GetPlayerPartyInfoboxFor(BattlerPuppet puppet)
    {
        if (playerPartyInfoboxes.Count <= puppet.battler.asSideIndex)
        {
            Debug.Log(puppet.gameObject.name + " is a higher party mbmber index than the bUI_ElementsGenerator's party infobox array can support, so we can't give it an infobox!");
        }
        else
        {
            bUI_BattlerInfobox infobox = playerPartyInfoboxes[puppet.battler.asSideIndex];
            infobox.AttachPuppet(puppet);
        }
    }

    /// <summary>
    /// Hides party infoboxes that aren't associated with party members.
    /// </summary>
    private void HideUnusedPlayerPartyInfoboxes ()
    {
        for (int i = 0; i < playerPartyInfoboxes.Count; i++)
        {
            if (playerPartyInfoboxes[i].localState == bUI_BattlerInfobox.LocalState.Uninitialized) playerPartyInfoboxes[i].gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Assigns infoboxes to all battlers, then hides unused party infoboxes.
    /// </summary>
    public void AssignInfoboxesToBattlers ()
    {
        for (int b = 0; b < BattleOverseer.allBattlers.Count; b++)
        {
            if (BattleOverseer.allBattlers[b].side == BattlerSideFlags.PlayerSide) GetPlayerPartyInfoboxFor(BattleOverseer.allBattlers[b].puppet);
            else GetEnemyInfoboxFor(BattleOverseer.allBattlers[b].puppet);
        }
        HideUnusedPlayerPartyInfoboxes();
    }
}
