using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.Reflection;
using MovementEffects;

namespace Universe
{
    /// <summary>
    /// Entry point of all game-wide serialized data.
    /// The Universe is a self-regulating script.
    /// When awaken, it loads all the possible game Managers.
    /// </summary>
    [AddComponentMenu("")]
    public sealed class Universe : MonoBehaviour
    {
        public const string PATH = "Universe/";
        private static List<IManager> managers = new List<IManager>();
        public static IManager[] Managers { get { return managers.ToArray(); } }
        private static Universe instance;
        public static Universe Instance { get { return instance; } }

        /// <summary>
        /// MonoBehaviour.Start()
        /// </summary>
        void Start ()
        {
            if (instance != null)
            {
#if !UNITY_EDITOR
                Util.Crash(new Exception("A second Universe should never exist."));
#endif
                DestroyImmediate(gameObject);

            }
            else
            {
                instance = this;
                Deserialize(this);
                DontDestroyOnLoad(gameObject);
            }
        }

        /// <summary>
        /// Used on editor only, to load a manager.
        /// </summary>
        public static void EditorLoad(Type type)
        {
            if (!(Application.isPlaying || !typeof(IManager).IsAssignableFrom(type)))
            {
                GameObject go = Resources.Load(PATH + type.Name) as GameObject;
                go = Instantiate(go) as GameObject;
                IManager manager = go.GetComponent(type) as IManager;
                manager.Deserialize();
                go.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        /// <summary>
        /// Retrieve all the Managers and their data.
        /// If data is nonexistant, create a new one.
        /// </summary>
        private static void Deserialize(Universe universe)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int a = 0; a < assemblies.Length; a++)
            {
                Assembly assembly = assemblies[a];
                Type[] types = assembly.GetTypes();
                for (int t = 0; t < types.Length; t++)
                {
                    Type type = types[t];
                    if (typeof(IManager).IsAssignableFrom(type) && !type.IsAbstract)
                    {
                        GameObject go = Resources.Load(PATH + type.Name) as GameObject;
                        if (go != null)
                        {
                            GameObject clone = Instantiate(go) as GameObject;
                            clone.name = type.Name;
                            clone.transform.parent = Instance.gameObject.transform;
                            IManager manager = clone.GetComponent(type) as IManager;
                            if (manager != null)
                            {
                                RemoveExisting(type);
                                manager.Deserialize();
                                managers.Add(manager);
                            }
                        }
                    }
                }
            }
            if (SceneManager.GetSceneAt(0).buildIndex == 0)
            {
                Action onCompletion = () =>
                {
                    ExtendedSceneManager.Instance.StageForLoading(ExtendedSceneManager.Instance.GetExtendedScene(1));
                    ExtendedSceneManager.Instance.StageForUnloading(ExtendedSceneManager.Instance.GetExtendedScene(0));
                };
                Timing.RunCoroutine(ExtendedSceneManager.Instance._WaitUntilLoadComplete(onCompletion));
            }
        }

        /// <summary>
        /// Removes an existing manager.
        /// </summary>
        private static void RemoveExisting(Type type)
        {
            for (int i = 0; i < managers.Count; i++)
                if (managers[i].GetType() == type)
                    managers.RemoveAt(i);
        }
    }
}