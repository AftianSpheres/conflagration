﻿using System;
using System.Collections.Generic;
using UnityEngine;
using CnfBattleSys;
using MovementEffects;

/// <summary>
/// Misc. helper functions.
/// </summary>
public static class Util
{
    public static ulong buildNumber { get { if (_buildNumber == 0) GetBuildNumber(); return _buildNumber; } }
    private readonly static char[] vowelsArray = { 'a', 'e', 'i', 'o', 'u', 'A', 'E', 'I', 'O', 'U' }; // lol
    private static ulong _buildNumber = 0;
    private const string buildNumberPath = "buildnumber";

    /// <summary>
    /// Loads in the build number.
    /// </summary>
    private static void GetBuildNumber ()
    {
        TextAsset buildNumberFile = Resources.Load<TextAsset>(buildNumberPath);
        if (buildNumberFile == null) Crash(new Exception("Couldn't load build number file!"));
        _buildNumber = ulong.Parse(buildNumberFile.text);
        if (_buildNumber == 0) Crash(new Exception("Build number of 0 should never happen - check the incrementer"));
    }

    /// <summary>
    /// Determines if a character is a vowel.
    /// Currently kinda dumb.
    /// </summary>
    public static bool CharIsVowel(char c)
    {
        bool r = false;
        for (int i = 0; i < vowelsArray.Length; i++)
        {
            if (c == vowelsArray[i])
            {
                r = true;
                break;
            }
        }
        return r;
    }

    /// <summary>
    /// Gets the total progress of an array of AsyncOperations by arithmetic mean.
    /// </summary>
    public static float AverageCompletionOfOps(AsyncOperation[] ops)
    {
        float r = 0;
        for (int i = 0; i < ops.Length; i++) r += ops[i].progress;
        r /= ops.Length;
        return r;
    }

    /// <summary>
    /// Batch replaces substrings in input string with replacements.
    /// </summary>
    public static string BatchReplace (string input, string[] oldSubstrings, string[] newSubstrings)
    {
        if (oldSubstrings.Length != newSubstrings.Length) Crash(new Exception("Can't batch replace substrings unless the same number of replacement strings are provided"));
        string output = input;
        for (int i = 0; i < oldSubstrings.Length; i++)
        {
            output = output.Replace(oldSubstrings[i], newSubstrings[i]);
        }
        return output;
    }

    /// <summary>
    /// Creates an empty gameobject, child of transform.
    /// (Are you my mummy?)
    /// </summary>
    public static GameObject CreateEmptyChild (Transform transform)
    {
        GameObject go = new GameObject();
        go.transform.parent = transform;
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one;
        go.transform.localRotation = Quaternion.identity;
        return go;
    }

    /// <summary>
    /// Calculate damage given attacker level, attack stat, defense stat, base damage, and deviation.
    /// Deviation is the amount of randomness permitted in the calculation, and generally should be between 0 and 1.
    /// More deviation = more random.
    /// </summary>
    public static int DamageCalc(int attackerLevel, int atkStat, int defStat, int baseDamage, float deviation)
    {
        const float magic = (Battler.maxLevel / 6f);
        float randomElement = 1 + UnityEngine.Random.Range(-deviation, deviation);
        float lvMod = (attackerLevel / magic) + 1;
        float statMod = atkStat / (float)defStat;
        return Mathf.RoundToInt(baseDamage * statMod * lvMod * randomElement);
    }

    /// <summary>
    /// Splits TextAsset by lines, ignores newlines after lnCount.
    /// </summary>
    public static string[] GetLinesFrom(TextAsset a, int lnCount)
    {
        return a.text.Split(new string[] { "\r\n", "\n" }, lnCount, System.StringSplitOptions.None);
    }

    /// <summary>
    /// Splits textAsset by lines.
    /// </summary>
    public static string[] GetLinesFrom(TextAsset a)
    {
        return a.text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
    }

    /// <summary>
    /// Returns arithmetic mean of values.
    /// </summary>
    public static float Mean (float[] vals)
    {
        if (vals.Length == 0) Crash(new Exception("Can't get the mean of a zero-length array of values!"));
        float r = 0;
        for (int i = 0; i < vals.Length; i++) r += vals[i];
        r /= vals.Length;
        return r;
    }

    /// <summary>
    /// Returns arithmetic mean of values.
    /// </summary>
    public static int Mean (int[] vals)
    {
        if (vals.Length == 0) Crash(new Exception("Can't get the mean of a zero-length array of values!"));
        int r = 0;
        for (int i = 0; i < vals.Length; i++) r += vals[i];
        r /= vals.Length;
        return r;
    }

    /// <summary>
    /// Coroutine: Waits until all ops have finished,
    /// then calls onCompletion.
    /// </summary>
    public static IEnumerator<float> _CallActionAfterOpsFinish(AsyncOperation[] ops, Action onCompletion)
    {
        float progress = AverageCompletionOfOps(ops);
        while (progress < 1.0f) yield return progress;
        onCompletion();
    }

    /// <summary>
    /// Coroutine: Wait one frame, then call onCompletion.
    /// </summary>
    public static IEnumerator<float> _WaitOneFrame (Action onCompletion)
    {
        yield return 0;
        onCompletion();
    }

    /// <summary>
    /// Crashes with an automatically generated exception message.
    /// Simplifies cases where you wanna choke on an out-of-range enum value or what have you.
    /// </summary>
    public static void Crash (object withBadValue, object caller, GameObject gameObject)
    {
        Type realType = withBadValue.GetType();
        Crash(new Exception("Bad " + realType + " value " + withBadValue.ToString() + " on " + caller.GetType() + " attached to GameObject " + gameObject.name + " in scene " + gameObject.scene.name));
    }

    /// <summary>
    /// Throws the exception, then crashes.
    /// Crashing is generally better than doing something
    /// nonsensical, which Unity'll happily do if you just
    /// throw an unhandled exception...
    /// </summary>
    public static void Crash(string msg)
    {
        Crash(new Exception(msg));
    }

    /// <summary>
    /// Throws the exception, then crashes.
    /// Crashing is generally better than doing something
    /// nonsensical, which Unity'll happily do if you just
    /// throw an unhandled exception...
    /// </summary>
    public static void Crash (Exception exception)
    {
        Timing.RunCoroutine(_CrashLoop());
        if (Application.isEditor) Debug.Break();
        else Application.Quit();
        throw exception;
    }

    /// <summary>
    /// Return a dictionary populates based on the given arrays of keys and values.
    /// </summary>
    public static Dictionary<T0, T1> PopulateDictWith<T0, T1> (T0[] keysArray, T1[] valuesArray)
    {
        Dictionary<T0, T1> dict = new Dictionary<T0, T1>();
        if (keysArray.Length != valuesArray.Length) Crash("Keys array and values array must be same length");
        else
        {
            for (int i = 0; i < keysArray.Length; i++) dict.Add(keysArray[i], valuesArray[i]);
        }
        return dict;
    }

    /// <summary>
    /// Waits one frame, then crashes.
    /// If the editor resumes after crashing we'll Debug.Break() once every frame until
    /// it's shut off. If you call Crash () it's assumed that you do in fact want to crash.
    /// </summary>
    private static IEnumerator<float> _CrashLoop ()
    {
        while (true)
        {
            if (Application.isEditor) Debug.Break();
            else Application.Quit();
            yield return 0;
        }
    }
}