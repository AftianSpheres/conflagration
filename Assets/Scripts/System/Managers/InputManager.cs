using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Universe;

/// <summary>
/// Manager that gets input from the player and exposes virtual buttons/axes.
/// </summary>
public class InputManager : Manager<InputManager>
{
    public EventSystem eventSystem { get; private set; }
    public StandaloneInputModule standaloneInputModule { get; private set; }

    /// <summary>
    /// MonoBehaviour.Awake ()
    /// </summary>
    void Awake ()
    {
        eventSystem = gameObject.AddComponent<EventSystem>();
        standaloneInputModule = gameObject.AddComponent<StandaloneInputModule>();
    }
}
