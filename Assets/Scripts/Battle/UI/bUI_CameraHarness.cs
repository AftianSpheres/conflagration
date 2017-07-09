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
    /// Valid modes the camera harness can be in.
    /// The specific mode the camera harness is in will determine
    /// the precise logic it applies to find its position
    /// in relation to its focus.
    /// </summary>
    public enum ViewpointMode
    {
        None,
        OverheadView,
        BehindBack,
        Dynamic
    }
    public Camera viewportCam;
    private Transform focusTransform;
    private ViewpointMode viewpointMode;
	
    /// <summary>
    /// MonoBehaviour.Start ()
    /// </summary>
    void Start()
    {
        bUI_BattleUIController.instance.RegisterCameraHarness(this);
    }

    public void AcceptBattleCameraScript (BattleCameraScript _battleCameraScript, Action callback)
    {
        Debug.Log("Implement this pls");
    }
}
