#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;

/// <summary>
/// Updates build number before build.
/// </summary>
public class BuildStage_BuildNumber : IPreprocessBuild
{
    public int callbackOrder { get { return 0; } }

    public void OnPreprocessBuild (BuildTarget target, string path)
    {
        BuildNumberTool.UpdateBuildNumber();
    }
}
#endif

