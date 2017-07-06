using System.Collections.Generic;
using UnityEngine;
using CnfBattleSys;

/// <summary>
/// Metadata for a RuntimeAnimatorController
/// </summary>
public class AnimatorMetadata
{
    /// <summary>
    /// A mapping of AnimEventType cases to sets of animator path hashes.
    /// When resolving an AnimEvent, a clip is chosen at random from
    /// the set provided by the resolver. Typically, the resolver will
    /// actually only have one clip, though!
    /// </summary>
    public struct AnimEventResolver
    {
        public readonly AnimEventType animEventType;
        public readonly int[] animsHashes;

        public AnimEventResolver(AnimEventType _animEventType, int[] _hashes)
        {
            animEventType = _animEventType;
            animsHashes = _hashes;
        }

        /// <summary>
        /// Resolves the event and returns an AnimEventHandle.
        /// </summary>
        public AnimEventHandle ResolveOn(AnimEvent animEvent, BattlerPuppet battlerPuppet)
        {
            int hash = animsHashes[UnityEngine.Random.Range(0, animsHashes.Length)];
            battlerPuppet.animator.Play(hash);
            return new AnimEventHandle(animEvent, battlerPuppet, hash);
        }
    }

    /// <summary>
    /// Container for info we can use to play back a known state
    /// at runtime.
    /// </summary>
    public class StateMetadata
    {
        public readonly int fullPathHash;
        public readonly int nameHash;
        public readonly string fullPath;
        public readonly string name;

        public StateMetadata (string _fullPath, string _name)
        {
            fullPath = _fullPath;
            fullPathHash = Animator.StringToHash(_fullPath);
            name = _name;
            nameHash = Animator.StringToHash(_name);
        }
    }
    private Dictionary<AnimEventType, AnimEventResolver> animEventResolverTable;
    private readonly StateMetadata[][] layeredStateMetadata;

    /// <summary>
    /// Gets metadata for first state with given name, if one exists.
    /// </summary>
    public StateMetadata GetMetadataByStateName (string stateName)
    {
        int hash = Animator.StringToHash(stateName);
        for (int l = 0; l < layeredStateMetadata.Length; l++)
        {
            for (int s = 0; s < layeredStateMetadata[l].Length; s++) if (layeredStateMetadata[l][s].nameHash == hash) return layeredStateMetadata[l][s];
        }
        return null;
    }

    public AnimatorMetadata (StateMetadata[][] _layeredStateMetadata, AnimEventType[] resolveableEvents, int[][] hashSets)
    {
        layeredStateMetadata = _layeredStateMetadata;
        animEventResolverTable = new Dictionary<AnimEventType, AnimEventResolver>(resolveableEvents.Length);
        for (int i = 0; i < resolveableEvents.Length; i++) animEventResolverTable.Add(resolveableEvents[i], new AnimEventResolver(resolveableEvents[i], hashSets[i]));
    }

    /// <summary>
    /// Returns true if either primary or fallback types of this anim event can be resolved.
    /// </summary>
    public bool AnimEventIsResolveable(AnimEvent animEvent)
    {
        return (animEventResolverTable.ContainsKey(animEvent.animEventType) || animEventResolverTable.ContainsKey(animEvent.fallbackType));
    }

    /// <summary>
    /// Dispatch the given anim event to the BattlerPuppet.
    /// </summary>
    public AnimEventHandle DispatchAnimEvent(AnimEvent animEvent, BattlerPuppet puppet)
    {
        if (animEventResolverTable.ContainsKey(animEvent.animEventType)) return animEventResolverTable[animEvent.animEventType].ResolveOn(animEvent, puppet);
        else if (animEventResolverTable.ContainsKey(animEvent.fallbackType)) return animEventResolverTable[animEvent.fallbackType].ResolveOn(animEvent, puppet);
        else if ((animEvent.flags & AnimEvent.Flags.IsMandatory) == AnimEvent.Flags.IsMandatory) Util.Crash(puppet.gameObject.name + " failed to resolve mandatory AnimEvent of type " + animEvent.animEventType + " and fallback type " + animEvent.fallbackType);
        return null;
    }
}
