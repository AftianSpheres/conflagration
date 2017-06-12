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
        Uncollapsed,
        Collapsed
    }
    public int index;
    private bUI_BattlerInfobox[] infoboxen;
    private Dictionary<InfoboxType, bUI_BattlerInfobox> infoboxDict;
    public State state { get; private set; }
    public bool collapsible { get { return infoboxDict.ContainsKey(InfoboxType.NPC_Collapsed) && infoboxDict.ContainsKey(InfoboxType.NPC_Uncollapsed); } }

    /// <summary>
    /// MonoBehaviour.Awake ()
    /// </summary>
    void Awake ()
    {
        state = State.Uninitialized;
        infoboxen = GetComponentsInChildren<bUI_BattlerInfobox>();
        infoboxDict = new Dictionary<InfoboxType, bUI_BattlerInfobox>(infoboxen.Length);
        for (int i = 0; i < infoboxen.Length; i++)
        {
            if (infoboxDict.ContainsKey(infoboxen[i].infoboxType)) Util.Crash(gameObject.name + " contains multiple child infoboxes of type " + infoboxen[i].infoboxType);
            else if (infoboxen[i].infoboxType != InfoboxType.None) infoboxDict[infoboxen[i].infoboxType] = infoboxen[i];
        }
        if (collapsible) Collapse();
    }

    /// <summary>
    /// Attaches puppet to all infoboxes and sets shell state.
    /// </summary>
    public void AttachPuppet (BattlerPuppet puppet)
    {
        state = State.Uncollapsed;
        DoOnInfoboxen((infobox) => { infobox.AttachPuppet(puppet); });
    }

    /// <summary>
    /// Display the collapsed-state infobox.
    /// </summary>
    public void Collapse ()
    {
        state = State.Collapsed;
        if (infoboxDict.ContainsKey(InfoboxType.NPC_Collapsed)) infoboxDict[InfoboxType.NPC_Collapsed].gameObject.SetActive(true);
        if (infoboxDict.ContainsKey(InfoboxType.NPC_Uncollapsed)) infoboxDict[InfoboxType.NPC_Uncollapsed].gameObject.SetActive(false);
    }

    /// <summary>
    /// Calls the given function on all infoboxes tied to this shell.
    /// </summary>
    public void DoOnInfoboxen (Action<bUI_BattlerInfobox> action)
    {
        for (int i = 0; i < infoboxen.Length; i++) action(infoboxen[i]);
    }

    /// <summary>
    /// Display the uncollapsed-state infobox.
    /// </summary>
    public void Uncollapse ()
    {
        state = State.Uncollapsed;
        if (infoboxDict.ContainsKey(InfoboxType.NPC_Collapsed)) infoboxDict[InfoboxType.NPC_Collapsed].gameObject.SetActive(false);
        if (infoboxDict.ContainsKey(InfoboxType.NPC_Uncollapsed)) infoboxDict[InfoboxType.NPC_Uncollapsed].gameObject.SetActive(true);
    }
}
