using System;
using System.Collections.Generic;
using UnityEngine;
using MovementEffects;

namespace Universe
{
    /// <summary>
    /// Base manager class that inforces a self-referencing singleton pattern.
    /// Your class should derive from this, not directly from the non-generic Manager.
    /// </summary>
    /// <typeparam name="T">Self</typeparam>
    public abstract class Manager<T> : MonoBehaviour, IManager where T : Manager<T>
    {
        private static T instance = null;

        public static T Instance
        {
            get { return instance; }
        }

        protected Manager() { }

        /// <summary>
        /// Called when a deserialized version is loaded.
        /// </summary>
        public void Deserialize()
        {
            instance = (T)this;
        }

        /// <summary>
        /// Calls the given Action once the Manager comes online.
        /// </summary>
        public static void DoOnceOnline (Action onceOnline)
        {
            if (Instance != null) onceOnline();
            else Timing.RunCoroutine(_DoOnceOnline(onceOnline));
        }

        /// <summary>
        /// Coroutine: Calls the given Action once the Manager comes online.
        /// </summary>
        private static IEnumerator<float> _DoOnceOnline (Action onceOnline)
        {
            while (Instance == null) yield return 0;
            onceOnline();
        }
    }
}