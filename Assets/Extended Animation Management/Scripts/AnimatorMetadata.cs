using UnityEngine;

/// <summary>
/// Metadata for a RuntimeAnimatorController
/// </summary>
public class AnimatorMetadata
{
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

    public AnimatorMetadata (StateMetadata[][] _layeredStateMetadata)
    {
        layeredStateMetadata = _layeredStateMetadata;
    }
}
