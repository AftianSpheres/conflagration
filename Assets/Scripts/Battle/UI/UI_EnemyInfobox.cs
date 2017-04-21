using UnityEngine;
using System.Collections;

public class UI_EnemyInfobox : MonoBehaviour
{
    public Camera battleCam;
    public BattlerPuppet attachedPuppet;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (Input.GetKeyDown(KeyCode.P)) SyncPositionWithPuppet();
	}

    public void SyncPositionWithPuppet ()
    {
        transform.position = battleCam.WorldToScreenPoint(attachedPuppet.transform.position);
    }
}
