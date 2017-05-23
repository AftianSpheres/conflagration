namespace ExtendedSceneManagement
{
    /// <summary>
    /// Path and ring metadata for one scene.
    /// </summary>
    public struct SceneMetadata
    {
        public readonly int buildIndex;
        public readonly string name;
        public readonly string path;
        public readonly SceneRing sceneRing;

        public SceneMetadata(int _buildIndex, string _name, string _path, int _sceneRing)
        {
            buildIndex = _buildIndex;
            name = _name;
            path = _path;
            sceneRing = (SceneRing)_sceneRing;
        }
    }
}