using UnityEngine;
using CnfBattleSys;

/// <summary>
/// Monobehaviour that handles animation events, waits for input where needed, and decides when to advance the battle state.
/// Does not currently exist.
/// </summary>
public class BattleStage : MonoBehaviour
{
    public struct UIEvent
    {
        //public readonly B
    }

    /// <summary>
    /// BattleStage isn't actually a singleton, but it interacts with a lot of static classes on a message-passing basis,
    /// so it's useful for those to be able to address the current instance without being given a reference to a specific
    /// BattleStage. There should never be more than one of these in a scene at a time, anyway.
    /// </summary>
    public static BattleStage instance;

	/// <summary>
    /// MonoBehaviour.Awake
    /// </summary>
	void Awake ()
    {
        instance = this;
	}
	
	/// <summary>
    /// MonoBehaviour.Update
    /// </summary>
	void Update ()
    {
	
	}
}
