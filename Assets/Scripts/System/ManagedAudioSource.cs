using System;
using UnityEngine;

/// <summary>
/// Helper MonoBehaviour that attaches itself to an AudioSource
/// and adjusts its volume based on the specified audioSourceType
/// and the value for that type in AudioSettingsManager.
/// </summary>
public class ManagedAudioSource : MonoBehaviour
{
    public AudioSourceType audioSourceType;
    private AudioSource source;
    private float baseVolume;

    /// <summary>
    /// MonoBehaviour.Awake ()
    /// </summary>
    void Awake ()
    {
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
    public void PlayOneShotAs (AudioClip clip, AudioSourceType clipType)
    {
        audioSourceType = clipType;
        ConformToManager();
        source.PlayOneShot(clip);
    }
}
