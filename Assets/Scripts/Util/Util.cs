using System;
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
    private readonly static char[] vowelsArray = { 'a', 'e', 'i', 'o', 'u', 'A', 'E', 'I', 'O', 'U' };
    private static ulong _buildNumber = 0;
    private const string buildNumberPath = "buildnumber";
#if UNITY_EDITOR
    public static ulong internalBuildNumber { get { return _buildNumber; } } // this is an ugly hack but we need to be able to make sure the build number hasn't already been set in order to only increment it once
#endif
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
    /// Throws the exception, then crashes.
    /// Crashing is generally better than doing something
    /// nonsensical, which Unity'll happily do if you just
    /// throw an unhandled exception...
    /// </summary>
    public static void Crash (Exception exception)
    {
        Timing.RunCoroutine(_DieInOneFrame());
        throw exception;
    }

    /// <summary>
    /// Waits one frame, then crashes.
    /// If the editor resumes after crashing we'll Debug.Break() once every frame until
    /// it's shut off. If you call Crash () it's assumed that you do in fact want to crash.
    /// </summary>
    private static IEnumerator<float> _DieInOneFrame ()
    {
        yield return 0;
        while (true)
        {
            if (Application.isEditor) Debug.Break();
            else Application.Quit();
            yield return 0;
        }
    }
}