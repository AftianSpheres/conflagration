using UnityEngine;
using System.Collections.Generic;

namespace CnfBattleSys
{
    /// <summary>
    /// A table of resolveable audio events.
    /// </summary>
    public class AudioEventResolverTable
    {
        /// <summary>
        /// Data structure defining an audio event this puppet can resolve
        /// and what it resolves to.
        /// </summary>
        private struct AudioEventResolver
        {
            public readonly AudioEventType audioEventType;
            public readonly AudioClip[] clips;

            public AudioEventResolver(AudioEventType _audioEventType, AudioClip[] _clips)
            {
                audioEventType = _audioEventType;
                clips = _clips;
            }
        }

        public readonly AudioEventResolverTableType audioEventResolverTableType;
        private readonly Dictionary<AudioEventType, AudioEventResolver> audioEventResolvers;

        public AudioEventResolverTable (AudioEventType[] _audioEventTypes, AudioClip[][] _clipSets)
        {
            audioEventResolvers = new Dictionary<AudioEventType, AudioEventResolver>(_audioEventTypes.Length);
            for (int i = 0; i < _audioEventTypes.Length; i++) audioEventResolvers[_audioEventTypes[i]] = new AudioEventResolver(_audioEventTypes[i], _clipSets[i]);
        }

        /// <summary>
        /// Returns true if this puppet can resolve the given audioEvent.
        /// </summary>
        public bool CanResolve(AudioEventType audioEventType)
        {
            return audioEventResolvers.ContainsKey(audioEventType);
        }

        /// <summary>
        /// Resolves the given audio event and play on the given managed audio source.
        /// Returns true if we managed to resolve for either primary or fallback types. 
        /// (i.e. "a sound was played")
        /// </summary>
        public bool Resolve(AudioEvent audioEvent, ManagedAudioSource managedAudioSource)
        {
            if (CanResolve(audioEvent.type))
            {
                AudioEventResolver resolver = audioEventResolvers[audioEvent.type];
                managedAudioSource.PlayClipAs(resolver.clips[Random.Range(0, resolver.clips.Length)], audioEvent.clipType);
                return true;
            }
            else if (CanResolve(audioEvent.fallbackType))
            {
                AudioEventResolver resolver = audioEventResolvers[audioEvent.fallbackType];
                managedAudioSource.PlayClipAs(resolver.clips[Random.Range(0, resolver.clips.Length)], audioEvent.clipType);
                return true;
            }
            else if ((audioEvent.flags & AudioEvent.Flags.IsMandatory) == AudioEvent.Flags.IsMandatory) Util.Crash("Table" + audioEventResolverTableType + " can't resolve audio event type of main " + audioEvent.type + " or fallback " + audioEvent.fallbackType);
            return false;
        }
    }
}