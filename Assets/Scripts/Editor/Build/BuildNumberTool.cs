#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using System.IO;

/// <summary>
/// Updates build number before editor play or build.
/// </summary>
public class BuildNumberTool : IPreprocessBuild
{
    public int callbackOrder { get { return 0; } }
    private const string buildNumberPath = "Assets/Resources/buildnumber.txt";
    private static bool built = false;

    /// <summary>
    /// IPreprocessBuild.OnpreprocessBuild (target, path)
    /// </summary>
    public void OnPreprocessBuild(BuildTarget target, string path)
    {
        UpdateBuildNumber();
    }

    /// <summary>
    /// OnPostProcessScene ()
    /// </summary>
    [PostProcessScene]
    public static void OnPostProcessScene ()
    {
        UpdateBuildNumber();
    }

    /// <summary>
    /// Updates the build number on disk.
    /// </summary>
    public static void UpdateBuildNumber()
    {
        if (!built)
        {
            built = true;
            TextAsset buildnumberFile = (TextAsset)AssetDatabase.LoadAssetAtPath(buildNumberPath, typeof(TextAsset));
            string newBuildNumber = (ulong.Parse(buildnumberFile.text) + 1).ToString();
            File.WriteAllText(AssetDatabase.GetAssetPath(buildnumberFile), newBuildNumber);
            EditorUtility.SetDirty(buildnumberFile);
            Debug.Log("Started editor play, build " + Util.buildNumber); // using the build number here also makes sure the internal build number is set so we don't update it twice
        }
    }
}
#endif

