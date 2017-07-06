using System;
using UnityEngine;

namespace CnfBattleSys
{
    /// <summary>
    /// Base class for battle FX controllers.
    /// This exposes common functionality that all battle FX
    /// prefabs use. To do a specific battle FX (ie. attack animations, etc.)
    /// inherit from this and implement specific functionality.
    /// </summary>
    public abstract class BattleFXController : MonoBehaviour
    {
        public event Action onCompletion;
        public event Action onStart;
        /// <summary>
        /// Because you can have different events of the same type (ie. same prefab) that behave differently depending on
        /// the flags they have set, use the fact that FXEvent implements IEquitable to figure out which controller
        /// to use.
        /// </summary>
        public FXEvent originatingEvent { get; private set; }
        public bool isRunning { get; private set; }

        /// <summary>
        /// Starts the FX.
        /// </summary>
        public virtual FXEventHandle Commence(FXEvent fxEvent)
        {
            onStart();
            isRunning = true;
            return new FXEventHandle(fxEvent, this);
        }

        /// <summary>
        /// Finishes the FX.
        /// This should also reset any animators or models or w/e
        /// that are a part of this FX to their original state.
        /// </summary>
        public virtual void Finish()
        {
            onCompletion();
            isRunning = false;
        }
    }

}