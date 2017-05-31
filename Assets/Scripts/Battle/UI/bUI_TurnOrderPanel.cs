using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CnfBattleSys;

/// <summary>
/// One of the panels the bUI_TurnOrderArea controls.
/// </summary>
public class bUI_TurnOrderPanel : MonoBehaviour
{
    private Animator animator;
    private bUI_TurnOrderArea turnOrderArea;

    /// <summary>
    /// MonoBehaviour.Awake ()
    /// </summary>
    void Start ()
    {
        animator = GetComponent<Animator>();
    }

    public void PairWithBattler (Battler battler)
    {

    }
}
