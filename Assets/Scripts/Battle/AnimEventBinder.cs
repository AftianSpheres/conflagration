using UnityEngine;
using CnfBattleSys;

/// <summary>
/// StateMachineBehaviour that maps an individual state to
/// one or more AnimEventTypes.
/// Doesn't actually _do_ anything - just here
/// to let the metadata builder tell what's what.
/// </summary>
public class AnimEventBinder : StateMachineBehaviour
{
    public AnimEventType[] animEventTypes;

    /// <summary>
    /// StateMachineBehaviour.OnStateEnter()
    /// </summary>
    public override void OnStateEnter (Animator animator, AnimatorStateInfo animatorStateInfo, int layerIndex)
    {
        Destroy(this); // this is a stub - just stores data for the metadata table to handle
    }
}
