using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Universe;
using CnfBattleSys;
using MovementEffects;

namespace ExtendedSceneManagement
{

    /// <summary>
    /// Manager that handles scene loading and unloading.
    /// Go through this to do anything that involves scene loading/unloading!
    /// We use this manager to expose a much less, uh, _bad_ set of
    /// functions to code elsewhere in the project on the basis
    /// that Unity's scene manager API is sorta questionable.
    /// </summary>
    public class ExtendedSceneManager : Manager<ExtendedSceneManager>
    {
        /// <summary>
        /// Contains scene load/unload rule generators.
        /// </summary>
        private static class Rules
        {
            /// <summary>
            /// Produce a method that compares to the given SceneRing bitmask.
            /// If extendedScene.metadata.sceneRing is in that bitmask, fire off the given action.
            /// </summary>
            public static Action<ExtendedScene> InRings(SceneRing comparison, Action<ExtendedScene> action)
            {
                return (extendedScene) =>
                {
                    if ((comparison & extendedScene.metadata.sceneRing) == comparison) action(extendedScene);
                };
            }

            /// <summary>
            /// Produce a method that compares to the given SceneRing bitmask.
            /// If extendedScene.metadata.sceneRing is in that bitmask and is the active scene in its ring,
            /// fire off the given action.
            /// </summary>
            public static Action<ExtendedScene> InRingsAndActive(SceneRing comparison, Action<ExtendedScene> action)
            {
                return (extendedScene) =>
                {
                    if ((comparison & extendedScene.metadata.sceneRing) == comparison && Instance.GetActiveScene(extendedScene.metadata.sceneRing) == extendedScene) action(extendedScene);
                };
            }

            /// <summary>
            /// Produce a method that compares to the given SceneRing bitmask.
            /// If extendedScene.metadata.sceneRing is in that bitmask and isn't the active scene in its ring,
            /// fire off the given action.
            /// </summary>
            public static Action<ExtendedScene> InRingsAndInactive(SceneRing comparison, Action<ExtendedScene> action)
            {
                return (extendedScene) =>
                {
                    if ((comparison & extendedScene.metadata.sceneRing) == comparison && Instance.GetActiveScene(extendedScene.metadata.sceneRing) != extendedScene) action(extendedScene);
                };
            }
        }
        /// <summary>
        /// Stages of the batch load/unload process.
        /// </summary>
        public enum LoadPhase
        {
            NotLoading,
            Loading,
            Unloading
        }
        /// <summary>
        /// Enables detailed log output from the ExtendedSceneManager,
        /// if this is also a debug build. (If you're not debugging the ExtendedSceneManager,
        /// you can set verbose = false here instead to avoid polluting the log.)
        /// </summary>
        private static bool verbose = false;
        public bool loading { get { return loadPhase == LoadPhase.Loading || loadPhase == LoadPhase.Unloading; } }
        public LoadPhase loadPhase { get; private set; }
        private Dictionary<SceneRing, ExtendedScene> lastScenesActiveInRings;
        private Dictionary<SceneRing, ExtendedScene[]> scenesBySceneRings;
        private ExtendedScene[] extendedScenesArray;
        private List<AsyncOperation> currentLoadingOps;
        private List<ExtendedScene> loadedScenes;
        private List<ExtendedScene> scenesBufferList;
        private List<SceneMetadata> sceneMetadataBufferList;
        private Queue<ExtendedScene> scenesToLoad;
        private Queue<ExtendedScene> scenesToUnload;
        private Timing timing;
        private Action<ExtendedScene>[] systemScenesRules = { Rules.InRings(SceneRing.SystemScenes, scene => { scene.StageForUnloading(); }) };
        private Action<ExtendedScene>[] venueScenesRules =  { Rules.InRingsAndInactive(SceneRing.WorldScenes, scene => { scene.StageForUnloading(); }),
                                                          Rules.InRingsAndActive(SceneRing.WorldScenes, scene => { scene.SuspendScene(); }) };

        /// <summary>
        /// MonoBehaviour.Awake()
        /// </summary>
        void Awake()
        {
            SceneRing[] rings = (SceneRing[])Enum.GetValues(typeof(SceneRing));
            BattleOverseer.FirstRunSetup();
            timing = gameObject.AddComponent<Timing>();
            currentLoadingOps = new List<AsyncOperation>(32);
            extendedScenesArray = new ExtendedScene[SceneDatatable.metadata.Length];
            loadedScenes = new List<ExtendedScene>(32);
            scenesToLoad = new Queue<ExtendedScene>(32);
            scenesToUnload = new Queue<ExtendedScene>(32);
            scenesBufferList = new List<ExtendedScene>(128);
            sceneMetadataBufferList = new List<SceneMetadata>(128);
            lastScenesActiveInRings = new Dictionary<SceneRing, ExtendedScene>(rings.Length);
            scenesBySceneRings = new Dictionary<SceneRing, ExtendedScene[]>(rings.Length);
            Action onceOnline = () =>
            {
                for (int r = 0; r < rings.Length; r++)
                {
                    SceneRing ring = rings[r];
                    scenesBufferList.Clear();
                    for (int s = 0; s < SceneDatatable.metadata.Length; s++)
                    {
                        ExtendedScene extendedScene = GetExtendedScene(s);
                        if (extendedScene.metadata.sceneRing == ring) scenesBufferList.Add(extendedScene);
                    }
                    scenesBySceneRings[ring] = scenesBufferList.ToArray();
                    lastScenesActiveInRings[ring] = null;
                }
                LoadGlobalScenes();
                SceneManager.activeSceneChanged += (sceneA, sceneB) => { SyncActiveScene(); };
            };
            timing.RunCoroutineOnInstance(_WaitUntilOnline(onceOnline));
        }

        /// <summary>
        /// Fetch the appropriate ruleset for the given ExtendedScene to use when loading in.
        /// </summary>
        private Action<ExtendedScene>[] AcquireSceneRingRules(ExtendedScene loadingScene)
        {
            Action<ExtendedScene>[] ruleset = new Action<ExtendedScene>[0];
            switch (loadingScene.metadata.sceneRing)
            {
                case SceneRing.None:
                case SceneRing.GlobalScenes:
                    break;
                case SceneRing.SystemScenes:
                    ruleset = systemScenesRules;
                    break;
                case SceneRing.VenueScenes:
                    ruleset = venueScenesRules;
                    break;
                case SceneRing.WorldScenes:
                    Util.Crash(new NotImplementedException());
                    break;
                default:
                    Util.Crash(new Exception("Invalid ring for scene " + loadingScene.metadata.path + ": " + loadingScene.metadata.sceneRing));
                    break;
            }
            return ruleset;
        }

        /// <summary>
        /// Applies the scene unload/suspend rules to all loaded scenes based on the ring of the scene that'd being loaded in.
        /// This allows for automation of complex behaviors like (ex:) "unload all scenes in ring x when entering scene of ring y"
        /// or "load only one scene in ring z" or "when loading scenes from ring a, suspend all scenes in ring b but the last active one."
        /// </summary>
        public void ApplySceneRingRules(ExtendedScene loadingScene)
        {
            Action<ExtendedScene>[] ruleset = AcquireSceneRingRules(loadingScene);
            for (int r = 0; r < ruleset.Length; r++)
            {
                for (int s = 0; s < loadedScenes.Count; s++)
                {
                    if (loadedScenes[s] != loadingScene) ruleset[r](loadedScenes[s]);
                }
            }
        }

        /// <summary>
        /// Find an object that exists as a child of any loaded scene's root handle and return its transform.
        /// </summary>
        public Transform Find(string name)
        {
            for (int i = 0; i < loadedScenes.Count; i++)
            {
                if (!loadedScenes[i].hasRootHandle) continue;
                Transform child = loadedScenes[i].rootHandle.transform.Find(name);
                if (child != null) return child;
            }
            return null;
        }

        /// <summary>
        /// Gets the last active scene in the given scene ring.
        /// </summary>
        public ExtendedScene GetActiveScene(SceneRing ring)
        {
            return lastScenesActiveInRings[ring];
        }

        /// <summary>
        /// Take a scene path and return an ExtendedScene that corresponds to it.
        /// </summary>
        public ExtendedScene GetExtendedScene(int buildIndex)
        {
            if (extendedScenesArray[buildIndex] == null) CreateExtendedSceneForIndex(buildIndex);
            return extendedScenesArray[buildIndex];
        }

        /// <summary>
        /// Gets all scenes - loaded or unloaded - in the specified rings.
        /// </summary>
        public ExtendedScene[] GetAllScenesInRings(SceneRing ring)
        {
            scenesBufferList.Clear();
            for (int r = 1; r > int.MinValue; r = r << 1)
            {
                SceneRing testRing = (SceneRing)r;
                if ((ring & testRing) == testRing)
                {
                    ExtendedScene[] scenes = scenesBySceneRings[testRing];
                    for (int s = 0; s < scenes.Length; s++) scenesBufferList.Add(scenes[s]);
                }
            }
            return scenesBufferList.ToArray();
        }

        /// <summary>
        /// Gets all loaded scenes in the specified rings.
        /// </summary>
        public ExtendedScene[] GetLoadedScenesInRings(SceneRing ring)
        {
            scenesBufferList.Clear();
            for (int r = 1; r > int.MinValue; r = r << 1)
            {
                SceneRing testRing = (SceneRing)r;
                if ((ring & testRing) == testRing)
                {
                    ExtendedScene[] scenes = scenesBySceneRings[testRing];
                    for (int s = 0; s < scenes.Length; s++)
                    {
                        if (scenes[s].isLoaded) scenesBufferList.Add(scenes[s]);
                    }
                }
            }
            return scenesBufferList.ToArray();
        }

        /// <summary>
        /// Get the current progress of ExtendedSceneManager for all scenes it's loading or unloading.
        /// </summary>
        public float GetProgressOfLoad()
        {
            if (loadPhase == LoadPhase.NotLoading) return 1.0f;
            else
            {
                float r = 0;
                for (int i = 0; i < currentLoadingOps.Count; i++) r += currentLoadingOps[i].progress;
                r /= currentLoadingOps.Count + scenesToLoad.Count + scenesToUnload.Count;
                return r;
            }
        }

        /// <summary>
        /// Add the given extendedScene to the list of loaded scenes.
        /// </summary>
        public void MarkSceneLoaded(ExtendedScene extendedScene)
        {
            if (loadedScenes.Contains(extendedScene)) Util.Crash(new Exception(extendedScene.metadata.path + " is already loaded!"));
            loadedScenes.Add(extendedScene);
            timing.RunCoroutineOnInstance(Util._WaitOneFrame(LoadPhaseAdvance));
        }

        /// <summary>
        /// Removes the given extendedScene from the list of loaded scenes.
        /// </summary>
        public void MarkSceneUnloaded(ExtendedScene extendedScene)
        {
            if (!loadedScenes.Contains(extendedScene)) Util.Crash(new Exception(extendedScene.metadata.path + " isn't loaded!"));
            loadedScenes.Remove(extendedScene);
            timing.RunCoroutineOnInstance(Util._WaitOneFrame(LoadPhaseAdvance));
        }

        /// <summary>
        /// Sets active scene based on given extended scene.
        /// </summary>
        public void SetActiveScene(ExtendedScene extendedScene)
        {
            lastScenesActiveInRings[extendedScene.metadata.sceneRing] = extendedScene;
            SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(extendedScene.metadata.buildIndex));
        }

        /// <summary>
        /// Starts loading the given extendedScene and applies the scene unload rules for its ring.
        /// </summary>
        public void StageForLoading(ExtendedScene extendedScene)
        {
            if (loadPhase == LoadPhase.Unloading) Util.Crash(new Exception("Can't stage " + extendedScene.metadata.path + " to load because ExtendedSceneManager is in the unload phase!"));
            if (extendedScene.isLoaded) Util.Crash(new Exception(extendedScene.metadata.path + " is already loaded!"));
            if (extendedScene.metadata.buildIndex == 0) Util.Crash(new Exception("Can't reload Universe start scene!"));
            if (Debug.isDebugBuild && verbose) Debug.Log("Staged scene for loading: " + extendedScene.metadata.path + " in phase " + loadPhase);
            scenesToLoad.Enqueue(extendedScene);
            ApplySceneRingRules(extendedScene);
            if (!loading) LoadStarted();
        }

        /// <summary>
        /// Starts unloading the given extendedScene.
        /// </summary>
        public void StageForUnloading(ExtendedScene extendedScene)
        {
            if (!extendedScene.isLoaded) Util.Crash(new Exception(extendedScene.metadata.path + " isn't loaded!"));
            if (Debug.isDebugBuild && verbose) Debug.Log("Staged scene for unloading: " + extendedScene.metadata.path + " in phase " + loadPhase);
            if (extendedScene.hasRootHandle) extendedScene.SuspendScene();
            scenesToUnload.Enqueue(extendedScene);
            if (!loading) LoadStarted();
        }

        /// <summary>
        /// Coroutine: Calls _onCompletion once finished loading.
        /// </summary>
        public IEnumerator<float> _WaitUntilLoadComplete(Action onCompletion)
        {
            while (loading)
            {
                float progress = GetProgressOfLoad();
                if (progress >= 1.0f) break;
                yield return progress;
            }
            onCompletion();
        }

        /// <summary>
        /// Adds the extended scene to the datatables.
        /// </summary>
        private void AddExtendedSceneToArray(ExtendedScene extendedScene)
        {
            extendedScenesArray[extendedScene.metadata.buildIndex] = extendedScene;
        }

        /// <summary>
        /// Take a build index and return a new ExtendedScene that corresponds to it.
        /// The new ExtendedScene will be added to the dictionaries.
        /// </summary>
        private ExtendedScene CreateExtendedSceneForIndex(int buildIndex)
        {
            ExtendedScene extendedScene = new ExtendedScene(SceneDatatable.metadata[buildIndex]);
            AddExtendedSceneToArray(extendedScene);
            return extendedScene;
        }

        /// <summary>
        /// Get all scene metadata entries for the given single ring.
        /// </summary>
        private SceneMetadata[] GetAllMetadataEntriesForRing (SceneRing sceneRing)
        {
            sceneMetadataBufferList.Clear();
            for (int i = 0; i < SceneDatatable.metadata.Length; i++)
            {
                if (SceneDatatable.metadata[i].sceneRing == SceneRing.GlobalScenes) sceneMetadataBufferList.Add(SceneDatatable.metadata[i]);
            }
            return sceneMetadataBufferList.ToArray();
        }

        /// <summary>
        /// Called once we've finished a batch of loading operations.
        /// Either stages the next batch or marks us done, depending.
        /// We don't actually care about the arguments, but the sceneLoaded
        /// and sceneUnloaded delegates want this signature.
        /// </summary>
        private void LoadPhaseAdvance()
        {
            Action loadDone = () =>
            {
                if (GetProgressOfLoad() < 1.0f) Util.Crash(new Exception());
                if (loadPhase != LoadPhase.NotLoading)
                {
                    loadPhase = LoadPhase.NotLoading;
                    if (Debug.isDebugBuild && verbose) Debug.Log("Completed batch scene load/unload operations.");
                    if (EventSystem.current != null) EventSystem.current.enabled = true;
                }
            };
            Action<int> load = (count) =>
            {
                loadPhase = LoadPhase.Loading;
                for (int i = 0; i < count; i++)
                {
                    ExtendedScene extendedScene = scenesToLoad.Dequeue();
                    if (Debug.isDebugBuild && verbose) Debug.Log("Started loading: " + extendedScene.metadata.path);
                    currentLoadingOps.Add(SceneManager.LoadSceneAsync(extendedScene.metadata.buildIndex, LoadSceneMode.Additive));
                }
            };
            Action<int> unload = (count) =>
            {
                loadPhase = LoadPhase.Unloading;
                for (int i = 0; i < count; i++)
                {
                    ExtendedScene extendedScene = scenesToUnload.Dequeue();
                    if (Debug.isDebugBuild && verbose) Debug.Log("Started unloading: " + extendedScene.metadata.path);
                    currentLoadingOps.Add(SceneManager.UnloadSceneAsync(extendedScene.metadata.buildIndex));
                }
            };
            if (Debug.isDebugBuild && verbose) Debug.Log("Phase " + loadPhase + ", progress " + GetProgressOfLoad() + ", scenes in load queue " + scenesToLoad.Count +
                                                         ", scenes in unload queue" + scenesToUnload.Count + " operations in progress" + currentLoadingOps.Count);
            switch (loadPhase)
            {
                case LoadPhase.NotLoading:
                case LoadPhase.Loading:
                    if (scenesToLoad.Count > 0 || Util.AverageCompletionOfOps(currentLoadingOps.ToArray()) < 1.0f) load(scenesToLoad.Count);
                    else if (scenesToUnload.Count > 0) unload(scenesToUnload.Count);
                    else if (GetProgressOfLoad() >= 1.0f) loadDone();
                    break;
                case LoadPhase.Unloading:
                    if (scenesToUnload.Count > 0 || Util.AverageCompletionOfOps(currentLoadingOps.ToArray()) < 1.0f) unload(scenesToUnload.Count);
                    else if (scenesToLoad.Count > 0) load(scenesToLoad.Count);
                    else if (GetProgressOfLoad() >= 1.0f) loadDone();
                    break;
            }
        }

        /// <summary>
        /// Called when we start a batch of loading operations.
        /// </summary>
        private void LoadStarted()
        {
            if (Debug.isDebugBuild && verbose) Debug.Log("Started batch scene load operations.");
            if (EventSystem.current != null) EventSystem.current.enabled = false;
            LoadPhaseAdvance();
        }

        /// <summary>
        /// Loads in the global scenes.
        /// </summary>
        private void LoadGlobalScenes()
        {
            ExtendedScene[] globalScenes = scenesBySceneRings[SceneRing.GlobalScenes];
            for (int i = 0; i < globalScenes.Length; i++) globalScenes[i].StageForLoading();
            if (SceneManager.GetSceneAt(0).buildIndex == 0)
            {
                StageForLoading(GetExtendedScene(1));
                StageForUnloading(GetExtendedScene(0));
            }
        }

        /// <summary>
        /// Gets the currently active scene as an ExtendedScene. Syncs that with the ring-active scenes.
        /// </summary>
        private void SyncActiveScene()
        {
            ExtendedScene extendedScene = GetExtendedScene(SceneManager.GetActiveScene().buildIndex);
            if (GetActiveScene(extendedScene.metadata.sceneRing) != extendedScene) lastScenesActiveInRings[extendedScene.metadata.sceneRing] = extendedScene;
        }

        /// <summary>
        /// Coroutine: Waits until ExtendedSceneManager.Instance is set, then calls onCompletion.
        /// </summary>
        private IEnumerator<float> _WaitUntilOnline(Action onCompletion)
        {
            while (Instance == null) yield return 0;
            onCompletion();
        }
    }
}