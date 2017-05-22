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
        None,
        LoadIn,
        Unload
    }
    /// <summary>
    /// Enables detailed log output from the ExtendedSceneManager,
    /// if this is also a debug build. (If you're not debugging the ExtendedSceneManager,
    /// you can set verbose = false here instead to avoid polluting the log.)
    /// </summary>
    public const bool verbose = true;
    public bool loading { get { return loadPhase == LoadPhase.LoadIn || loadPhase == LoadPhase.Unload; } }
    public LoadPhase loadPhase { get; private set; }
    private Dictionary<SceneRing, ExtendedScene> lastScenesActiveInRings;
    private Dictionary<SceneRing, List<int>> sceneIndicesBySceneRings;
    private ExtendedScene[] extendedScenesArray;
    private List<AsyncOperation> currentLoadingOps;
    private List<ExtendedScene> loadedScenes;
    private List<ExtendedScene> scenesToUnload;
    private Timing myTiming;
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
        currentLoadingOps = new List<AsyncOperation>();
        extendedScenesArray = new ExtendedScene[SceneManager.sceneCountInBuildSettings];
        loadedScenes = new List<ExtendedScene>();
        scenesToUnload = new List<ExtendedScene>();
        lastScenesActiveInRings = new Dictionary<SceneRing, ExtendedScene>(rings.Length);     
        sceneIndicesBySceneRings = new Dictionary<SceneRing, List<int>>(rings.Length);
        myTiming = gameObject.AddComponent<Timing>();
        for (int i = 0; i < rings.Length; i++)
        {
            sceneIndicesBySceneRings[rings[i]] = new List<int>();
            lastScenesActiveInRings[rings[i]] = null;
        }
        LoadGlobalScenes();
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
        return GetExtendedScene(SceneManager.GetActiveScene().buildIndex);
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
        switch (loadPhase)
        {
            case LoadPhase.LoadIn:
                float r = 0;
                for (int i = 0; i < currentLoadingOps.Count; i++) r += currentLoadingOps[i].progress;
                r /= currentLoadingOps.Count + scenesToUnload.Count; // we haven't started unloading anything yet, but they're staged, so treat them like asyncoperations that just haven't made any progress
                return r;
            case LoadPhase.Unload:
                return Util.AverageCompletionOfOps(currentLoadingOps.ToArray());
            default:
                return 1.0f;
        }
    }

    /// <summary>
    /// Add the given extendedScene to the list of loaded scenes.
    /// </summary>
    public void MarkSceneLoaded (ExtendedScene extendedScene)
    {
        if (loadedScenes.Contains(extendedScene)) Util.Crash(new Exception(extendedScene.path + " is already loaded!"));
        loadedScenes.Add(extendedScene);
    }

    /// <summary>
    /// Removes the given extendedScene from the list of loaded scenes.
    /// </summary>
    public void MarkSceneUnloaded (ExtendedScene extendedScene)
    {
        if (!loadedScenes.Contains(extendedScene)) Util.Crash(new Exception(extendedScene.path + " isn't loaded!"));
        loadedScenes.Remove(extendedScene);
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
        if (loadPhase == LoadPhase.Unload) Util.Crash(new Exception("Can't stage " + extendedScene.path + " to load because ExtendedSceneManager is in the unload phase!"));
        if (extendedScene.isLoaded) Util.Crash(new Exception(extendedScene.metadata.path + " is already loaded!"));
        if (extendedScene.buildIndex == 0) Util.Crash(new Exception("Can't reload Universe start scene!"));
        if (Debug.isDebugBuild && verbose) Debug.Log("Staged scene for loading: " + extendedScene.path);
        currentLoadingOps.Add(SceneManager.LoadSceneAsync(extendedScene.buildIndex, LoadSceneMode.Additive));
        ApplySceneRingRules(extendedScene);
        if (!loading) LoadStarted();
    }

    /// <summary>
    /// Starts unloading the given extendedScene.
    /// </summary>
    public void StageForUnloading (ExtendedScene extendedScene)
    {
        if (Debug.isDebugBuild && verbose) Debug.Log("Staged scene for unloading: " + extendedScene.path + " in phase " + loadPhase.ToString());
        if (!extendedScene.isLoaded) Util.Crash(new Exception(extendedScene.metadata.path + " isn't loaded!"));
        if (extendedScene.hasRootHandle) extendedScene.SuspendScene();
        if (loadPhase == LoadPhase.Unload) currentLoadingOps.Add(SceneManager.LoadSceneAsync(extendedScene.buildIndex));
        else scenesToUnload.Add(extendedScene);
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
    /// </summary>
    private void LoadCompleted ()
    {
        loadPhase = LoadPhase.None;
        if (Debug.isDebugBuild && verbose) Debug.Log("Completed batch scene load/unload operations.");
        if (EventSystem.current != null) EventSystem.current.enabled = true;
        currentLoadingOps.Clear();
    }

    /// <summary>
    /// Called when we finish loading scenes in and can start unloading them.
    /// </summary>
    private void LoadTransitionsToUnload ()
    {
        loadPhase = LoadPhase.Unload;
        for (int i = 0; i < scenesToUnload.Count; i++)
        {
            if (Debug.isDebugBuild && verbose) Debug.Log("Dispatching unload operation for " + scenesToUnload[i].path);
            currentLoadingOps.Add(SceneManager.UnloadSceneAsync(scenesToUnload[i].buildIndex));
        }
        scenesToUnload.Clear();
    }

    /// <summary>
    /// Called when we start a batch of loading operations.
    /// </summary>
    private void LoadStarted ()
    {
        loadPhase = LoadPhase.LoadIn;
        if (Debug.isDebugBuild && verbose) Debug.Log("Started batch scene load operations."); 
        if (EventSystem.current != null) EventSystem.current.enabled = false;
        myTiming.RunCoroutineOnInstance(_WaitUntilPossibleThenStartUnloading());
        myTiming.RunCoroutineOnInstance(_WaitUntilLoadComplete(LoadCompleted));
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
        };
        myTiming.RunCoroutineOnInstance(_WaitUntilOnline(onCompletion));
    }  

    /// <summary>
    /// Coroutine: Waits until ExtendedSceneManager.Instance is set, then calls onCompletion.
    /// </summary>
    private IEnumerator<float> _WaitUntilOnline (Action onCompletion)
    {
        while (Instance == null) yield return 0;
        onCompletion();
    }

    /// <summary>
    /// Coroutine: wait until we finish the current set of loading ops, then call LoadTransitionsToUnload()
    /// This specifically doesn't use GetProgressOfLoad because it's here
    /// to let us start handling the load phase first, wait until those are done loading in, and only then
    /// start unloading.
    /// </summary>
    private IEnumerator<float> _WaitUntilPossibleThenStartUnloading ()
    {
        float realProgress = 0;
        while (realProgress < 1.0f)
        {
            realProgress = Util.AverageCompletionOfOps(currentLoadingOps.ToArray());
            yield return 0;
        }
        if (loadPhase != LoadPhase.LoadIn) Util.Crash(new Exception("Was waiting to transition to unload phase, but wasn't in load phase when that became possible."));
        LoadTransitionsToUnload();
    }
}
