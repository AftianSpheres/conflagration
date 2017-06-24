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
    public Transform fxControllersParent { get; private set; }
    public CapsuleCollider capsuleCollider;
    public Animator animator;
    public SkinnedMeshRenderer skinnedMeshRenderer;
    private Dictionary<AnimEventType, AnimEventResolver> animEventResolverTable;
    public bool loaded { get; private set; }
    public bool loading { get; private set; }
    private AnimatorMetadataContainer animatorMetadataContainer;
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
        fxControllersParent = Util.CreateEmptyChild(transform).transform;
        fxControllersParent.gameObject.name = "FX Controllers";
        animEventResolverTable = new Dictionary<AnimEventType, AnimEventResolver>();
    }

    /// <summary>
    /// MonoBehaviour.Start ()
    /// </summary>
    void Start()
    {
        Timing.RunCoroutine(_Load(), thisTag);    
    }

    /// <summary>
    /// MonoBehaviour.OnDestroy ()
    /// </summary>
    void OnDestroy()
    {
        Timing.KillCoroutines(thisTag);
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
        BattleStage.instance.TiePuppetToBattler(_battler, this);
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
        //DispatchAnimEvent(exitEvent);
    }

    /// <summary>
    /// Returns true if either primary or fallback types of this anim event can be resolved.
    /// </summary>
    /// <param name="animEvent"></param>
    /// <returns></returns>
    public bool AnimEventIsResolveable (AnimEvent animEvent)
    {
        return (animEventResolverTable.ContainsKey(animEvent.animEventType) || animEventResolverTable.ContainsKey(animEvent.fallbackType));
    }

    public void DispatchAnimEvent (AnimEventType aet)
    {
        Util.Crash("this is just to compile");
    }

    /// <summary>
    /// Dispatch the given anim event to the 
    /// </summary>
    public void DispatchAnimEvent(AnimEvent animEvent)
    {
        
        //BattleStage.instance.PrepareAnimEvent(animEventType, battler);
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
        bool r = true; // HasAnimFor(moveEvent);
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
    /// Coroutine: Loads all resources this battler will require.
    /// </summary>
    public IEnumerator<float> _Load()
    {
        if (loaded) yield break;
        loading = true;
        bool operationCompleted = false;
        Action whenLoaderAvailable = () => 
        {
            Timing.RunCoroutine(BattleEventResolverTables.instance._AwaitAudioEventResolverTableLoad(battlerData.audioEventResolverTableType, () => { operationCompleted = true; }));
        };
        Action<BattleAction> loadForBattleAction = (action) =>
        {
            LoadForEventBlock(action.animSkip);
            LoadForEventBlock(action.onStart);
            LoadForEventBlock(action.onConclusion);
            string[] keys = new string[action.subactions.Keys.Count];
            action.subactions.Keys.CopyTo(keys, 0);
            for (int sa = 0; sa < keys.Length; sa++)
            {
                string key = keys[sa];
                for (int f = 0; f < action.subactions[key].effectPackages.Length; f++) LoadForEventBlock(action.subactions[key].effectPackages[f].eventBlock);
                LoadForEventBlock(action.subactions[key].eventBlock);
            }
        };
        Timing.RunCoroutine(BattleEventResolverTables._OnceAvailable(whenLoaderAvailable));
        while (!operationCompleted) yield return 0;
        for (int s = 0; s < battler.stances.Length; s++)
        {
            for (int a = 0; a < battler.stances[s].actionSet.Length; a++)
            {
                loadForBattleAction(battler.stances[s].actionSet[a]);
            }
        }
        for (int a = 0; a < battler.metaStance.actionSet.Length; a++)
        {
            loadForBattleAction(battler.metaStance.actionSet[a]);
        }
        loading = false;
        loaded = true;
    }

    /// <summary>
    /// Crawl through the event block and make any requests you
    /// need to for its contents.
    /// </summary>
    private void LoadForEventBlock (EventBlock eventBlock)
    {
        for (int l = 0; l < eventBlock.layers.Length; l++)
        {
            for (int f = 0; f < eventBlock.layers[l].fxEvents.Length; f++) BattleEventResolverTables.instance.RequestFXLoad(eventBlock.layers[l].fxEvents[f].fxEventType);
        }
    }

    private void BuildAnimEventResolverTable ()
    {
     
    }
}
