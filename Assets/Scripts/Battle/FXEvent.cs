using System;

namespace CnfBattleSys
{
    /// <summary>
    /// Data structure representing a single FX event.
    /// </summary>
    public struct FXEvent : IEquatable<FXEvent>
    {
        /// <summary>
        /// Flags applicable to FXEvents.
        /// </summary>
        [Flags]
        public enum Flags
        {
            //None,
            /// <summary>
            /// Crash if we can't resolve this. If it's not mandatory, we just throw away an event we can't resolve.
            /// </summary>
            IsMandatory = 1,
            /// <summary>
            /// Don't move on to lower-priority events/next subaction/etc. until this finishes.
            /// </summary>
            WaitForMe = 1 << 1,
            /// <summary>
            /// This FX's scale is multiplied by the modifier provided by the
            /// BattlerPuppet or BattleStage it's attached to.
            /// (For a quick example: little things have little explosions.
            /// 50-foot robots or whatever, on the other hand, have very big
            /// explosions.)
            /// </summary>
            Scalable = 1 << 2,
            /// <summary>
            /// Dispatch this event to all relevant puppets -
            /// even if the associated subaction or w/e didn't apply to them.
            /// </summary>
            HandleEvenIfFailed = 1 << 3
        }

        /// <summary>
        /// Type of the FX event
        /// </summary>
        public readonly FXEventType fxEventType;
        public readonly BattleEventTargetType targetType;
        /// <summary>
        /// Flags associated with this FX event.
        /// </summary>
        public readonly Flags flags;
        /// <summary>
        /// Events with higher priority will execute before events with lower priority.
        /// If we have to wait on a higher priorty event, we don't handle lower priorty
        /// events until it's done.
        /// </summary>
        public readonly int priority;

        public bool onBattlers { get { return (targetType & BattleEventTargetType.PrimaryTargets) == BattleEventTargetType.PrimaryTargets || 
                                              (targetType & BattleEventTargetType.SecondaryTargets) == BattleEventTargetType.SecondaryTargets ||
                                              (targetType & BattleEventTargetType.User) == BattleEventTargetType.User; } }
        public bool onStage { get { return (targetType & BattleEventTargetType.Stage) == BattleEventTargetType.Stage; } }
        public bool isMandatory { get { return (flags & Flags.IsMandatory) == Flags.IsMandatory; } }
        public bool isScalable { get { return (flags & Flags.Scalable) == Flags.Scalable; } }
        public SignedFXEventType signedFXEventType { get { return new SignedFXEventType(this); } }

        public FXEvent (FXEventType _fxEventType, BattleEventTargetType _targetType, Flags _flags, int _priority)
        {
            fxEventType = _fxEventType;
            targetType = _targetType;
            flags = _flags;
            priority = _priority;
        }

        /// <summary>
        /// IEquatable.Equals ()
        /// Returns true if these events are a) of the same time and b) both have the same scalable flag.
        /// If those conditions are true, they can use the same prefab. Otherwise, they can't.
        /// </summary>
        bool IEquatable<FXEvent>.Equals(FXEvent other)
        {
            return (other.fxEventType == fxEventType && (other.flags & Flags.Scalable) == (flags & Flags.Scalable));
        }
    }
}