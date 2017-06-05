using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ExtendedSceneManagement
{
    /// <summary>
    /// Class that incorporates supplemental functionality to extend base Unity Scene structure.
    /// </summary>
    public class ExtendedScene
    {
        public readonly SceneMetadata metadata;
        public bool hasRootHandle { get { if (!isLoaded) Util.Crash(new Exception("Can't tell if " + metadata.path + " has a root handle because it isn't loaded")); return rootHandle != null; } }
        public bool isLoaded { get { return sceneData.isLoaded; } }
        public bool isValid { get { return SceneUtility.GetBuildIndexByScenePath(metadata.path) > -1; } } // Scene.IsValid() is broken. This is probably slower, but it isn't broken.
        public bool suspended { get { if (!hasRootHandle) Util.Crash(new Exception("Can't suspend/unsuspend" + metadata.path + " because it doesn't have a root handle")); return rootHandle.activeInHierarchy; } }
        public GameObject rootHandle { get; private set; }
        /// <summary>
        /// This actually generates a new Scene struct every time you invoke it.
        /// That's kinda ugly but it's necessary because of Scene's weird-ass behavior.
        /// Still, bear that in mind: if you're going to be doing multiple operations
        /// on sceneData at once, best to store it in a local variable
        /// instead of eating a chunk of memory each time you call it.
        /// </summary>
        private Scene sceneData { get { return SceneManager.GetSceneByBuildIndex(metadata.buildIndex); } }

        /// <summary>
        /// Gets scene struct and sets up ExtendedScene based on a given Scene struct.
        /// If there's only one root game object in the scene, the ExtendedScene will take that
        /// as a root handle, allowing you to use the scene suspend/unsuspend functions.
        /// </summary>
        public ExtendedScene(SceneMetadata _metadata)
        {
            metadata = _metadata;
            if (isLoaded)
            {
                AssignRootGO();
                ExtendedSceneManager.Instance.MarkSceneLoaded(this);
            }
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        /// <summary>
        /// Delegate that fires for each ExtendedScene when a scene is loaded.
        /// </summary>
        private void OnSceneLoaded(Scene loadedScene, LoadSceneMode loadSceneMode)
        {
            if (loadedScene.buildIndex == metadata.buildIndex)
            {
                AssignRootGO();
                ExtendedSceneManager.Instance.MarkSceneLoaded(this);
            }
        }

        /// <summary>
        /// Delegate that fires for each ExtendedScene when a scene is unloaded.
        /// </summary>
        private void OnSceneUnloaded(Scene unloadedScene)
        {
            if (unloadedScene.buildIndex == metadata.buildIndex)
            {
                rootHandle = null;
                ExtendedSceneManager.Instance.MarkSceneUnloaded(this);
            }
        }

        /// <summary>
        /// Sets this scene as the active scene.
        /// </summary>
        public void SetAsActiveScene()
        {
            ExtendedSceneManager.Instance.SetActiveScene(this);
        }

        /// <summary>
        /// Suspends the scene, if it has a root handle.
        /// </summary>
        public void SuspendScene()
        {
            if (!isLoaded) Util.Crash(new Exception("Can't perform scene suspend/unsuspend operations on scene " + metadata.path + " because it's not loaded."));
            if (!hasRootHandle) Util.Crash(new Exception("Can't perform scene suspend/unsuspend operations on scene " + metadata.path + " because it doesn't have a single root gameobject."));
            rootHandle.SetActive(false);
        }

        /// <summary>
        /// Starts loading this scene and applies the scene unload rules for its ring.
        /// </summary>
        public void StageForLoading()
        {
            ExtendedSceneManager.Instance.StageForLoading(this);
        }

        /// <summary>
        /// Starts unloading this scene.
        /// </summary>
        public void StageForUnloading()
        {
            ExtendedSceneManager.Instance.StageForUnloading(this);
        }

        /// <summary>
        /// Unsuspends the scene, if it has a root handle.
        /// </summary>
        public void UnsuspendScene()
        {
            if (!isLoaded) Util.Crash(new Exception("Can't perform scene suspend/unsuspend operations on scene " + metadata.path + " because it's not loaded."));
            if (!hasRootHandle) Util.Crash(new Exception("Can't perform scene suspend/unsuspend operations on scene " + metadata.path + " because it doesn't have a single root gameobject."));
            rootHandle.SetActive(true);
        }

        /// <summary>
        /// Puts path strings in the format scene manager wants for GetSceneByPath, etc.
        /// </summary>
        public static string ConvertPath(string path)
        {
            return "Assets/" + path + ".unity";
        }

        /// <summary>
        /// Puts path strings back in a standard Unity asset path format.
        /// </summary>
        public static string DeconvertPath(string path)
        {
            return path.Replace("Assets/", "").Replace(".unity", "");
        }

        /// <summary>
        /// Gets and assigns root gameobject, if possible.
        /// </summary>
        private void AssignRootGO()
        {
            GameObject[] roots = sceneData.GetRootGameObjects();
            if (roots.Length == 1) rootHandle = roots[0];
            else rootHandle = null;
            if (rootHandle != null)
            {
                rootHandle.name = "[Root] " + sceneData.name;
                rootHandle.tag = "Untagged";
            }
            else if (metadata.buildIndex != 0) // universe scene doesn't need a root. every other scene does.
            {
                Util.Crash(new Exception("No root handle in scene " + metadata.path));
            }
        }
    }
}