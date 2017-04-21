﻿using UnityEngine;
using CnfBattleSys;

/// <summary>
/// Starts the first ever battle.
/// </summary>
public class Stage0_BattleKickstarter : MonoBehaviour
{
    int ctr = 0;
    	
	// Update is called once per frame
	void Update ()
    {
        if (BattleOverseer.overseerState == BattleOverseer.OverseerState.Offline && ctr > 20)
        {
            BattleOverseer.StartBattle(FormationDatabase.Get(FormationType.TestFight));
            Destroy(gameObject);
        }
        else ctr++;
	}
}
