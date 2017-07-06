using System;
using System.Collections.Generic;
using CnfBattleSys;

/// <summary>
/// Object created when an event block is dispatched.
/// Manages handles for layers and their child events,
/// keeps them in sequence, and calls onBlockCompleted
/// action once everything is done.
/// </summary>
public class EventBlockHandle
{
    /// <summary>
    /// Object created when a Layer is dispatched.
    /// Manages a set of AnimEventHandle/AudioEventHandle/FXEventHandle
    /// instances and calls out when everything that needs to finish has
    /// done so.
    /// </summary>
    public class LayerHandle
    {
        public readonly List<AnimEventHandle> animEventHandles = new List<AnimEventHandle>(16);
        public readonly List<AudioEventHandle> audioEventHandles = new List<AudioEventHandle>(16);
        public readonly List<FXEventHandle> fxEventHandles = new List<FXEventHandle>(16);
        private LinkedList<BattleEventHandle> eventHandlesAwaited = new LinkedList<BattleEventHandle>();
        private readonly EventBlockHandle parent;

        /// <summary>
        /// The EventBlock layer this handle corresponds to.
        /// </summary>
        public readonly EventBlock.Layer layer;
        
        public LayerHandle (EventBlock.Layer _layer, EventBlockHandle _parent)
        {
            parent = _parent;
            layer = _layer;
            for (int i = 0; i < layer.animEvents.Length; i++) Dispatch(layer.animEvents[i]);
            for (int i = 0; i < layer.audioEvents.Length; i++) Dispatch(layer.audioEvents[i]);
            for (int i = 0; i < layer.fxEvents.Length; i++) Dispatch(layer.fxEvents[i]);
            if (eventHandlesAwaited.Count < 1) LayerFinished(); // If we don't wind up waiting on _something_ we never call EventHandleFinished, so.
        }

        /// <summary>
        /// Finds the puppets an animEvent should be
        /// dispatched to, and dispatches it to them.
        /// </summary>
        private void Dispatch (AnimEvent animEvent)
        {
            Action<BattlerPuppet> forThis = (puppet) => 
            {
                AnimEventHandle evtHandle = puppet.DispatchAnimEvent(animEvent);
                if (evtHandle.waitForMe) WaitOn(evtHandle);
                animEventHandles.Add(evtHandle);
            };
            if ((animEvent.targetType & BattleEventTargetType.User) == BattleEventTargetType.User)
            {
                forThis(BattleOverseer.currentBattle.actionExecutionSubsystem.currentActingBattler.puppet);
            }
            if ((animEvent.targetType & BattleEventTargetType.PrimaryTargets) == BattleEventTargetType.PrimaryTargets)
            {
                for (int i = 0; i < BattleOverseer.currentBattle.actionExecutionSubsystem.targets.Count; i++) forThis(BattleOverseer.currentBattle.actionExecutionSubsystem.targets[i].puppet);
            }
            if ((animEvent.targetType & BattleEventTargetType.SecondaryTargets) == BattleEventTargetType.SecondaryTargets)
            {
                for (int i = 0; i < BattleOverseer.currentBattle.actionExecutionSubsystem.alternateTargets.Count; i++) forThis(BattleOverseer.currentBattle.actionExecutionSubsystem.alternateTargets[i].puppet);
            }
        }

        /// <summary>
        /// Finds the sources an audioEvent should be
        /// dispatched to, and dispatches it to them.
        /// </summary>
        private void Dispatch (AudioEvent audioEvent)
        {
            Func<ManagedAudioSource, bool> forThis = (managedAudioSource) =>
            {
                AudioEventHandle evtHandle = managedAudioSource.DispatchAudioEvent(audioEvent);
                if (evtHandle.waitForMe) WaitOn(evtHandle);
                audioEventHandles.Add(evtHandle);
                return ((audioEvent.flags & AudioEvent.Flags.Exclusive) == AudioEvent.Flags.Exclusive);
            };
            if ((audioEvent.targetType & BattleEventTargetType.Stage) == BattleEventTargetType.Stage)
            {
                if (forThis(BattleStage.instance.managedAudioSource)) return;
            }
            if ((audioEvent.targetType & BattleEventTargetType.User) == BattleEventTargetType.User)
            {
                if (forThis(BattleOverseer.currentBattle.actionExecutionSubsystem.currentActingBattler.puppet.managedAudioSource)) return;
            }
            if ((audioEvent.targetType & BattleEventTargetType.PrimaryTargets) == BattleEventTargetType.PrimaryTargets)
            {
                for (int i = 0; i < BattleOverseer.currentBattle.actionExecutionSubsystem.targets.Count; i++)
                    if (forThis(BattleOverseer.currentBattle.actionExecutionSubsystem.targets[i].puppet.managedAudioSource)) return;
            }
            if ((audioEvent.targetType & BattleEventTargetType.SecondaryTargets) == BattleEventTargetType.SecondaryTargets)
            {
                for (int i = 0; i < BattleOverseer.currentBattle.actionExecutionSubsystem.alternateTargets.Count; i++)
                {
                    if (forThis(BattleOverseer.currentBattle.actionExecutionSubsystem.alternateTargets[i].puppet.managedAudioSource)) return;
                }
            }
        }

        /// <summary>
        /// Finds the controller an FXevent should be dispatched to.
        /// Dispatches to that.
        /// </summary>
        private void Dispatch (FXEvent fxEvent)
        {
            Action<BattleFXContainer> forThis = (container) =>
            {
                FXEventHandle evtHandle = container.GetController(fxEvent).Commence(fxEvent);
                if (evtHandle.waitForMe) WaitOn(evtHandle);
                fxEventHandles.Add(evtHandle);
            };
            if ((fxEvent.targetType & BattleEventTargetType.Stage) == BattleEventTargetType.Stage)
            {
                forThis(BattleStage.instance.battleFXContainer);
            }
            if ((fxEvent.targetType & BattleEventTargetType.User) == BattleEventTargetType.User)
            {
                forThis(BattleOverseer.currentBattle.actionExecutionSubsystem.currentActingBattler.puppet.battleFXContainer);
            }
            if ((fxEvent.targetType & BattleEventTargetType.PrimaryTargets) == BattleEventTargetType.PrimaryTargets)
            {
                for (int i = 0; i < BattleOverseer.currentBattle.actionExecutionSubsystem.targets.Count; i++) forThis(BattleOverseer.currentBattle.actionExecutionSubsystem.targets[i].puppet.battleFXContainer);
            }
            if ((fxEvent.targetType & BattleEventTargetType.SecondaryTargets) == BattleEventTargetType.SecondaryTargets)
            {
                for (int i = 0; i < BattleOverseer.currentBattle.actionExecutionSubsystem.alternateTargets.Count; i++) forThis(BattleOverseer.currentBattle.actionExecutionSubsystem.alternateTargets[i].puppet.battleFXContainer);
            }
        }

        /// <summary>
        /// This layer of the EventBlock has completed everything it
        /// needs to do, and so we can move on.
        /// </summary>
        private void LayerFinished ()
        {
            parent.Advance();
        }

        /// <summary>
        /// Removes an event handle from the list of ones we're waiting on.
        /// If we're no longer waiting on any, finish the layer.
        /// </summary>
        private void EventHandleFinished (LinkedListNode<BattleEventHandle> node)
        {
            eventHandlesAwaited.Remove(node);
            if (eventHandlesAwaited.Count < 1) LayerFinished();
        }

        /// <summary>
        /// Adds this event to the list of event handles we're waiting on,
        /// and passes it a callback that lets it remove itself from that list when it's done.
        /// </summary>
        private void WaitOn(BattleEventHandle evtHandle)
        {
            LinkedListNode<BattleEventHandle> node = eventHandlesAwaited.AddLast(evtHandle);
            evtHandle.onEventCompleted += () => { EventHandleFinished(node); };
        }
    }

    public event Action onBlockCompleted;
    public readonly EventBlock eventBlock;
    public int layerIndex { get; private set; }
    public LayerHandle currentLayer { get; private set; }

    public EventBlockHandle (EventBlock _eventBlock)
    {
        eventBlock = _eventBlock;
        currentLayer = DispatchLayer(eventBlock.layers[0]);
    } 

    /// <summary>
    /// Move onto the next layer, if one exists.
    /// Otherwise, this event block has finished.
    /// </summary>
    protected void Advance()
    {
        layerIndex++;
        if (layerIndex < eventBlock.layers.Length) currentLayer = DispatchLayer(eventBlock.layers[layerIndex]);
        else onBlockCompleted();
    }

    /// <summary>
    /// Dispatches a layer and returns a LayerHandle.
    /// </summary>
    private LayerHandle DispatchLayer (EventBlock.Layer layer)
    {
        return new LayerHandle(layer, this);
    }
}