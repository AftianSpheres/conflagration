using UnityEngine;
using System.Collections;

public class ActionDBTest : MonoBehaviour {

	// Use this for initialization
	void Start ()
    {
        CnfBattleSys.ActionDatabase.Get(CnfBattleSys.ActionType.TestBuff);
        CnfBattleSys.StanceDatabase.Get(CnfBattleSys.StanceType.TestStance);
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
