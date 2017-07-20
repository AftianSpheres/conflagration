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
    /// <summary>
    /// States the puppet can be in,
    /// indicating which phase of the load process this is.
    /// </summary>
    public enum LoadingState
    {
        NoLoadStarted,
        Loading,
        LoadCompleted
    }
    /// <summary>
    /// Event that fires off when the attached animator
    /// changes animator states.
    /// </summary>
    public event Action onAnimatorStateChanged;
    /// <summary>
    /// Event that fires off when the associated Battler
    /// updates its status packets in some way.
    /// </summary>
    public event Action onStatusPacketsChanged;

    public Battler battler { get; private set; }
    public BattlerData battlerData { get; private set; }
    public BattleFXContainer battleFXContainer { get; private set; }
    public bUI_InfoboxShell infoboxShell { get; private set; }
    public ManagedAudioSource managedAudioSource { get; private set; }
    public Transform fxControllersParent { get; private set; }
    public CapsuleCollider capsuleCollider;
    public Animator animator;
    public SkinnedMeshRenderer skinnedMeshRenderer;
    public LoadingState loadingState { get; private set; }
    private AnimatorMetadataContainer animatorMetadataContainer;
    private Vector3 logicalPositionOffset;
    private Vector3 originalScale;
    private float stepTime;
    private string thisTag;
    /// <summary>
    /// This is the last rotation that we determined for this puppet.
    /// currentRotation is recalculated each time we move or select a target,
    /// and ensures the puppet "looks" at what it needs to look at.
    /// </summary>
    private Quaternion currentRotation;

    /// <summary>
    /// MonoBehaviour.Awake ()
    /// </summary>
    void Awake ()
    {
        thisTag = GetInstanceID().ToString();
        managedAudioSource = GetComponent<ManagedAudioSource>();
        fxControllersParent = Util.CreateEmptyChild(transform).transform;
        fxControllersParent.gameObject.name = "FX Controllers";
        originalScale = transform.localScale;
    }

    /// <summary>
    /// MonoBehaviour.Start ()
    /// </summary>
    void Start ()
    {
        animatorMetadataContainer = GetComponent<AnimatorMetadataContainer>();
        if (animatorMetadataContainer == null) animatorMetadataContainer = gameObject.AddComponent<AnimatorMetadataContainer>();
        Action whenSMEAvailable = () =>
        {
            animatorMetadataContainer.stateMachineExtender.onStateChanged += onAnimatorStateChanged;
            Timing.RunCoroutine(_Load(), thisTag);
        };
        if (animatorMetadataContainer.stateMachineExtender != null) whenSMEAvailable();
        else animatorMetadataContainer.onceFilled += whenSMEAvailable;        
    }

    /// <summary>
    /// MonoBehaviour.OnDestroy ()
    /// </summary>
    void OnDestroy ()
    {
        if (animatorMetadataContainer != null && animatorMetadataContainer.stateMachineExtender != null) animatorMetadataContainer.stateMachineExtender.onStateChanged -= onAnimatorStateChanged; // don't try to do this if we never started the puppet
        Timing.KillCoroutines(thisTag);
    }

    /// <summary>
    /// Attaches and populates FX container.
    /// </summary>
    public void AcquireFXContainer()
    {
        battleFXContainer = BattleFXContainer.AttachTo(gameObject);
    }

    /// <summary>
    /// Attaches this puppet to the specified Battler.
    /// </summary>
    public void AttachBattler (Battler _battler)
    {
        battler = _battler;
        battlerData = BattlerDatabase.Get(battler.battlerType);
        logicalPositionOffset = new Vector3(0, battlerData.yOffset, 0);
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
    /// Coroutine: lerps unit between original and final positions.
    /// speed is a float that - like stepTime - represents the amount of time it should take to move one unit.
    /// Normally speed = stepTime, but we might wanna eg. throw units around at speeds independent of their
    /// stepTime values.
    /// </summary>
    private IEnumerator<float> _Move(Vector3 moveVector, float speed)
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
        Idle();
    }

    /// <summary>
    /// Returns true if either primary or fallback types of this anim event can be resolved.
    /// </summary>
    public bool AnimEventIsResolveable (AnimEvent animEvent)
    {
        return animatorMetadataContainer.contents.AnimEventIsResolveable(animEvent);
    }

    /// <summary>
    /// Re-conforms position, scale, and rotation to BattlerData and
    /// field position, then dispatches idle AnimEvent.
    /// </summary>
    public void Idle ()
    {
        SyncPosition();
        if (!battler.isDead) DispatchAnimEvent(new AnimEvent(AnimEventType.Idle, AnimEventType.None, 0, AnimEvent.Flags.IsMandatory, 0));
    }

    /// <summary>
    /// Generates and dispatches an event block for death.
    /// </summary>
    public void DispatchDeathEventBlock ()
    {
        DispatchAnimEvent(new AnimEvent(AnimEventType.Die, AnimEventType.None, 0, AnimEvent.Flags.IsMandatory, 0));
    }

    /// <summary>
    /// Generates and dispatches an event block for stance break.
    /// </summary>
    public void DispatchStanceBreakEventBlock()
    {

    }

    /// <summary>
    /// Generates and dispatches an event block for stance changes.
    /// </summary>
    public void DispatchStanceChangeEventBlock ()
    {

    }

    /// <summary>
    /// Dispatch the given anim event to the BattlerPuppet.
    /// </summary>
    public AnimEventHandle DispatchAnimEvent(AnimEvent animEvent)
    {
        return animatorMetadataContainer.contents.DispatchAnimEvent(animEvent, this);
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
    /// Fires onStatusPacketsChanged event.
    /// </summary>
    public void FireOnStatusPacketsChanged()
    {
        onStatusPacketsChanged();
    }

    /// <summary>
    /// Processes move command. If we have an animation for moveEvent we move gradually across the playfield playing that animation;
    /// otherwise, we immediately SyncPosition().
    /// Plays exitEvent after the move event ends. Normally this is AnimEventType.Idle.
    /// Returns true if we were able to process the moveEvent.
    /// </summary>
    public bool ProcessMove(Vector3 moveVector, float speed, AnimEventType moveEvent)
    {
        bool r = true; // HasAnimFor(moveEvent);
        if (r == true)
        {
            Timing.RunCoroutine(_Move(moveVector, speed));
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
        transform.position = battler.logicalPosition + logicalPositionOffset;
        transform.localRotation = currentRotation;
        transform.localScale = originalScale;
    }

    /// <summary>
    /// Coroutine: Loads all resources this battler will require.
    /// </summary>
    public IEnumerator<float> _Load()
    {
        loadingState = LoadingState.Loading;
        bool operationCompleted = false;
        Action whenLoaderAvailable = () => 
        {
            BattleEventResolverTablesLoader.instance.RequestAudioEventResolverTableLoad(battlerData.audioEventResolverTableType, () =>
            {
                operationCompleted = true;
                managedAudioSource.AcquireAudioEventResolverTable(battlerData.audioEventResolverTableType);
            });
        };
        Action<EventBlock> loadForEventBlock = (eventBlock) =>
        {
            for (int l = 0; l < eventBlock.layers.Length; l++)
            {
                for (int f = 0; f < eventBlock.layers[l].fxEvents.Length; f++)
                {
                    BattleEventResolverTablesLoader.instance.RequestFXLoad(eventBlock.layers[l].fxEvents[f], null);
                }
            }
        };
        Action<BattleAction> loadForBattleAction = (action) =>
        {
            loadForEventBlock(action.animSkip);
            loadForEventBlock(action.onStart);
            loadForEventBlock(action.onConclusion);
            for (int s = 0; s < action.subactions.Length; s++)
            {
                for (int f = 0; f < action.subactions[s].effectPackages.Length; f++) loadForEventBlock(action.subactions[s].effectPackages[f].eventBlock);
                loadForEventBlock(action.subactions[s].eventBlock);
            }
        };
        Timing.RunCoroutine(BattleEventResolverTablesLoader._OnceAvailable(whenLoaderAvailable));
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
        loadingState = LoadingState.LoadCompleted;
    }
}
