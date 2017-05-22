using System;
using UnityEngine;

/// <summary>
/// MonoBehaviour that kicks you out of the battle system scene if you try and enter without loading a venue.
/// If the test menu is available, it'll send you there; otherwise, it'll crash.
/// </summary>
public class ForbidDirectBattleSystemSceneEntry : MonoBehaviour {

    /// <summary>
    /// MonoBehaviour.Update()
    /// </summary>
    void Update ()
    {
        if (BattleTransitionManager.Instance != null)
        {
            if (BattleTransitionManager.Instance.state == BattleTransitionManager.State.OutOfBattle)
            {
                gameObject.SetActive(false);
                if (Debug.isDebugBuild) BattleTransitionManager.Instance.EnterBattleTestMenu();
                else Util.Crash(new Exception("Attempted to load battle scene without going through BattleSceneManager!"));
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
