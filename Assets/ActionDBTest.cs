using UnityEngine;
using System.Collections;

public class ActionDBTest : MonoBehaviour {

	// Use this for initialization
	void Start ()
    {
        CnfBattleSys.Datasets.GetAction(CnfBattleSys.ActionType.TestBuff);
        CnfBattleSys.Datasets.GetStance(CnfBattleSys.StanceType.TestStance);
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
