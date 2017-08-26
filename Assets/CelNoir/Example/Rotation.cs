using UnityEngine;

public class Rotation : MonoBehaviour
{
    public Vector3 rotation;

	void Update ()
    {
        transform.Rotate(rotation * Time.deltaTime);
	}
}
