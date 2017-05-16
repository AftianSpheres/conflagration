using UnityEngine;

/// <summary>
/// Wee bitty babby MonoBehaviour that ensures the attached object isn't moved and/or
/// rotated in world space. Use this for 2D elements only - it doesn't lock
/// specific axes; it locks every axis.
/// </summary>
public class TransformLock : MonoBehaviour
{
    public bool lockPosition;
    public bool lockRotation;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    /// <summary>
    /// MonoBehaviour.Awake()
    /// </summary>
    void Awake ()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
    }

    /// <summary>
    /// MonoBehaviour.LateUpdate()
    /// </summary>
    void LateUpdate ()
    {
        if (lockPosition) transform.position = originalPosition;
        if (lockRotation) transform.rotation = originalRotation;
    }
}
