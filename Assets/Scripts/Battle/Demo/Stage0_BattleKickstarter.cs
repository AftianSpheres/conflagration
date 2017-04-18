using UnityEngine;
using CnfBattleSys;

/// <summary>
/// Starts the first ever battle.
/// </summary>
public class Stage0_BattleKickstarter : MonoBehaviour
{
    public GameObject bObj;
    	
	// Update is called once per frame
	void Update ()
    {
        if (BattleOverseer.overseerState == BattleOverseer.OverseerState.Offline && Input.GetKeyDown(KeyCode.Space))
        {
            BattleOverseer.StartBattle(FormationDatabase.Get(FormationType.TestFight));
            bObj.SetActive(true);
        }
	}
}
