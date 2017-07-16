using System;
using UnityEngine;
using ExtendedAnimationManagement;

/// <summary>
/// Component that stores an AnimatorMetadata reference.
/// Automatically attached to gameObjects with an animator by the
/// MetadataPusher.
/// This also keeps a reference to the StateMachineExtender in
/// order to allow other components to subscribe to its events.
/// </summary>
public class AnimatorMetadataContainer : MonoBehaviour
{
    public event Action onceFilled;
    public AnimatorMetadata contents { get; private set; }
    public StateMachineExtender stateMachineExtender { get; private set; }

    /// <summary>
    /// Put the metadata in the container.
    /// </summary>
    public void FillWith (AnimatorMetadata _contents, StateMachineExtender _stateMachineExtender)
    {
        contents = _contents;
        stateMachineExtender = _stateMachineExtender;
        onceFilled?.Invoke();
        onceFilled = null;
    }
}
