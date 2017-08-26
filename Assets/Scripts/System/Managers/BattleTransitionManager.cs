using System;
using UnityEngine;
using Universe;
using CnfBattleSys;
using ExtendedSceneManagement;

/// <summary>
/// Manager that handles transitions between the battle scene
/// and out-of-battle scenes.
/// </summary>
public class BattleTransitionManager : Manager<BattleTransitionManager>
{
    /// <summary>
    /// Models a transition to/from the battle scene.
    /// </summary>
    private class BattleTransition
    {
        private readonly Action callback;

        public BattleTransition (Action _callback)
        {
            callback = _callback;
        }

        /// <summary>
        /// Resource load requests that need to finish before we can
        /// leave the loading screen are done.
        /// </summary>
        private bool resourcesReady;
        /// <summary>
        /// Scene load requests that need to finish before we can
        /// leave the loading screen are done.
        /// </summary>
        private bool scenesReady;

        /// <summary>
        /// We're no longer waiting on any resource or scene loads.
        /// Fire the callback.
        /// </summary>
        private void CompleteTransition ()
        {
            callback?.Invoke();
        }

        /// <summary>
        /// Indicate that we no longer need to wait on resource loading.
        /// </summary>
        public void ResourceLoadsFinished ()
        {
            resourcesReady = true;
            if (scenesReady) CompleteTransition();
        }

        /// <summary>
        /// Indicate that we no longer need to wait on scene loading.
        /// </summary>
        public void SceneLoadsFinished ()
        {
            scenesReady = true;
            if (resourcesReady) CompleteTransition();
        }
    }

    public enum State
    {
        None,
        OutOfBattle,
        TransitioningToBattle,
        InBattle
    }
    public bool systemAndVenueScenesActive { get { return battleScene.isLoaded && venueScene.isLoaded; } }
    public State state { get; private set; }
    private GameObject lastNonBattleScene_rootGO;
    private ExtendedScene battleScene;
    private ExtendedScene prebattleScene;
    private ExtendedScene testMenuScene;
    private ExtendedScene venueScene;
    private BattleTransition current;

    /// <summary>
    /// MonoBehaviour.Awake ()
    /// </summary>
    void Awake ()
    {
        state = State.OutOfBattle;
    }

    /// <summary>
    /// Suspends out-of-battle scene, loads battle scene and venue scene, and
    /// prepares to start the battle once those are done.
    /// </summary>
    public void EnterBattleScene (BattleFormation formation)
    {
        Action transitionCallback = () =>
        {
            venueScene.SetAsActiveScene();
            state = State.InBattle;
            BattleOverseer.StartBattle();
            current = null;
        };
        if (battleScene == null) GetSceneRefs();
        if (state != State.OutOfBattle) Util.Crash(new Exception("Can't enter battle scene: scenechangemanager state is " + state.ToString()));
        prebattleScene = ExtendedSceneManager.Instance.GetActiveScene(SceneRing.WorldScenes);
        if (prebattleScene != null) prebattleScene.SuspendScene();
        venueScene = GetSceneForVenue(formation.venue);
        battleScene.StageForLoading();
        venueScene.StageForLoading();
        state = State.TransitioningToBattle;
        BattleOverseer.PrepareBattle(formation);
        BattleStage.LoadResources(null);
        current = new BattleTransition(transitionCallback);
        if (ExtendedSceneManager.Instance.loading) ExtendedSceneManager.Instance.onBatchedSceneLoadsComplete += current.SceneLoadsFinished;
        else current.SceneLoadsFinished();
        if (ResourceLoadManager.Instance.loading) ResourceLoadManager.Instance.onBatchedResourceLoadsComplete += current.ResourceLoadsFinished;
        else current.ResourceLoadsFinished();
        LoadingScreen.DisplayWithShade();
    }

    /// <summary>
    /// Unsuspends out-of-battle scene and exits battle scene.
    /// </summary>
    public void ReturnFromBattleScene ()
    {
        Util.Crash(new NotImplementedException());
    }

    /// <summary>
    /// Enter the battle test menu.
    /// </summary>
    public void EnterBattleTestMenu ()
    {
        Action transitionCallback = () =>
        {
            testMenuScene.SetAsActiveScene();
        };
        if (testMenuScene == null) GetSceneRefs();
        state = State.OutOfBattle;
        testMenuScene.StageForLoading();
        ExtendedSceneManager.Instance.onBatchedSceneLoadsComplete += transitionCallback;
        LoadingScreen.DisplayWithShade();
    }

    /// <summary>
    /// Returns the scene associated with the given VenueType.
    /// </summary>
    private ExtendedScene GetSceneForVenue(VenueType venue)
    {
        ExtendedScene[] venueScenes = ExtendedSceneManager.Instance.GetAllScenesInRings(SceneRing.VenueScenes);
        for (int i = 0; i < venueScenes.Length; i++)
        {
            if (venueScenes[i].metadata.name == venue.ToString()) return venueScenes[i];
        }
        Util.Crash("No venue scene for venue type of " + venue);
        return null;
    }

    /// <summary>
    /// Gets references to ExtendedScenes from the manager.
    /// </summary>
    private void GetSceneRefs ()
    {
        battleScene = ExtendedSceneManager.Instance.GetExtendedScene(SceneDatatable.BattleSystemScene.buildIndex);
        testMenuScene = ExtendedSceneManager.Instance.GetExtendedScene(SceneDatatable.TestMenuScene.buildIndex);
    }
}
