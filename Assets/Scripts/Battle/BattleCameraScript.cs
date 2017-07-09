using System;
using System.Collections.Generic;
using MovementEffects;

/// <summary>
/// Base class for the scripts that control battle camera during
/// (ex: ) attack animations. Note that it's not "just" attack
/// anims, though - the camera is always "playing" some
/// form of BattleCameraScript. These classes issue
/// commands to the CameraHarness they're attached to
/// and maintain any state that's relevant to
/// "what the camera is doing right now" as opposed
/// to the camera itself.
/// </summary>
public abstract class BattleCameraScript
{
    /// <summary>
    /// Valid states a BattleCameraScript can inhabit.
    /// </summary>
    public enum State
    {
        NotYetStarted,
        Alive,
        Dead
    }

    /// <summary>
    /// Callback that the BattleCameraScript should
    /// run upon finishing.
    /// </summary>
    protected Action callback;
    /// <summary>
    /// Current state of this BattleCameraScript.
    /// </summary>
    protected State state;
    /// <summary>
    /// The current battle camera harness in use.
    /// </summary>
    protected readonly bUI_CameraHarness cameraHarness;
    /// <summary>
    /// If true, this battle camera script should never be expected
    /// to end on its own. If false, it will eventually reach a "final"
    /// state, at which point it will change state to Dead.
    /// </summary>
    public bool isIndefiniteDuration { get; protected set; }

    /// <summary>
    /// Coroutine: This runs while the BattleCameraScript is
    /// "alive." This needs to manage the stage of the BattleCameraScript,
    /// accept any inputs it needs to, and either finish or loop
    /// when it reaches its end.
    /// _CameraSequence is abstract, and must be implemented by
    /// each BattleCameraScript. Since the precise behavior of
    /// "a camera script" is arbitrary, this is basically
    /// bespoke for each derived class.
    /// </summary>
    protected abstract IEnumerator<float> _CameraSequence ();
    /// <summary>
    /// Tag what we run coroutines associated with this instance with.
    /// </summary>
    protected string thisTag;
    /// <summary>
    /// Ulong. Incremented with each BattleCameraScript created, allowing for creation of unique tags.
    /// </summary>
    private static ulong instanceCtr;

    public BattleCameraScript ()
    {
        cameraHarness = bUI_BattleUIController.instance.cameraHarness;
        thisTag = "BCS_" + instanceCtr.ToString();
        instanceCtr++;
    }

    /// <summary>
    /// Method called to force this BattleCameraScript
    /// to finish what it's doing. Always call this
    /// before throwing a BattleCameraScript child
    /// instance away - even if it's "over."
    /// </summary>
    public void End ()
    {
        InEnd();
        callback();
    }

    /// <summary>
    /// Implements behaviors specific to a given
    /// BattleCameraScript derived class that are
    /// called during BattleCameraScript.End().
    /// </summary>
    protected abstract void InEnd();

    /// <summary>
    /// Method called when the BattleCameraScript
    /// finishes, either because it's reached an end
    /// point or because End () has been called.
    /// Fires off the callback (if any) and does
    /// cleanup.
    /// </summary>
    protected abstract void PrepareForEnd ();

    /// <summary>
    /// Method called to begin processing this
    /// BattleCameraScript.
    /// callback will be invoked when the
    /// BattleCameraScript is finally finished.
    /// </summary>
    public void Start (Action _callback)
    {
        callback = _callback;
        InStart();
        Timing.RunCoroutine(_CameraSequence(), thisTag);
    }

    /// <summary>
    /// Implements behaviors specific to a given
    /// BattleCameraScript derived class that are
    /// called during BattleCameraScript.Start().
    /// </summary>
    public abstract void InStart();
}
