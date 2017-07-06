using UnityEngine;
using CnfBattleSys;

/// <summary>
/// Object created when an AudioEvent is dispatched.
/// One per source per event.
/// If an event is dispatched to multiple sources, each of them will
/// return their own AudioEventHandle; the EventBlockHandle needs
/// to handle those intelligently.
/// </summary>
public class AudioEventHandle : BattleEventHandle
{
    /// <summary>
    /// The AudioEvent that created this handle.
    /// </summary>
    public readonly AudioEvent audioEvent;
    /// <summary>
    /// The ManagedAudioSource that was given a clip based on this.
    /// </summary>
    public readonly ManagedAudioSource managedAudioSource;
    /// <summary>
    /// The AudioEventResolverTable that was used to resolve this event.
    /// </summary>
    public readonly AudioEventResolverTable audioEventResolverTable;
    /// <summary>
    /// The clip the event resolved to.
    /// </summary>
    public readonly AudioClip audioClip;
    public bool waitForMe { get { return (audioEvent.flags & AudioEvent.Flags.WaitForMe) == AudioEvent.Flags.WaitForMe; } }

    public AudioEventHandle (AudioEvent _audioEvent, ManagedAudioSource _managedAudioSource, AudioClip _audioClip)
    {
        audioEvent = _audioEvent;
        managedAudioSource = _managedAudioSource;
        audioEventResolverTable = managedAudioSource.audioEventResolverTable;
        audioClip = _audioClip;
        managedAudioSource.onClipFinish += ClipFinished;
    }

    /// <summary>
    /// Called when the audio clip finishes playing.
    /// </summary>
    private void ClipFinished ()
    {
        managedAudioSource.onClipFinish -= ClipFinished;
        FireOnEventCompleted();
    }
}
