using UnityEngine;
using System.Collections;

/// <summary>
/// MonoBehaviour attached to all venue prefabs.
/// Stores data used to set up global lighting, etc. when loading in the prefab.
/// </summary>
public class VenueContainer : MonoBehaviour
{
    public Material skyboxMaterial;
    public Color cameraBGColor;
}
