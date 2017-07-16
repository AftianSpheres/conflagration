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
	/// Called when we finish handling the final
	/// enqueued event block.
	/// (This is automatically set to null after it's
	/// fired.)
	/// </summary>
	public event Action onAllEventBlocksFinished;
    private BattlerPuppet[] puppets;
    private Dictionary<Battler, BattlerPuppet> puppetsDict = new Dictionary<Battler, BattlerPuppet>();
    private Queue<EventBlockHandle> eventBlocksToDispatch = new Queue<EventBlockHandle>(8);

    /// <summary>
    /// BattleStage isn't actually a singleton, but it interacts with a lot of static classes on a message-passing basis,
    /// so it's useful for those to be able to address the current instance without being given a reference to a specific
    /// BattleStage. There should never be more than one of these in a scene at a time, anyway.
    /// </summary>
    public bool processingAnyEventBlock { get { return currentEventBlock != null; } }
    public static BattleStage instance { get; private set; }
    public BattleFXContainer battleFXContainer { get; private set; }
    public EventBlockHandle currentEventBlock { get; private set; }
    public ManagedAudioSource managedAudioSource { get; private set; }
    public Transform battlerPuppetsParent { get; private set; }
    public Transform fxControllersParent { get; private set; }
    public float fxScale = 30;
    private string thisTag;

    /// <summary>
    /// Ensures that onAllEventBlocksFinished is cleared after running
    /// </summary>
    void onAllEventBlocksFinishedAutoclear ()
    {
        onAllEventBlocksFinished = onAllEventBlocksFinishedAutoclear;
    }

	/// <summary>
    /// MonoBehaviour.Awake ()
    /// </summary>
	void Awake ()
    {
        instance = this;
        thisTag = GetInstanceID().ToString();
        battlerPuppetsParent = Util.CreateEmptyChild(transform).transform;
        fxControllersParent = Util.CreateEmptyChild(transform).transform;
        battlerPuppetsParent.gameObject.name = "Battlers";
        fxControllersParent.gameObject.name = "FX Controllers";
        onAllEventBlocksFinished = onAllEventBlocksFinishedAutoclear;
	}

    /// <summary>
    /// MonoBehaviour.OnDestroy ()
    /// </summary>
    void OnDestroy()
    {
        Timing.KillCoroutines(thisTag);
        if (instance == this) instance = null;
    }

    /// <summary>
    /// Creates a handle for the given event block, enqueues it if needed, and
    /// returns it so that the caller can subscribe to events.
    /// </summary>
    public EventBlockHandle Dispatch (EventBlock eventBlock, Action callback = null)
    {
        Action advance = () =>
        {
            if (eventBlocksToDispatch.Count > 0) currentEventBlock = eventBlocksToDispatch.Dequeue();
            else 
			{
			    currentEventBlock = null;
				onAllEventBlocksFinished();
			}
        };
        EventBlockHandle h = new EventBlockHandle(eventBlock, callback);
        h.onBlockCompleted += advance;
        if (processingAnyEventBlock) eventBlocksToDispatch.Enqueue(h); // event blocks are always processed one at a time
        else currentEventBlock = h;
        return h;
    }

    /// <summary>
    /// Attaches and populates FX container.
    /// </summary>
    public void AcquireFXContainer ()
    {
        battleFXContainer = BattleFXContainer.AttachTo(gameObject);
    }

    /// <summary>
    /// Called by BattleOverseer at start of battle.
    /// </summary>
    public void StartOfBattle ()
    {
        Initialize();
    }

    /// <summary>
    /// Called when BattleStage is offline if starting a new battle.
    /// </summary>
    private void Initialize()
    {
        bUI_BattleUIController.instance.elementsGen.AssignInfoboxesToBattlers();
    }

    /// <summary>
    /// Called by the BattleOverseer each time it starts a turn.
    /// </summary>
    public void StartOfTurn ()
    {
        if (BattleOverseer.currentBattle.state != BattleData.State.Offline) LogBattleState();
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
    /// Gets the puppet tied to this battler, assuming one exists
    /// </summary>
    public BattlerPuppet GetPuppetAssociatedWithBattler(Battler battler)
    {
        if (!puppetsDict.ContainsKey(battler)) return null;
        else return puppetsDict[battler];
    }

    /// <summary>
    /// Add the given battler:puppet pairing to the dict.
    /// </summary>
    public void TiePuppetToBattler (Battler battler, BattlerPuppet puppet)
    {
        puppetsDict[battler] = puppet;
        puppet.capsuleCollider.radius = battler.footprintRadius;
    }

    /// <summary>
    /// Coroutine: Load in all resources that battlers will require during this battle.
    /// </summary>
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
            puppets[b] = Instantiate((BattlerPuppet)request.asset, battlerPuppetsParent);
            yield return Timing.WaitUntilDone(Timing.RunCoroutine(puppets[b]._Load(), thisTag));
            while (puppets[b].loadingState == BattlerPuppet.LoadingState.Loading) yield return 0;
        }
    }

    /// <summary>
    /// Aborts the current event block and
    /// cancels all remaining ones.
    /// </summary>
    public void CancelEventBlocks ()
    {
        eventBlocksToDispatch.Clear();
        currentEventBlock.Abort();
        onAllEventBlocksFinished();
    }

    /// <summary>
    /// Calls Idle() on all BattlerPuppets.
    /// </summary>
    public void SetBattlersIdle ()
    {
        for (int i = 0; i < puppets.Length; i++) puppets[i].Idle();
    }
}
