using UnityEngine;
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
    private struct AnimEventResolver
    {
        public readonly AnimEventType animEventType;
        public readonly int[] animsHashes;

        public AnimEventResolver (AnimEventType _animEventType, string[] anims)
        {
            animEventType = _animEventType;
            int[] hashes = new int[anims.Length];
            for (int i = 0; i < anims.Length; i++) hashes[i] = Animator.StringToHash(anims[i]);
            animsHashes = hashes;
        }
    }

    public Battler battler { get; private set; }
    public BattlerData battlerData { get; private set; }
    public bUI_InfoboxShell infoboxShell { get; private set; }
    public ManagedAudioSource managedAudioSource { get; private set; }
    public AudioEventResolverTable audioEventResolverTable { get; private set; }
    public CapsuleCollider capsuleCollider;
    public Animator animator;
    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;
    public bool loading { get; private set; }
    private Vector3 offset;
    private float stepTime;
    private LinkedList<Action> StatusPacketsModified;
    private string thisTag;

    /// <summary>
    /// MonoBehaviour.Awake ()
    /// </summary>
    void Awake ()
    {
        thisTag = GetInstanceID().ToString();
        StatusPacketsModified = new LinkedList<Action>();
        managedAudioSource = GetComponent<ManagedAudioSource>();
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
    ///
    /// </summary>
    private IEnumerator<float> _Load()
    {
        // ACTUALLY YOU KNOW WHAT MAKES A HELL OF A LOT MORE SENSE
        // BattlerPuppet should be an abstract class.
        // When you generate a puppet for a battler,
        // you consult a big-ass lookup table to identify
        // which BattlerPuppet class it is?
        // idk...
        // there def. needs to be some way to resolve sfx types anyhow.
        // ie. "genericHitSfx" is handled by the puppet and resolved to "randomly select thisUnitHitSfx1 or "thisUnitHitSfx2"
        // so for the metadata table: there's a list of animation and fx events that this puppet can resolve,
        // and then information on how it resolves those. ie. "animEvent.Foo" > "foo", "foo2" has you call one of those two anims when given animEvent.Foo
        // also let's split anim events up into "field" anims and "battler" anims
        // so it's StageAnimEvent, BattlerAnimEvent, StageFXEvent, BattlerFXEvent, StageSFXEvent, BattlerSFXEvent?
        // where a stage anim or w/e can call events on battlers...
        // for fx: all you do is instantiate a prefab w/ a BattleFXController-derived monobehaviour on that and wait until it destroys itself.
        // actually, nah - instantiate all the fx prefabs as children of either the fxparent (for the stage) or the individual puppets - use
        // fxcontroller interfaces to "start" fx (normalize state and start executing animations and shit) and wait until it's finished (at
        // which point the controller automatically hides the object again
        // destroying these would be costly
        // stage anims don't really exist actually, do they?
        // stages don't animate.
        // and fx controllers should be able to call anims on their own, so
        // it's just stagefx/battlerfx/battleranim
        loading = true;
        bool operationCompleted = false;
        Action whenLoaderAvailable = () => { Timing.RunCoroutine(TableLoader.instance._AwaitTableLoad(battlerData.audioEventResolverTableType, () => { operationCompleted = true; })); };
        Timing.RunCoroutine(TableLoader._OnceAvailable(whenLoaderAvailable));
        while (!operationCompleted) yield return 0;
        for (int s = 0; s < battler.stances.Length; s++)
        {
            for (int a = 0; a < battler.stances[s].actionSet.Length; a++)
            {
                 
                // get effects and shit
            }
        }
        for (int a = 0; a < battler.metaStance.actionSet.Length; a++)
        {
            // don't forget about this
            // actually there should be an AttackAnimPlayer behaviour and we should just pass action IDs
            // into it, and it should batch all of the resources it'll need
            // so this is just bUI_AttackAnimEngine.LoadIn(battler.metaStance.actionSet[a]);
        }
        // load model corresponding to modelID 
        // and a package of sound effects and things
        // + visual effects for all your attack anims and shit...
        // load animator controller for that model
        // animator data to animevent...
    }
}
