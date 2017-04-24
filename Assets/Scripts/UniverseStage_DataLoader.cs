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
            BattleOverseer.FirstRunSetup();
            _runOnce = true;
        }
        else
        {
            Debug.Log("UniverseStage_DataLoader tried to run, but it already had???");
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A)) Debug.Log(TextBankManager.Instance.GetCommonTextBank(typeof(BattlerType)).GetPage(BattlerType.TestAIUnit).text);
    }
}
