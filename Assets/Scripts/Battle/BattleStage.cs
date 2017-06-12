using UnityEngine;
using System;
using System.Collections.Generic;
using CnfBattleSys;
using MovementEffects;

/// <summary>
/// Monobehaviour that handles animation events, waits for input where needed, and decides when to advance the battle state.
/// At the present time, this is sorta stubbly and just implemented enough for Demo_BattleConsole to piggyback off of it.
/// </summary>
public class BattleStage : MonoBehaviour
{
    /// <summary>
    /// Parent class for AnimEvent and GUIEvent objects.
    /// </summary>
    private abstract class StageEvent
    {
        /// <summary>
        /// This is almost always 0. If greater than 0, this event will be staged such that it's ahead of all
        /// events of lower priority.
        /// </summary>
        public int priority { get; protected set; }
        public Battler invoker { get; protected set; }
        protected bool isRunning = false;

        public abstract void Update();

        /// <summary>
        /// Called when the StageEvent has finished running
        /// </summary>
        public void MarkCompleted ()
        {
            isRunning = false;
        }
    }

    /// <summary>
    /// Class that ties an AnimEventType to the battler that created it.
    /// Once created, the AnimEvent acts as a simple data-container object until we dequeue
    /// it and start handling it.
    /// </summary>
    private class AnimEvent : StageEvent
    {
        public readonly AnimEventType animEventType;
        public readonly AnimEventFlags flags;

        public AnimEvent (AnimEventType _animEventType, Battler _invoker, AnimEventFlags _flags)
        {
            animEventType = _animEventType;
            invoker = _invoker;
            flags = _flags;
        }

        /// <summary>
        /// Checks progress of the event and removes from all lists if finished.
        /// </summary>
        public override void Update ()
        {
            if (!isRunning) instance.EndAnimEvent(this);
        }
    }

    /// <summary>
    /// Flags for anim event parameters.
    /// </summary>
    [Flags]
    private enum AnimEventFlags
    {
        None = 0,
        RequiresCameraControl = 1,
        FocusOnInvoker = 1 << 1,
        RunConcurrentWithOtherEvents = 1 << 2,
        WaitUntilUICatchesUp = 1 << 3
    } 

    /// <summary>
    /// Battle stage states.
    /// </summary>
    private enum LocalState
    {
        Offline,
        HandlingAnimEvent,
        ReadyToAdvanceBattle
    }

    private LocalState localState = LocalState.Offline;
    private BattlerPuppet[] puppets;
    private List<AnimEvent> activeAnimEvents;
    /// <summary>
    /// This is used FIFO, generally speaking, but it can't be a queue because we need to be able to break strict FIFO for handling priority.
    /// </summary>
    private List<AnimEvent> unhandledAnimEvents;
    private List<StageEvent> allActiveStageEvents;
    private List<StageEvent> stageEventsBuffer;
    private LinkedList<FXEventType> fxEventsToLoad;
    private Transform fxParent;
    /// <summary>
    /// BattleStage isn't actually a singleton, but it interacts with a lot of static classes on a message-passing basis,
    /// so it's useful for those to be able to address the current instance without being given a reference to a specific
    /// BattleStage. There should never be more than one of these in a scene at a time, anyway.
    /// </summary>
    public static BattleStage instance { get; private set; }
    public Vector3 fxScaleMod = Vector3.one;

	/// <summary>
    /// MonoBehaviour.Awake ()
    /// </summary>
	void Awake ()
    {
        instance = this;
        fxParent = Instantiate(default(GameObject), transform).transform;
        fxParent.localPosition = Vector3.zero;
        fxParent.localScale = Vector3.one;
        fxParent.gameObject.name = "FX";
        activeAnimEvents = new List<AnimEvent>();
        unhandledAnimEvents = new List<AnimEvent>();
        allActiveStageEvents = new List<StageEvent>();
        stageEventsBuffer = new List<StageEvent>();
	}

    /// <summary>
    /// MonoBehaviour.OnDestroy ()
    /// </summary>
    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    /// <summary>
    /// MonoBehaviour.Update
    /// </summary>
    void Update ()
    {
        if (bUI_BattleUIController.instance.actionWheel.isOpen)
        {
            Debug.Log("Action wheel test!!!!");
            return;
        }
        switch (localState)
        {
            case LocalState.ReadyToAdvanceBattle:
                switch (BattleOverseer.currentBattle.state)
                {
                    case BattleData.State.BattleWon:
                    case BattleData.State.BattleLost:
                        gameObject.SetActive(false);
                        break;
                    case BattleData.State.WaitingForInput:
                    case BattleData.State.Paused:
                        break;
                    default:
                        BattleOverseer.currentBattle.BattleStep();
                        break;
                }
                break;
            case LocalState.HandlingAnimEvent:
                if (activeAnimEvents.Count == 0)
                {
                    if (unhandledAnimEvents.Count > 0) AdvanceAnimEvents();
                    else localState = LocalState.ReadyToAdvanceBattle;
                }
                else
                {
                    for (int i = 0; i < activeAnimEvents.Count; i++) activeAnimEvents[i].Update();
                }
                break;
        }
	}

    /// <summary>
    /// Called by BattleOverseer at start of battle.
    /// </summary>
    public void StartOfBattle ()
    {
        if (localState == LocalState.Offline) Initialize();
    }

    /// <summary>
    /// Called when BattleStage is offline if starting a new battle.
    /// </summary>
    private void Initialize()
    {
        bUI_BattleUIController.instance.elementsGen.AssignInfoboxesToBattlers();
        localState = LocalState.ReadyToAdvanceBattle;
    }

    /// <summary>
    /// Called by the BattleOverseer each time it starts a turn.
    /// </summary>
    public void StartOfTurn ()
    {
        if (BattleOverseer.currentBattle.state != BattleData.State.Offline) LogBattleState();
    }

    /// <summary>
    /// This should look up the right coroutine to run based on the animEventType and
    /// pass any necessary arguments into it before returning a handle, but it doesn't do that yet.
    /// Right now it just returns an instance of our "fake handle" coroutine,
    /// so everything is going to be done instantly.
    /// </summary>
    private void RunAnimEventCoroutine(StageEvent stageEvent, AnimEventType animEventType, Battler invoker)
    {
        Timing.RunCoroutine(_FinishStageEventInstantly(stageEvent));
    }

    /// <summary>
    /// Gets and handles the next batch of anim events.
    /// </summary>
    private void AdvanceAnimEvents ()
    {
        stageEventsBuffer.Clear();
        int priority = unhandledAnimEvents[0].priority;
        for (int i = 0; i < unhandledAnimEvents.Count; i++)
        {
            AnimEvent evt = unhandledAnimEvents[i];
            stageEventsBuffer.Add(evt);
            if ((evt.flags & AnimEventFlags.RunConcurrentWithOtherEvents) != AnimEventFlags.RunConcurrentWithOtherEvents || evt.priority != priority) break;
        }
        for (int i = 0; i < stageEventsBuffer.Count; i++)
        {
            HandleNextAnimEvent(stageEventsBuffer[i] as AnimEvent);
        }
    }

    /// <summary>
    /// Starts processing the given animEvent.
    /// </summary>
    private void HandleNextAnimEvent (AnimEvent evt)
    {
        unhandledAnimEvents.Remove(evt);
        RunAnimEventCoroutine(evt, evt.animEventType, evt.invoker);
        allActiveStageEvents.Add(evt);
        activeAnimEvents.Add(evt);
    }

    /// <summary>
    /// Enqueues an anim event of the specified type and invoker.
    /// </summary>
    public void PrepareAnimEvent (AnimEventType animEventType, Battler invoker)
    {
        AnimEvent evt = new AnimEvent(animEventType, invoker, GetFlagsForAnimEventType(animEventType));
        int jumpAheadIndex = -1;
        for (int i = 0; i < unhandledAnimEvents.Count; i++)
        {
            if (evt.priority > unhandledAnimEvents[i].priority) jumpAheadIndex = i;
        }
        if (jumpAheadIndex != -1) unhandledAnimEvents.Insert(jumpAheadIndex, evt);
        else unhandledAnimEvents.Add(evt);
    }

    /// <summary>
    /// Adds the given fx type to the list of fx prefabs for the BattleStage to load in
    /// and instantiate at the first opportunity.
    /// </summary>
    public void RequestFXLoad (FXEventType fxEventType)
    {
        if (!fxEventsToLoad.Contains(fxEventType)) fxEventsToLoad.AddLast(fxEventType);
    }

    /// <summary>
    /// Called after an anim event has finished.
    /// Removes it from the lists the BattleStage uses to track what's running.
    /// </summary>
    private void EndAnimEvent (AnimEvent evt)
    {
        Debug.Log("Ran anim event: " + evt.animEventType.ToString());
        allActiveStageEvents.Remove(evt);
        activeAnimEvents.Remove(evt);
    }

    /// <summary>
    /// Gets flags for the specified anim event type.
    /// </summary>
    private AnimEventFlags GetFlagsForAnimEventType (AnimEventType animEventType)
    {
        AnimEventFlags flags = AnimEventFlags.None;
        switch (animEventType)
        {
            case AnimEventType.StanceBreak:
            case AnimEventType.Move:
            case AnimEventType.Dodge:
            case AnimEventType.TestAnim_OnUse:
                flags |= AnimEventFlags.FocusOnInvoker;
                flags |= AnimEventFlags.RequiresCameraControl;
                break;
            case AnimEventType.Hit:
                flags |= AnimEventFlags.RunConcurrentWithOtherEvents;
                break;
        }
        return flags;
    }

    /// <summary>
    /// Dumps battle state to console.
    /// </summary>
    private void LogBattleState()
    {
        string o = string.Empty;
        for (int b = 0; b < BattleOverseer.currentBattle.allBattlers.Length; b++)
        {
            Battler bat = BattleOverseer.currentBattle.allBattlers[b];
            // This might actually be the single worst line of code in the world but idgaf given what the usage case is.
            string battlerString = b.ToString() + ": " + bat.battlerType.ToString() + "|" + bat.currentStance.ToString() + " (HP: " + bat.currentHP + " / " + bat.stats.maxHP + ") (Stamina: " + bat.currentStamina.ToString() + ") [" + bat.side.ToString() + "]";
            o += battlerString + Environment.NewLine;
        }
        o += "<Hit spacebar to advance battle>";
        Debug.Log(o);
    }

    /// <summary>
    /// This is a "fake" coroutine we use just to be able to instantly mark stage events as done.
    /// </summary>
    private IEnumerator<float> _FinishStageEventInstantly (StageEvent stageEvent)
    {
        stageEvent.MarkCompleted();
        yield break;
    }

    private IEnumerator<float> _LoadForBattlers (Battler[] battlers)
    {
        puppets = new BattlerPuppet[battlers.Length];
        const string prefabsPath = "Battle/Prefabs/BattlePuppet/";
        for (int b = 0; b < battlers.Length; b++)
        {
            ResourceRequest request = Resources.LoadAsync<BattlerPuppet>(prefabsPath + battlers[b].battlerType);
            while (request.progress < 1.0f) yield return 0;
            if (request.asset == null)
            {
                Util.Crash("No battler puppet prefab for battler id of " + battlers[b].battlerType);
                yield break;
            }
            BattlerPuppet prefab = (BattlerPuppet)request.asset;
            while (prefab.loading) yield return 0;
            puppets[b] = prefab;
        }
    }

    private IEnumerator<float> _LoadForFXEvent (FXEvent fxEvent)
    {
        const string prefabsPath = "Battle/Prefabs/FX/";
        ResourceRequest request = Resources.LoadAsync<BattleFXController>(prefabsPath + fxEvent.fxEventType);
        while (request.progress < 1.0f) yield return request.progress;
        if ((fxEvent.flags & FXEvent.Flags.ApplyToStage) == FXEvent.Flags.ApplyToStage)
        {
            BattleFXController newController = (BattleFXController)Instantiate(request.asset, fxParent);
            // a dict to correlate controller references to fx ids
        }
        if ((fxEvent.flags & FXEvent.Flags.ApplyToTargets) == FXEvent.Flags.ApplyToTargets || (fxEvent.flags & FXEvent.Flags.ApplyToUser) == FXEvent.Flags.ApplyToUser)
        {

        }
    }
}
