#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

/// <summary>
/// Updates build number before editor play or build.
/// </summary>
public static class BuildNumberTool
{
    private const string buildNumberPath = "Assets/Resources/buildnumber.txt";

    /// <summary>
    /// A somewhat hacky means of automatically updating the build number when we enter play mode.
    /// </summary>
    [PostProcessScene]
    public static void OnPostProcessScene ()
    {
        if (Util.internalBuildNumber == 0)
        {
            UpdateBuildNumber();
            Debug.Log("Started editor play, build " + Util.buildNumber); // using the build number here also makes sure the internal build number is set so we don't update it twice
        }
    }

    /// <summary>
    /// Updates the build number on disk.
    /// </summary>
    public static void UpdateBuildNumber()
    {
        TextAsset buildnumberFile = (TextAsset)AssetDatabase.LoadAssetAtPath(buildNumberPath, typeof(TextAsset));
        string newBuildNumber = (ulong.Parse(buildnumberFile.text) + 1).ToString();
        File.WriteAllText(AssetDatabase.GetAssetPath(buildnumberFile), newBuildNumber);
        EditorUtility.SetDirty(buildnumberFile);
    }
}
#endif

