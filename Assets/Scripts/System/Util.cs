using UnityEngine;
using CnfBattleSys;

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
        if (buildNumberFile == null) throw new System.Exception("Couldn't load build number file!");
        _buildNumber = ulong.Parse(buildNumberFile.text);
        if (_buildNumber == 0) throw new System.Exception("Build number of 0 should never happen - check the incrementer");
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
    /// Calculate damage given attacker level, attack stat, defense stat, base damage, and deviation.
    /// Deviation is the amount of randomness permitted in the calculation, and generally should be between 0 and 1.
    /// More deviation = more random.
    /// </summary>
    public static int DamageCalc (int attackerLevel, int atkStat, int defStat, int baseDamage, float deviation)
    {
        const float magic = (Battler.maxLevel / 6f);
        float randomElement = 1 + Random.Range(-deviation, deviation);
        float lvMod = (attackerLevel / magic) + 1;
        float statMod = atkStat / (float)defStat;
        return Mathf.RoundToInt(baseDamage * statMod * lvMod * randomElement);
    }

    /// <summary>
    /// Batch replaces substrings in input string with replacements.
    /// </summary>
    public static string BatchReplace (string input, string[] oldSubstrings, string[] newSubstrings)
    {
        if (oldSubstrings.Length != newSubstrings.Length) throw new System.Exception("Can't batch replace substrings unless the same number of replacement strings are provided");
        string output = input;
        for (int i = 0; i < oldSubstrings.Length; i++)
        {
            output = output.Replace(oldSubstrings[i], newSubstrings[i]);
        }
        return output;
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
        return a.text.Split(new string[] { "\r\n", "\n" }, System.StringSplitOptions.None);
    }

    /// <summary>
    /// Returns arithmetic mean of values.
    /// </summary>
    public static float Mean (float[] vals)
    {
        if (vals.Length == 0) throw new System.Exception("Can't get the mean of a zero-length array of values!");
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
        if (vals.Length == 0) throw new System.Exception("Can't get the mean of a zero-length array of values!");
        int r = 0;
        for (int i = 0; i < vals.Length; i++) r += vals[i];
        r /= vals.Length;
        return r;
    }
}