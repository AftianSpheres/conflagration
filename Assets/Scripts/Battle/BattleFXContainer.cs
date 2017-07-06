using System.Collections.Generic;
using UnityEngine;
using CnfBattleSys;

/// <summary>
/// Provides access to instantiated FX controllers attached to this
/// game object.
/// </summary>
public class BattleFXContainer : MonoBehaviour
{
    private Dictionary<SignedFXEventType, BattleFXController> fxControllers;

    /// <summary>
    /// Attaches a BattleFXContainer component to the given
    /// GameObject and populates it with all child FX controllers.
    /// </summary>
    public static BattleFXContainer AttachTo(GameObject go)
    {
        BattleFXContainer container = go.AddComponent<BattleFXContainer>();
        container.Populate();
        return container;
    }

    /// <summary>
    /// Gets the controller tied to the given FX event type.
    /// </summary>
    public BattleFXController GetController (FXEvent fxEvent)
    {
        return fxControllers[fxEvent.signedFXEventType]; // Load () should take care of every fx controller we can possibly actually need. You should never be able to pass a value into this that _isn't_ loaded.
    }

    /// <summary>
    /// Populate the FXContainer with our controllers.
    /// </summary>
    private void Populate ()
    {
        BattleFXController[] array = GetComponentsInChildren<BattleFXController>();
        fxControllers = new Dictionary<SignedFXEventType, BattleFXController>(array.Length);
        for (int i = 0; i < array.Length; i++) fxControllers.Add(array[i].originatingEvent.signedFXEventType, array[i]);
    }
}
