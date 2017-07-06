using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CnfBattleSys;

/// <summary>
/// Object created when an FXEvent is dispatched.
/// One per controller per event.
/// If an event is dispatched to controllers, each of them will
/// return their own FXEventHandle; the EventBlockHandle needs
/// to handle those intelligently.
/// </summary>
public class FXEventHandle : BattleEventHandle
{
    /// <summary>
    /// The event that created this handle.
    /// </summary>
    public readonly FXEvent fxEvent;
    /// <summary>
    /// The FX controller this resolved to.
    /// </summary>
    public readonly BattleFXController battleFXController;
    public bool waitForMe { get { return (fxEvent.flags & FXEvent.Flags.WaitForMe) == FXEvent.Flags.WaitForMe; } }

    public FXEventHandle (FXEvent _fxEvent, BattleFXController _battleFXController)
    {
        fxEvent = _fxEvent;
        battleFXController = _battleFXController;
        battleFXController.onCompletion += FireOnEventCompleted;
    }
}
