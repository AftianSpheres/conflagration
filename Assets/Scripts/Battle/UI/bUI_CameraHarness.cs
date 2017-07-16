using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CnfBattleSys;

/// <summary>
/// MonoBehaviour that controls the battle camera.
/// This exposes a set of simple methods that other parts of the battle system can call to have
/// the camera behave in the desired manner without worrying about inline camera logic.
/// </summary>
public class bUI_CameraHarness : MonoBehaviour
{
    /// <summary>
    /// The BattleCameraScript instance that's currently driving the CameraHarness.
    /// </summary>
    public BattleCameraScript battleCameraScript { get; private set; }
    /// <summary>
    /// The Camera attached to this CameraHarness.
    /// </summary>
    public Camera viewportCam { get; private set; }
	
    void Awake ()
    {
        viewportCam = GetComponent<Camera>();
    }

    /// <summary>
    /// MonoBehaviour.Start ()
    /// </summary>
    void Start ()
    {
        bUI_BattleUIController.instance.RegisterCameraHarness(this);
    }

    /// <summary>
    /// Forces the current battleCameraScript to end
    /// and normalizes the state of the cameraHarness.
    /// </summary>
    public void AbortCurrentBattleCameraScript ()
    {
        battleCameraScript.End();
    }

    /// <summary>
    /// Take the BattleCameraScript given and start handling it.
    /// The callback given will be called when this battleCameraScript finishes
    /// and End() is called on it.
    /// </summary>
    public void AcceptBattleCameraScript (BattleCameraScript _battleCameraScript, Action callback = null)
    {
        battleCameraScript = _battleCameraScript;
        battleCameraScript.Start(callback);
    }


}
