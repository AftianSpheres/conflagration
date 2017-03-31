using UnityEngine;

/// <summary>
/// Misc. helper functions.
/// </summary>
public static class Util
{
    private readonly static char[] vowelsArray = { 'a', 'e', 'i', 'o', 'u', 'A', 'E', 'I', 'O', 'U' };

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