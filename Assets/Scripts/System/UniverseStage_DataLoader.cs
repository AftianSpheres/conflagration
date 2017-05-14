using UnityEngine;
using UnityEngine.SceneManagement;
using CnfBattleSys;

/// <summary>
/// Teeny tiny stub that loads in all our datasets when the game starts.
/// </summary>
public class UniverseStage_DataLoader : MonoBehaviour
{
    private static bool _runOnce;
    private readonly static string[] utilScenes = { "LoadingScreenScene" };

    /// <summary>
    /// Implements MonoBehaviour.Awake()
    /// </summary>
    void Awake ()
    {
        if (!_runOnce)
        {
            BattleOverseer.FirstRunSetup();
            for (int i = 0; i < utilScenes.Length; i++) SceneManager.LoadSceneAsync(utilScenes[i], LoadSceneMode.Additive);
            _runOnce = true;
        }
    }
}
