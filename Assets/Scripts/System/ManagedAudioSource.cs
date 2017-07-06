using System;
using System.Collections.Generic;
using UnityEngine;
using CnfBattleSys;
using MovementEffects;

/// <summary>
/// Helper MonoBehaviour that attaches itself to an AudioSource
/// and adjusts its volume based on the specified audioSourceType
/// and the value for that type in AudioSettingsManager.
/// </summary>
public class ManagedAudioSource : MonoBehaviour
{
    public event Action onClipFinish;
    public AudioSourceType audioSourceType;
    public AudioEventResolverTable audioEventResolverTable { get; private set; }
    private AudioSource source;
    private float baseVolume;
    private string thisTag;

    /// <summary>
    /// MonoBehaviour.Awake ()
    /// </summary>
    void Awake ()
    {
        thisTag = GetInstanceID().ToString();
        source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();
        baseVolume = source.volume;
    }

    /// <summary>
    /// MonoBehaviour.Start ()
    /// </summary>
    void Start ()
    {
        Action onceOnline = () =>
        {
            AudioSettingsManager.Instance.RegisterManagedAudioSource(this);
            ConformToManager();
        };
        AudioSettingsManager.DoOnceOnline(onceOnline);
    }

    /// <summary>
    /// Conforms volume of audio source to manager settings.
    /// </summary>
    public void ConformToManager ()
    {
        float volume = 1;
        switch (audioSourceType)
        {
            case AudioSourceType.Music_BGM:
                volume = AudioSettingsManager.Instance.volumeMod_Music_BGM;
                break;
            case AudioSourceType.Music_FX:
                volume = AudioSettingsManager.Instance.volumeMod_Music_FX;
                break;
            case AudioSourceType.Sound_FX:
                volume = AudioSettingsManager.Instance.volumeMod_Sound_FX;
                break;
            case AudioSourceType.Sound_Ambient:
                volume = AudioSettingsManager.Instance.volumeMod_Sound_Ambient;
                break;
            case AudioSourceType.Voice:
                volume = AudioSettingsManager.Instance.volumeMod_Voice;
                break;
            default:
                Util.Crash("Bad audioSourceType in ConformToManager: " + audioSourceType);
                break;
        }
        source.volume = baseVolume * volume;
    }

    /// <summary>
    /// Sets the type of this managedAudioSource to clipType, conform,
    /// and play the given clip.
    /// </summary>
    public void PlayClipAs (AudioClip clip, AudioSourceType clipType)
    {
        audioSourceType = clipType;
        ConformToManager();
        source.clip = clip;
        source.Play();
        Timing.RunCoroutine(_CallOnceClipFinished(onClipFinish), thisTag);
    }

    /// <summary>
    /// Acquire a reference to a loaded AudioEventResolverTable.
    /// If the table isn't loaded Bad Things will happen, so make
    /// sure you don't call this until it's loaded!
    /// </summary>
    public void AcquireAudioEventResolverTable (AudioEventResolverTableType tableType)
    {
        audioEventResolverTable = BattleEventResolverTablesLoader.instance.GetTable(tableType);
    }

    /// <summary>
    /// Dispatches an AudioEvent to this ManagedAudioSource.
    /// Returns an AudioEventHandle.
    /// </summary>
    public AudioEventHandle DispatchAudioEvent (AudioEvent audioEvent)
    {
        audioEventResolverTable.Resolve(audioEvent, this);
        return new AudioEventHandle(audioEvent, this, source.clip);
    }

    /// <summary>
    /// Coroutine: Execute callback once clip has finished playing.
    /// </summary>
    private IEnumerator<float> _CallOnceClipFinished (Action callback)
    {
        while (source.isPlaying) yield return 0;
        callback();
    }
}
