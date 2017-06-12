using System.Collections;
using System.Collections.Generic;
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
        public FXEventType fxEventType { get; private set; }
        public bool isRunning { get; private set; }

        /// <summary>
        /// Starts the FX.
        /// </summary>
        public virtual void Commence()
        {
            isRunning = true;
        }

        /// <summary>
        /// Finishes the FX.
        /// This should also reset any animators or models or w/e
        /// that are a part of this FX to their original state.
        /// </summary>
        public virtual void Finish()
        {
            isRunning = false;
        }
    }

}