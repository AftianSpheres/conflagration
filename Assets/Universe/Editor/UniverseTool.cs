using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Reflection;
using UnityEngine.SceneManagement;

namespace Universe
{
    /// <summary>
    /// This simple tool is there to guaranty that one Universe exist at all time.
    /// Should a new Manager be found, it saves it as an Asset.
    /// </summary>
    [InitializeOnLoad]
    public class UniverseTool
    {
        static UniverseTool()
        {
            EditorApplication.playmodeStateChanged += PlaymodeStateChanged;
            PlaymodeStateChanged();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (typeof(IManager).IsAssignableFrom(type) && !type.IsAbstract)
                    {
                        GameObject go = Resources.Load(Universe.PATH + type.Name) as GameObject;
                        if (go != null)
                            continue;

                        go = new GameObject(type.Name);
                        go.AddComponent(type);

                        DirectoryInfo dir = new DirectoryInfo(Application.dataPath + "/Resources/Universe/");
                        if (!dir.Exists)
                            dir.Create();

                        PrefabUtility.CreatePrefab("Assets/Resources/Universe/" + type.Name + ".prefab", go.gameObject);
                        GameObject.DestroyImmediate(go);
                    }
                }
            }
        }

        private static void PlaymodeStateChanged()
        {
            if (Application.isPlaying)
            {
                if (SceneManager.GetActiveScene().buildIndex != 0)
                {
                    GameObject go = new GameObject("Universe");
                    go.AddComponent<Universe>();
                }
            }
        }
    }
}