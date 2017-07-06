using System;

/// <summary>
/// Common functionality and interfaces for handles
/// that track the progress of AnimEvents, etc.
/// </summary>
public abstract class BattleEventHandle
{
    public bool isRunning { get; private set; }
    public event Action onEventCompleted;

    protected internal BattleEventHandle ()
    {
        isRunning = true;
    }

    /// <summary>
    /// Used by child classes to fire the onEventCompleted action.
    /// </summary>
    protected internal void FireOnEventCompleted ()
    {
        isRunning = false;
        onEventCompleted();
    }

}