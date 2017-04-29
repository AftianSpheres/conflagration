using UnityEngine;
using System.Collections;
using CnfBattleSys;

/// <summary>
/// MonoBehaviour that generates battle YI element widgets.
/// </summary>
public class bUI_ElementsGenerator : MonoBehaviour
{
    public GameObject enemyInfoboxPrefab;
    public static bUI_ElementsGenerator instance { get; private set; }

    /// <summary>
    /// MonoBehaviour.Awake()
    /// </summary>
    void Awake()
    {
        instance = this;
    }

    /// <summary>
    /// Generates an enemy infobox and attaches it to the given puppet.
    /// </summary>
    public bUI_EnemyInfobox GetEnemyInfoboxFor (BattlerPuppet puppet)
    {
        bUI_EnemyInfobox infobox = Instantiate(enemyInfoboxPrefab).GetComponent<bUI_EnemyInfobox>();
        infobox.AttachPuppet(puppet);
        return infobox;
    }
}
