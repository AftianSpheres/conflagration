using UnityEngine.SceneManagement;

/// <summary>
/// Path and ring metadata for one scene.
/// </summary>
public struct SceneMetadata
{
    /// <summary>
    /// This is aligned with the build settings scene array and facilitates easy metadata lookup based on build index.
    /// </summary>
    public static readonly SceneMetadata[] array;
    public readonly string path;
    public readonly SceneRing sceneRing;

    static SceneMetadata ()
    {
        array = new SceneMetadata[SceneManager.sceneCountInBuildSettings];
    }

    public SceneMetadata (string _path, SceneRing _sceneRing)
    {
        path = _path;
        sceneRing = _sceneRing;
        int index = SceneUtility.GetBuildIndexByScenePath(ExtendedScene.ConvertPath(path));
        array[index] = this;
    }
}