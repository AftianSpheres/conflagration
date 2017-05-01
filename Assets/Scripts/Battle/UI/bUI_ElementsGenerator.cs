using UnityEngine;
using System.Collections;
using CnfBattleSys;

/// <summary>
/// MonoBehaviour that generates battle YI element widgets.
/// </summary>
public class bUI_ElementsGenerator : MonoBehaviour
{
    public GameObject enemyInfoboxPrefab;
    public Transform enemyInfoboxesParent;
    public bUI_BattlerInfobox[] playerPartyInfoboxes;
    public static bUI_ElementsGenerator instance { get; private set; }

    /// <summary>
    /// MonoBehaviour.Awake()
    /// </summary>
    void Awake()
    {
        instance = this;
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
        if (playerPartyInfoboxes.Length <= puppet.battler.asSideIndex)
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
        for (int i = 0; i < playerPartyInfoboxes.Length; i++)
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
