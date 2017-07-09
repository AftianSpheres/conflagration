using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Test implementation of a BattleCameraScript.
/// </summary>
public class BattleCameraScript_Demo : BattleCameraScript
{
    public BattleCameraScript_Demo ()
    {
        isIndefiniteDuration = true;
    }

    /// <summary>
    /// BattleCameraScript.InStart()
    /// </summary>
    public override void InStart()
    {
        Debug.Log("InStart fire");
    }

    /// <summary>
    /// BattleCameraScript.InEnd()
    /// </summary>
    protected override void InEnd()
    {
        Debug.Log("InEnd fire");
    }

    protected override void PrepareForEnd()
    {
        throw new NotImplementedException();
    }

    protected override IEnumerator<float> _CameraSequence()
    {
        throw new NotImplementedException();
    }
}
