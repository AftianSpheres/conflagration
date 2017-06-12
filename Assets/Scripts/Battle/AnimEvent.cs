namespace CnfBattleSys
{
    /// <summary>
    /// Data structure representing a single animation event
    /// for a puppet to resolve.
    /// </summary>
    public struct AnimEvent
    {
        /// <summary>
        /// Flags applicable to AnimEvents.
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
            /// Apply this anim event to the user of an action or w/e.
            /// </summary>
            ApplyToUser = 1 << 2,
            /// <summary>
            /// Apply this anim event to the targets of an action or w/e.
            /// </summary>
            ApplyToTargets = 1 << 3,
        }
        /// <summary>
        /// Type of the AnimEvent.
        /// </summary>
        public readonly AnimEventType animEventType;
        /// <summary>
        /// If this event can't be resolved, try to resolve this instead.
        /// </summary>
        public readonly AnimEventType fallbackType;
        /// <summary>
        /// Flags attached to this event.
        /// </summary>
        public readonly Flags flags;
        /// <summary>
        /// Events with higher priority will execute before events with lower priority.
        /// If we have to wait on a higher priorty event, we don't handle lower priorty
        /// events until it's done.
        /// </summary>
        public readonly int priority;

        public AnimEvent (AnimEventType _animEventType, AnimEventType _fallbackType, Flags _flags, int _priority)
        {
            animEventType = _animEventType;
            fallbackType = _fallbackType;
            flags = _flags;
            priority = _priority;
        }
    }
}