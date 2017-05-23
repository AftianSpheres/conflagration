using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Universe;
using CnfBattleSys;
using MovementEffects;

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
        public static Action<ExtendedScene> InRings (SceneRing comparison, Action<ExtendedScene> action)
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
        public static Action<ExtendedScene> InRingsAndActive (SceneRing comparison, Action<ExtendedScene> action)
        {
            return (extendedScene) =>
            {
                if ((comparison & extendedScene.metadata.sceneRing) == comparison && Instance.GetActiveSceneInRing(extendedScene.metadata.sceneRing) == extendedScene) action(extendedScene);
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
                if ((comparison & extendedScene.metadata.sceneRing) == comparison && Instance.GetActiveSceneInRing(extendedScene.metadata.sceneRing) != extendedScene) action(extendedScene);
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
    public const bool verbose = true;
    public bool loading { get { return loadPhase == LoadPhase.Loading || loadPhase == LoadPhase.Unloading; } }
    public LoadPhase loadPhase { get; private set; }
    private Dictionary<SceneRing, ExtendedScene> lastScenesActiveInRings;
    private Dictionary<SceneRing, List<int>> sceneIndicesBySceneRings;
    private ExtendedScene[] extendedScenesArray;
    private List<AsyncOperation> currentLoadingOps;
    private List<ExtendedScene> loadedScenes;
    private Queue<ExtendedScene> scenesToLoad;
    private Queue<ExtendedScene> scenesToUnload;
    private Timing timing;
    private Action<ExtendedScene>[] systemScenesRules = { Rules.InRings(SceneRing.SystemScenes, scene => { scene.StageForUnloading(); }) };
    private Action<ExtendedScene>[] venueScenesRules =  { Rules.InRingsAndInactive(SceneRing.WorldScenes, scene => { scene.StageForUnloading(); }),
                                                          Rules.InRingsAndActive(SceneRing.WorldScenes, scene => { scene.SuspendScene(); }) };

    /// <summary>
    /// MonoBehaviour.Awake()
    /// </summary>
    void Awake ()
    {
        SceneRing[] rings = (SceneRing[])Enum.GetValues(typeof(SceneRing));
        SceneDatatable.Bootstrap();
        BattleOverseer.FirstRunSetup();
        timing = gameObject.AddComponent<Timing>();
        currentLoadingOps = new List<AsyncOperation>();
        extendedScenesArray = new ExtendedScene[SceneManager.sceneCountInBuildSettings];
        loadedScenes = new List<ExtendedScene>();
        scenesToLoad = new Queue<ExtendedScene>();
        scenesToUnload = new Queue<ExtendedScene>();
        lastScenesActiveInRings = new Dictionary<SceneRing, ExtendedScene>(rings.Length);     
        sceneIndicesBySceneRings = new Dictionary<SceneRing, List<int>>(rings.Length);
        for (int i = 0; i < rings.Length; i++)
        {
            sceneIndicesBySceneRings[rings[i]] = new List<int>();
            lastScenesActiveInRings[rings[i]] = null;
        }
        LoadGlobalScenes();

        // Use these delegates to keep ExtendedSceneManager synchronized with the built-in scene manager.
        // We don't actually care about the arguments in any of these cases (and we throw away GetActiveScene()'s return value)
        // but we need the housekeeping to get done.

        SceneManager.activeSceneChanged += (sceneA, sceneB) => { GetActiveScene(); };
    }

#if UNITY_EDITOR
    /// <summary>
    /// MonoBehaviour.Start()
    /// </summary>
    void Start()
    {
        // Since hitting the play button won't generate an ExtendedScene for the loaded scene...
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (extendedScenesArray[scene.buildIndex] == null && SceneMetadata.array[scene.buildIndex].sceneRing != SceneRing.None)
            {
                Debug.Log("Rescue ExtendedScene created for " + GetExtendedScene(scene.buildIndex).path + ". (This is normal.)");              
            }
        }
    }
#endif

    /// <summary>
    /// Fetch the appropriate ruleset for the given ExtendedScene to use when loading in.
    /// </summary>
    private Action<ExtendedScene>[] AcquireSceneRingRules (ExtendedScene loadingScene)
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
                Util.Crash(new Exception("Invalid ring for scene " + loadingScene.path + ": " + loadingScene.metadata.sceneRing));
                break;
        }
        return ruleset;
    }

    /// <summary>
    /// Applies the scene unload/suspend rules to all loaded scenes based on the ring of the scene that'd being loaded in.
    /// This allows for automation of complex behaviors like (ex:) "unload all scenes in ring x when entering scene of ring y"
    /// or "load only one scene in ring z" or "when loading scenes from ring a, suspend all scenes in ring b but the last active one."
    /// </summary>
    public void ApplySceneRingRules (ExtendedScene loadingScene)
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
    /// Gets the currently active scene and returns it as an ExtendedScene.
    /// </summary>
    public ExtendedScene GetActiveScene ()
    {
        ExtendedScene extendedScene = GetExtendedScene(SceneManager.GetActiveScene().buildIndex);
        if (GetActiveSceneInRing(extendedScene.metadata.sceneRing) != extendedScene) lastScenesActiveInRings[extendedScene.metadata.sceneRing] = extendedScene;
        return extendedScene;
    }

    /// <summary>
    /// Gets the last active scene in the given scene ring.
    /// </summary>
    public ExtendedScene GetActiveSceneInRing (SceneRing ring)
    {
        return lastScenesActiveInRings[ring];
    }

    /// <summary>
    /// Take a scene path and return an ExtendedScene that corresponds to it.
    /// </summary>
    public ExtendedScene GetExtendedScene (int buildIndex)
    {
        if (extendedScenesArray[buildIndex] == null) CreateExtendedSceneForIndex(buildIndex);
        return extendedScenesArray[buildIndex];
    }

    /// <summary>
    /// Take a scene path and return an ExtendedScene that corresponds to it.
    /// </summary>
    public ExtendedScene GetExtendedScene (string path)
    {
        return GetExtendedScene(SceneUtility.GetBuildIndexByScenePath(ExtendedScene.ConvertPath(path)));
    }

    /// <summary>
    /// Get the current progress of ExtendedSceneManager for all scenes it's loading or unloading.
    /// </summary>
    public float GetProgressOfLoad ()
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
    public void MarkSceneLoaded (ExtendedScene extendedScene)
    {
        if (loadedScenes.Contains(extendedScene)) Util.Crash(new Exception(extendedScene.path + " is already loaded!"));
        loadedScenes.Add(extendedScene);
        timing.RunCoroutineOnInstance(Util._WaitOneFrame(LoadPhaseAdvance));
    }

    /// <summary>
    /// Removes the given extendedScene from the list of loaded scenes.
    /// </summary>
    public void MarkSceneUnloaded (ExtendedScene extendedScene)
    {
        if (!loadedScenes.Contains(extendedScene)) Util.Crash(new Exception(extendedScene.path + " isn't loaded!"));
        loadedScenes.Remove(extendedScene);
        timing.RunCoroutineOnInstance(Util._WaitOneFrame(LoadPhaseAdvance));
    }

    /// <summary>
    /// Sets active scene based on given extended scene.
    /// </summary>
    public void SetActiveScene (ExtendedScene extendedScene)
    {
        lastScenesActiveInRings[extendedScene.metadata.sceneRing] = extendedScene;
        SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(extendedScene.buildIndex));
    }

    /// <summary>
    /// Starts loading the given extendedScene and applies the scene unload rules for its ring.
    /// </summary>
    public void StageForLoading (ExtendedScene extendedScene)
    {
        if (loadPhase == LoadPhase.Unloading) Util.Crash(new Exception("Can't stage " + extendedScene.path + " to load because ExtendedSceneManager is in the unload phase!"));
        if (extendedScene.isLoaded) Util.Crash(new Exception(extendedScene.metadata.path + " is already loaded!"));
        if (extendedScene.buildIndex == 0) Util.Crash(new Exception("Can't reload Universe start scene!"));
        if (Debug.isDebugBuild && verbose) Debug.Log("Staged scene for loading: " + extendedScene.path + " in phase " + loadPhase);
        scenesToLoad.Enqueue(extendedScene);
        ApplySceneRingRules(extendedScene);
        if (!loading) LoadStarted();
    }

    /// <summary>
    /// Starts unloading the given extendedScene.
    /// </summary>
    public void StageForUnloading (ExtendedScene extendedScene)
    {
        if (!extendedScene.isLoaded) Util.Crash(new Exception(extendedScene.metadata.path + " isn't loaded!"));
        if (Debug.isDebugBuild && verbose) Debug.Log("Staged scene for unloading: " + extendedScene.path + " in phase " + loadPhase);
        if (extendedScene.hasRootHandle) extendedScene.SuspendScene();
        scenesToUnload.Enqueue(extendedScene);
        if (!loading) LoadStarted();
    }

    /// <summary>
    /// Coroutine: Calls _onCompletion once finished loading.
    /// </summary>
    public IEnumerator<float> _WaitUntilLoadComplete (Action onCompletion)
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
    /// Take a build index and return a new ExtendedScene that corresponds to it.
    /// The new ExtendedScene will be added to the dictionaries.
    /// </summary>
    private ExtendedScene CreateExtendedSceneForIndex(int buildIndex)
    {
        ExtendedScene extendedScene = new ExtendedScene(SceneUtility.GetScenePathByBuildIndex(buildIndex));
        AddExtendedSceneToTables(extendedScene);
        return extendedScene;
    }

    /// <summary>
    /// Adds the extended scene to the datatables.
    /// </summary>
    private void AddExtendedSceneToTables (ExtendedScene extendedScene)
    {
        extendedScenesArray[extendedScene.buildIndex] = extendedScene;
        sceneIndicesBySceneRings[extendedScene.metadata.sceneRing].Add(extendedScene.buildIndex);
    }

    /// <summary>
    /// Called once we've finished a batch of loading operations.
    /// Either stages the next batch or marks us done, depending.
    /// We don't actually care about the arguments, but the sceneLoaded
    /// and sceneUnloaded delegates want this signature.
    /// </summary>
    private void LoadPhaseAdvance ()
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
                if (Debug.isDebugBuild && verbose) Debug.Log("Started loading: " + extendedScene.path);
                currentLoadingOps.Add(SceneManager.LoadSceneAsync(extendedScene.buildIndex, LoadSceneMode.Additive));
            }
        };
        Action<int> unload = (count) =>
        {
            loadPhase = LoadPhase.Unloading;
            for (int i = 0; i < count; i++)
            {
                ExtendedScene extendedScene = scenesToUnload.Dequeue();
                if (Debug.isDebugBuild && verbose) Debug.Log("Started unloading: " + extendedScene.path);
                currentLoadingOps.Add(SceneManager.UnloadSceneAsync(extendedScene.buildIndex));
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
    private void LoadStarted ()
    {
        if (Debug.isDebugBuild && verbose) Debug.Log("Started batch scene load operations."); 
        if (EventSystem.current != null) EventSystem.current.enabled = false;
        LoadPhaseAdvance();
    }

    /// <summary>
    /// Loads in the global scenes.
    /// </summary>
    private void LoadGlobalScenes ()
    {
        SceneMetadata[] globalScenes = SceneDatatable.GlobalScenes.GetAll();
        Action onCompletion = () =>
        {
            for (int i = 0; i < globalScenes.Length; i++) GetExtendedScene(globalScenes[i].path).StageForLoading();
            if (SceneManager.GetSceneAt(0).buildIndex == 0)
            {
                StageForLoading(GetExtendedScene(1));
                StageForUnloading(GetExtendedScene(0));
            }
        };
        timing.RunCoroutineOnInstance(_WaitUntilOnline(onCompletion));
    }

    /// <summary>
    /// Coroutine: Waits until ExtendedSceneManager.Instance is set, then calls onCompletion.
    /// </summary>
    private IEnumerator<float> _WaitUntilOnline (Action onCompletion)
    {
        while (Instance == null) yield return 0;
        onCompletion();
    }
}
