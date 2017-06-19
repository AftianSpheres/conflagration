namespace CnfBattleSys
{
    /// <summary>
    /// Data structure representing a single FX event.
    /// </summary>
    public struct FXEvent
    {
        /// <summary>
        /// Flags applicable to FXEvents.
        /// </summary>
        [System.Flags]
        public enum Flags
        {
            None,
            /// <summary>
            /// Crash if we can't resolve this. If it's not mandatory, we just throw away an event we can't resolve.
            /// </summary>
            IsMandatory = 1,
            /// <summary>
            /// Don't move on to lower-priority events/next subaction/etc. until this finishes.
            /// </summary>
            WaitForMe = 1 << 1,
            /// <summary>
            /// Apply this FX to the user of an action or w/e.
            /// </summary>
            ApplyToUser = 1 << 2,
            /// <summary>
            /// Apply this FX to the targets of an action or w/e.
            /// </summary>
            ApplyToTargets = 1 << 3,
            /// <summary>
            /// This FX can be applied to the BattleStage.
            /// </summary>
            ApplyToStage = 1 << 4,
            /// <summary>
            /// This FX's scale is multiplied by the modifier provided by the
            /// BattlerPuppet or BattleStage it's attached to.
            /// (For a quick example: little things have little explosions.
            /// 50-foot robots or whatever, on the other hand, have very big
            /// explosions.)
            /// </summary>
            Scalable = 1 << 5
        }

        /// <summary>
        /// Type of the FX event
        /// </summary>
        public readonly FXEventType fxEventType;
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

        public bool onBattlers { get { return (flags & Flags.ApplyToTargets) == Flags.ApplyToTargets || (flags & Flags.ApplyToUser) == Flags.ApplyToUser; } }
        public bool onStage { get { return (flags & Flags.ApplyToStage) == Flags.ApplyToStage; } }
        public bool isMandatory { get { return (flags & Flags.IsMandatory) == Flags.IsMandatory; } }
        public bool isScalable { get { return (flags & Flags.Scalable) == Flags.Scalable; } }

        public FXEvent (FXEventType _fxEventType, Flags _flags, int _priority)
        {
            fxEventType = _fxEventType;
            flags = _flags;
            priority = _priority;
        }
    }
}