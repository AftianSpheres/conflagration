using UnityEngine;

/// <summary>
/// Component that stores an AnimatorMetadata reference.
/// Automatically attached to gameObjects with an animator by the
/// MetadataPusher.
/// </summary>
public class AnimatorMetadataContainer : MonoBehaviour
{
    public AnimatorMetadata contents { get; private set; }

    /// <summary>
    /// Put the metadata in the container.
    /// </summary>
    public void FillWith (AnimatorMetadata _contents)
    {
        contents = _contents;
    }
}
