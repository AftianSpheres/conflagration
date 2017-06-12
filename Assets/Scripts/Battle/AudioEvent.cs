namespace CnfBattleSys
{
    /// <summary>
    /// Data structure defining an in-battle audio event.
    /// </summary>
    public struct AudioEvent
    {
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
            /// Apply this audio event to the user of an action or w/e.
            /// </summary>
            ApplyToUser = 1 << 2,
            /// <summary>
            /// Apply this audio event to the targets of an action or w/e.
            /// </summary>
            ApplyToTargets = 1 << 3,
            /// <summary>
            /// This audio event can be applied to the BattleStage.
            /// </summary>
            ApplyToStage = 1 << 4
        }

        /// <summary>
        /// Type of the audio event.
        /// </summary>
        public readonly AudioEventType type;
        /// <summary>
        /// If the resolver table doesn't contain a resolver for the primary
        /// type, we'll try to resolve this type instead.
        /// </summary>
        public readonly AudioEventType fallbackType;
        /// <summary>
        /// The AudioSourceType that the clip should play as.
        /// </summary>
        public readonly AudioSourceType clipType;
        /// <summary>
        /// Flags associated with this audio event.
        /// </summary>
        public readonly Flags flags;
        /// <summary>
        /// Events with higher priority will execute before events with lower priority.
        /// If we have to wait on a higher priorty event, we don't handle lower priorty
        /// events until it's done.
        /// </summary>
        public readonly int priority;

        public AudioEvent (AudioEventType _type, AudioEventType _fallbackType, AudioSourceType _clipType, Flags _flags, int _priority)
        {
            if (_type == AudioEventType.None) Util.Crash(_type, typeof(AudioEvent), null);
            type = _type;
            fallbackType = _fallbackType;
            clipType = _clipType;
            flags = _flags;
            priority = _priority;
        }
    }
}