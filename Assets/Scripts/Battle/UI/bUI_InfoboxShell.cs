using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Synchronizes and associates multiple infoboxes for e.g.
/// expanding/collapsing enemy infoboxes. (Infoboxen?)
/// </summary>
public class bUI_InfoboxShell : MonoBehaviour
{
    /// <summary>
    /// bUI_EnemyInfobox states
    /// </summary>
    public enum State
    {
        None,
        Uninitialized,
        Initialized
    }
    public int index;
    private bUI_BattlerInfobox[] infoboxen;
    private Dictionary<InfoboxType, bUI_BattlerInfobox> infoboxDict;
    public State state { get; private set; }

    /// <summary>
    /// MonoBehaviour.Awake ()
    /// </summary>
	void Awake ()
    {
        infoboxen = GetComponentsInChildren<bUI_BattlerInfobox>();
        infoboxDict = new Dictionary<InfoboxType, bUI_BattlerInfobox>(infoboxen.Length);
        for (int i = 0; i < infoboxen.Length; i++)
        {
            if (infoboxDict.ContainsKey(infoboxen[i].infoboxType)) Util.Crash(gameObject.name + " contains multiple child infoboxes of type " + infoboxen[i].infoboxType);
            else if (infoboxen[i].infoboxType != InfoboxType.None) infoboxDict[infoboxen[i].infoboxType] = infoboxen[i];
        }
    }

    /// <summary>
    /// Attaches puppet to all infoboxes and sets shell state.
    /// </summary>
    public void AttachPuppet (BattlerPuppet puppet)
    {
        state = State.Initialized;
        DoOnInfoboxen((infobox) => { infobox.AttachPuppet(puppet); });
    }

    /// <summary>
    /// Calls the given function on all infoboxes tied to this shell.
    /// </summary>
    public void DoOnInfoboxen (Action<bUI_BattlerInfobox> action)
    {
        for (int i = 0; i < infoboxen.Length; i++) action(infoboxen[i]);
    }

}
