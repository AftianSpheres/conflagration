﻿using UnityEngine;
using System;
using System.Collections.Generic;
using MovementEffects;
using CnfBattleSys;

/// <summary>
/// MonoBehaviour side of a battler.
/// Doesn't do any battle-logic things - just gets messages from the Battler
/// and handles gameObject movement/model/animations/etc.
/// </summary>
public class BattlerPuppet : MonoBehaviour
{
    public Battler battler { get; private set; }
    public bUI_InfoboxShell infoboxShell { get; private set; }
    public CapsuleCollider capsuleCollider;
    public Animator animator;
    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;
    private BattlerModelType modelType;
    private Vector3 offset;
    private float stepTime;
    private LinkedList<Action> StatusPacketsModified;

    /// <summary>
    /// MonoBehaviour.Awake ()
    /// </summary>
    void Awake ()
    {
        StatusPacketsModified = new LinkedList<Action>();
    }

    /// <summary>
    /// Adds the action to the linked list of functions that will
    /// be called when status packets are touched.
    /// </summary>
    public void AddToOnStatusPacketsModified(Action action)
    {
        StatusPacketsModified.AddLast(action);
    }

    /// <summary>
    /// Attaches this puppet to the specified Battler.
    /// </summary>
    public void AttachBattler (Battler _battler)
    {
        battler = _battler;
        battler.GivePuppet(this);
        SyncPosition();
        gameObject.name = "BattlerPuppet " + _battler.battlerType.ToString() + ": " + _battler.index;
    }

    /// <summary>
    /// Associate puppet with infobox shell.
    /// </summary>
    public void AttachInfoboxShell (bUI_InfoboxShell _infoboxShell)
    {
        infoboxShell = _infoboxShell;
        infoboxShell.AttachPuppet(this);
    }

    /// <summary>
    /// Fired off when we do anything that touches StatusPackets.
    /// </summary>
    public void OnStatusPacketsModified()
    {
        LinkedListNode<Action> node = StatusPacketsModified.First;
        while (true)
        {
            node.Value();
            if (node.Next != null) node = node.Next;
            else break;
        }
    }

    /// <summary>
    /// Removes the action from the linked list of functions that will
    /// be called when status packets are touched.
    /// </summary>
    public void RemoveFromOnStatusPacketsModified(Action action)
    {
        StatusPacketsModified.Remove(action);
    }

    /// <summary>
    /// Coroutine: lerps unit between original and final positions.
    /// speed is a float that - like stepTime - represents the amount of time it should take to move one unit.
    /// Normally speed = stepTime, but we might wanna eg. throw units around at speeds independent of their
    /// stepTime values.
    /// </summary>
    private IEnumerator<float> _Move(Vector3 moveVector, float speed, AnimEventType exitEvent)
    {
        float vd = 0;
        float distance = moveVector.magnitude;
        Vector3 op = transform.position;
        Vector3 np = transform.position + moveVector;
        while (vd < distance)
        {
            transform.position = Vector3.Lerp(op, np, vd / distance);
            vd += (1 / speed) * Timing.DeltaTime * distance;
            yield return 0;
        }
        SyncPosition();
        DispatchAnimEvent(exitEvent);
    }

    /// <summary>
    /// Stub.
    /// TO-DO: This returns true if the animator has an animation of the same name as the animEventType.
    /// </summary>
    private bool HasAnimFor (AnimEventType animEventType)
    {
        return true;
    }

    /// <summary>
    /// Stubbed. This just forwards the animEvent straight to the BattleStage right now.
    /// </summary>
    public void DispatchAnimEvent(AnimEventType animEventType)
    {
        BattleStage.instance.PrepareAnimEvent(animEventType, battler);
    }

    /// <summary>
    /// Processes the given battler UI event.
    /// </summary>
    public void DispatchBattlerUIEvent (BattlerUIEventType battlerUIEventType)
    {
        switch (battlerUIEventType)
        {
            case BattlerUIEventType.HPValueChange:
                if (infoboxShell != null) infoboxShell.DoOnInfoboxen((infobox) => { infobox.HandleHPValueChanges(); });
                break;
            case BattlerUIEventType.StaminaValueChange:
                if (infoboxShell != null) infoboxShell.DoOnInfoboxen((infobox) => { infobox.HandleStaminaValueChanges(); });
                break;
            case BattlerUIEventType.StanceChange:
                if (infoboxShell != null) infoboxShell.DoOnInfoboxen((infobox) => { infobox.DisplayStanceName(); });
                break;
            default:
                Util.Crash(new Exception("Bad battler UI event: " + battlerUIEventType.ToString()));
                break;
        }
    }

    /// <summary>
    /// Processes move command. If we have an animation for moveEvent we move gradually across the playfield playing that animation;
    /// otherwise, we immediately SyncPosition().
    /// Plays exitEvent after the move event ends. Normally this is AnimEventType.Idle.
    /// Returns true if we were able to process the moveEvent.
    /// </summary>
    public bool ProcessMove(Vector3 moveVector, float speed, AnimEventType moveEvent, AnimEventType exitEvent)
    {
        bool r = HasAnimFor(moveEvent);
        if (r == true)
        {
            Timing.RunCoroutine(_Move(moveVector, speed, exitEvent));
        }
        else
        {
            SyncPosition();
        }
        return r;
    }

    /// <summary>
    /// Updates position of GameObject w/ battler's logical position
    /// </summary>
    private void SyncPosition()
    {
        transform.position = battler.logicalPosition + offset;
    }

    /// <summary>
    /// Loads our model from the Resources folder, based on the modelType
    /// </summary>
    private void LoadModel()
    {
        const string bmPath = "Battle/Models/";
        const string meshPath = "/mesh";
        string myPath = modelType.ToString();
        Mesh mesh = Resources.Load<Mesh>(bmPath + myPath + meshPath);
        if (mesh == null) Util.Crash(new Exception("Couldn't load battle model mesh: " + myPath + meshPath));
        meshFilter.mesh = mesh;
    }
}
