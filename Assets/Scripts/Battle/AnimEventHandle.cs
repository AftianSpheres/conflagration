using CnfBattleSys;

/// <summary>
/// Object created when an AnimEvent is dispatched.
/// One per puppet per event.
/// If an event is dispatched to multiple puppets, each of them will
/// return their own AnimEventHandle; the EventBlockHandle needs
/// to handle those intelligently.
/// </summary>
public class AnimEventHandle : BattleEventHandle
{
    /// <summary>
    /// The AnimEvent that created this handle.
    /// </summary>
    public readonly AnimEvent animEvent;
    /// <summary>
    /// The BattlerPuppet that resolved the AnimEvent.
    /// </summary>
    public readonly BattlerPuppet battlerPuppet;
    /// <summary>
    /// The full path of the animator state that this event resolved to.
    /// </summary>
    public readonly int fullPathHash;
    /// <summary>
    /// Returns true if the AnimEvent wants us to wait until it finishes before
    /// moving onto the next layer.
    /// </summary>
    public bool waitForMe { get { return (animEvent.flags & AnimEvent.Flags.WaitForMe) == AnimEvent.Flags.WaitForMe; } }

    public AnimEventHandle (AnimEvent _animEvent, BattlerPuppet _battlerPuppet, int _fullPathHash)
    {
        animEvent = _animEvent;
        battlerPuppet = _battlerPuppet;
        fullPathHash = _fullPathHash;
        battlerPuppet.onAnimatorStateChanged += CheckIfDone;
    }

    /// <summary>
    /// Check if the clip that the AnimEvent resolved to has finished.
    /// If true, detach from the battlerPuppet and call onEventCompleted.
    /// </summary>
    private void CheckIfDone ()
    {
        if (battlerPuppet.animator.GetCurrentAnimatorStateInfo(0).fullPathHash != fullPathHash)
        {
            battlerPuppet.onAnimatorStateChanged -= CheckIfDone;
            FireOnEventCompleted();
        }
    }
}
