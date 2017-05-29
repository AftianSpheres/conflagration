using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Universe;

/// <summary>
/// Manager that handles global audio settings and whatnot.
/// </summary>
public class AudioSettingsManager : Manager<AudioSettingsManager>
{
    public float volumeMod_Music_BGM { get; private set; }
    public float volumeMod_Music_FX { get; private set; }
    public float volumeMod_Sound_Ambient { get; private set; }
    public float volumeMod_Sound_FX { get; private set; }
    public float volumeMod_Voice { get; private set; }
    private List<ManagedAudioSource> _Music_BGM;
    private List<ManagedAudioSource> _Music_FX;
    private List<ManagedAudioSource> _Sound_Ambient;
    private List<ManagedAudioSource> _Sound_FX;
    private List<ManagedAudioSource> _Voice;

    /// <summary>
    /// MonoBehaviour.Awake ()
    /// </summary>
    void Awake ()
    {
        volumeMod_Music_BGM = 1;
        volumeMod_Music_FX = 1;
        volumeMod_Sound_Ambient = 1;
        volumeMod_Sound_FX = 1;
        volumeMod_Voice = 1;
        _Music_BGM = new List<ManagedAudioSource>(32);
        _Music_FX = new List<ManagedAudioSource>(128);
        _Sound_Ambient = new List<ManagedAudioSource>(32);
        _Sound_FX = new List<ManagedAudioSource>(128);
        _Voice = new List<ManagedAudioSource>(128);
    }

    /// <summary>
    /// Change the volume modifier associated with ManagedAudioSources of the given source type,
    /// and update any in the loaded scenes to use the new value.
    /// This is somewhat expensive, but because the AudioSettingsManager does the heavy lifting,
    /// we avoid having an Update() method on ManagedAudioSource, and can cut out a lot of unnecessary
    /// Update() calls in favor of having a little more work to do in the much rarer event of a settings
    /// change.
    /// </summary>
    public void ChangedVolumeMod(float newValue, AudioSourceType audioSourceType)
    {
        switch (audioSourceType)
        {
            case AudioSourceType.Music_BGM:
                volumeMod_Music_BGM = newValue;
                DoOnAllInList(_Music_BGM, (managedAudioSource) => managedAudioSource.ConformToManager());
                break;
            case AudioSourceType.Music_FX:
                volumeMod_Music_FX = newValue;
                DoOnAllInList(_Music_FX, (managedAudioSource) => managedAudioSource.ConformToManager());
                break;
            case AudioSourceType.Sound_Ambient:
                volumeMod_Sound_Ambient = newValue;
                DoOnAllInList(_Sound_Ambient, (managedAudioSource) => managedAudioSource.ConformToManager());
                break;
            case AudioSourceType.Sound_FX:
                volumeMod_Sound_FX = newValue;
                DoOnAllInList(_Sound_FX, (managedAudioSource) => managedAudioSource.ConformToManager());
                break;
            case AudioSourceType.Voice:
                volumeMod_Voice = newValue;
                DoOnAllInList(_Voice, (managedAudioSource) => managedAudioSource.ConformToManager());
                break;
        }
    }

    /// <summary>
    /// Adds the managedAudioSource to the list corresponding to its audioSourceType.
    /// </summary>
    public void RegisterManagedAudioSource (ManagedAudioSource managedAudioSource)
    {
        switch (managedAudioSource.audioSourceType)
        {
            case AudioSourceType.Music_BGM:
                _Music_BGM.Add(managedAudioSource);
                break;
            case AudioSourceType.Music_FX:
                _Music_FX.Add(managedAudioSource);
                break;
            case AudioSourceType.Sound_Ambient:
                _Sound_Ambient.Add(managedAudioSource);
                break;
            case AudioSourceType.Sound_FX:
                _Sound_FX.Add(managedAudioSource);
                break;
            case AudioSourceType.Voice:
                _Voice.Add(managedAudioSource);
                break;
            default:
                Util.Crash(managedAudioSource, this, gameObject);
                break;
        }
    }

    /// <summary>
    /// Performs action on all ManagedAudioSource instances on list,
    /// or removes null references.
    /// </summary>
    private void DoOnAllInList (List<ManagedAudioSource> list, Action<ManagedAudioSource> action)
    {
        foreach(ManagedAudioSource source in list)
        {
            if (source == null) list.Remove(source);
            else action(source);
        }
    }
}
