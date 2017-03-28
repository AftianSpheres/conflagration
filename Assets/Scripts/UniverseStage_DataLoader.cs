using UnityEngine;
using CnfBattleSys;

/// <summary>
/// Teeny tiny stub that loads in all our datasets when the game starts.
/// </summary>
public class UniverseStage_DataLoader : MonoBehaviour
{
    private static bool _runOnce;

    /// <summary>
    /// Implements MonoBehaviour.Awake()
    /// </summary>
    void Start ()
    {
        if (!_runOnce)
        {
            ActionDatabase.Load();
            StanceDatabase.Load(); // stances reference actions
            BattlerDatabase.Load(); // battlers reference stances
            _runOnce = true;
        }
        else
        {
            Debug.Log("UniverseStage_DataLoader tried to run, but it already had???");
        }
    }
}
