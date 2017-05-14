using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Universe;
using CnfBattleSys;
using MovementEffects;

/// <summary>
/// Manager that handles transitions between scenes.
/// We use this to do things like go from an "outside" scene
/// to the battle scene and back without having
/// messy scene-handling logic elsewhere.
/// </summary>
public class BattleSceneManager : Manager<BattleSceneManager>
{
    public enum State
    {
        None,
        OutOfBattle,
        TransitioningToBattle,
        InBattle
    }
    private GameObject lastNonBattleScene_rootGO;
    private Scene battleScene;
    private Scene sceneBattleEnteredFrom;
    private Scene venueScene;
    private State state = State.OutOfBattle;
    private const string battleSystemScenePath = "Scenes/BattleSystemScene";
    private const string venueScenesPath = "Scenes/BattleVenues/";

    /// <summary>
    /// Suspends out-of-battle scene, loads battle scene and venue scene, and
    /// prepares to start the battle once those are done.
    /// </summary>
    public void EnterBattleScene (BattleFormation formation)
    {
        if (state != State.OutOfBattle) throw new Exception("Can't enter battle scene: scenechangemanager state is " + state.ToString());
        sceneBattleEnteredFrom = SceneManager.GetActiveScene();
        lastNonBattleScene_rootGO = GameObject.Find("GameObjectsRoot");
        if (lastNonBattleScene_rootGO != null) lastNonBattleScene_rootGO.SetActive(false); // if there's a root gameobject in the scene, set it inactive - we won't update or render any objects in the underlying scene.
        else throw new Exception("No root gameobject in scene " + sceneBattleEnteredFrom.path + ", so we can't suspend it to enter a battle.");
        LoadingScreen.instance.DisplayWithShade(LoadScenesForBattle(formation.venue));
        Timing.RunCoroutine(_StartBattleWhenReady(formation));
    }
    
    /// <summary>
    /// Unsuspends out-of-battle scene and exits battle scene.
    /// </summary>
    public void ReturnFromBattleScene ()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Loads in battle system and venue scenes.
    /// </summary>
    private AsyncOperation[] LoadScenesForBattle (VenueType venue)
    {
        AsyncOperation venueUnloading = null;
        if (venueScene.path != null) venueUnloading = SceneManager.UnloadSceneAsync(venueScene);
        AsyncOperation battleSysLoading = SceneManager.LoadSceneAsync(battleSystemScenePath, LoadSceneMode.Additive);
        AsyncOperation venueLoading = SceneManager.LoadSceneAsync(venueScenesPath + venue.ToString(), LoadSceneMode.Additive);
        AsyncOperation[] returnArray;
        if (venueUnloading != null) returnArray = new AsyncOperation[] { venueUnloading, venueLoading, battleSysLoading };
        else returnArray = new AsyncOperation[] { venueLoading, battleSysLoading };
        return returnArray;
    }

    /// <summary>
    /// Coroutine: Starts the battle once loading is done.
    /// </summary>
    private IEnumerator<float> _StartBattleWhenReady (BattleFormation formation)
    {
        state = State.TransitioningToBattle;
        while (LoadingScreen.instance.progress < 1.0f) yield return LoadingScreen.instance.progress;
        battleScene = SceneManager.GetSceneByPath("Assets/" + battleSystemScenePath + ".unity");
        venueScene = SceneManager.GetSceneByPath("Assets/" + venueScenesPath + formation.venue.ToString() + ".unity");
        SceneManager.SetActiveScene(battleScene);
        BattleOverseer.StartBattle(formation);
        state = State.InBattle;
    }
}
