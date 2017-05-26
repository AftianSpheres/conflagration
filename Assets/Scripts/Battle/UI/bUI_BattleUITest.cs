using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CnfBattleSys;
using CnfBattleSys.AI;

/// <summary>
/// Framework for testing the battle ui controller
/// </summary>
public class bUI_BattleUITest : bUI_BattleUIController
{
    void Update()
    {
        if (!actionWheel.isOpen)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) TestForAttacks();

        }
    }

    /// <summary>
    /// Pull up action wheel with a choice between attacks.
    /// </summary>
    private void TestForAttacks ()
    {
        Battler b = new Battler(new BattleFormation.FormationMember(BattlerDatabase.Get(BattlerType.TestPCUnit), Vector2.zero, StanceDatabase.Get(StanceType.TestStance_Melee), BattlerSideFlags.PlayerSide, 0));
        AIModule_PlayerSide_ManualControl.GetTurnActionsFromPlayer(b, true);
        SubmitCommand(Command.WheelFromTopLevel);
    }
}
