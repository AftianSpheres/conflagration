using System;
using UnityEngine;
using Universe;
using CnfBattleSys;
using MovementEffects;

/// <summary>
/// Manager that handles transitions between the battle scene
/// and out-of-battle scenes.
/// </summary>
public class BattleTransitionManager : Manager<BattleTransitionManager>
{
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
    private Timing myTiming;


    /// <summary>
    /// MonoBehaviour.Awake ()
    /// </summary>
    void Awake ()
    {
        state = State.OutOfBattle;
        myTiming = gameObject.AddComponent<Timing>();   
    }

    /// <summary>
    /// Suspends out-of-battle scene, loads battle scene and venue scene, and
    /// prepares to start the battle once those are done.
    /// </summary>
    public void EnterBattleScene (BattleFormation formation)
    {
        Action onCompletion = () =>
        {
            battleScene.SetAsActiveScene();
            BattleOverseer.StartBattle(formation);
            state = State.InBattle;
        };
        if (battleScene == null) GetSceneRefs();
        if (state != State.OutOfBattle) Util.Crash(new Exception("Can't enter battle scene: scenechangemanager state is " + state.ToString()));
        prebattleScene = ExtendedSceneManager.Instance.GetActiveSceneInRing(SceneRing.WorldScenes);
        if (prebattleScene != null) prebattleScene.SuspendScene();
        venueScene = ExtendedSceneManager.Instance.GetExtendedScene(SceneDatatable.VenueScenes.Get(formation.venue).path);
        battleScene.StageForLoading();
        venueScene.StageForLoading();
        state = State.TransitioningToBattle;
        ToLoadingScreen(onCompletion);
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
        Action onCompletion = () =>
        {
            testMenuScene.SetAsActiveScene();
        };
        if (testMenuScene == null) GetSceneRefs();
        state = State.OutOfBattle;
        testMenuScene.StageForLoading();
        ToLoadingScreen(onCompletion);
    }

    /// <summary>
    /// Gets references to ExtendedScenes from the manager.
    /// </summary>
    private void GetSceneRefs ()
    {
        battleScene = ExtendedSceneManager.Instance.GetExtendedScene(SceneDatatable.SystemScenes.battleSystem.path);
        testMenuScene = ExtendedSceneManager.Instance.GetExtendedScene(SceneDatatable.SystemScenes.testMenu.path);
    }

    /// <summary>
    /// Pulls up loading screen and runs the given action once the ExtendedSceneManager has finished loading.
    /// </summary>
    private void ToLoadingScreen(Action onCompletion)
    {
        LoadingScreen.instance.DisplayWithShade();
        myTiming.RunCoroutineOnInstance(ExtendedSceneManager.Instance._WaitUntilLoadComplete(onCompletion));
    }
}
