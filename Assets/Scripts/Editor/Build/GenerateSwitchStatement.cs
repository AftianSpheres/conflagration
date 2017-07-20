#if UNITY_EDITOR
using System;
using System.CodeDom;

public static partial class BuildTools
{
    /// <summary>
    /// Generates a switch statement from the given Enum. Somewhat hacky.
    /// Outputs literal C# code, so you can't use this if
    /// you're generating something else.
    /// </summary>
    public static CodeStatement GenerateSwitchStatement(Type enumType, Func<string, string> actionGenerator, string switchVar, bool breakAfterEntry, int indentation, string defaultAction = null)
    {
        if (!enumType.IsEnum) throw new Exception("Can't call GenerateSwitchStatement with a non-enumerated type.");
        const string spacer = "    ";
        const string breakLine = "break;";
        const string switchEnd = "}";
        const string switchEntry_Break = "case !!val!!:\n!!space!!!!action!!;";
        const string switchEntry_Return = "case !!val!!:\n!!space!!return !!action!!;";
        const string switchHead = "switch (!!switchVar!!)";
        string baseIndent = string.Empty;
        for (int s = 0; s < indentation; s++) baseIndent += spacer;
        string l2Indent = baseIndent + spacer;
        string l3Indent = l2Indent + spacer;
        CodeSnippetStatement statement = new CodeSnippetStatement(baseIndent + switchHead.Replace("!!switchVar!!", switchVar) + Environment.NewLine + baseIndent + "{");
        Array values = Enum.GetValues(enumType);
        for (int i = 0; i < values.Length; i++)
        {
            string name = values.GetValue(i).ToString();
            string actionBody = actionGenerator(name);
            if (actionBody != string.Empty)
            {
                if (breakAfterEntry)
                {
                    string entry = l2Indent + switchEntry_Break.Replace("!!val!!", enumType.Name + "." + name).Replace("!!space!!", l3Indent);
                    statement.Value += Environment.NewLine + entry.Replace("!!action!!", actionBody) + Environment.NewLine + l3Indent + breakLine;
                }
                else
                {
                    string entry = l2Indent + switchEntry_Return.Replace("!!val!!", enumType.Name + "." + name).Replace("!!space!!", l3Indent);
                    statement.Value += Environment.NewLine + entry.Replace("!!action!!", actionBody);
                }
            }
        }
        if (defaultAction != null)
        {
            statement.Value += Environment.NewLine + l2Indent + "default:" + Environment.NewLine + l3Indent + defaultAction;
            if (breakAfterEntry) statement.Value += Environment.NewLine + l3Indent + breakLine;
        }
        statement.Value += Environment.NewLine;
        statement.Value += baseIndent + switchEnd;
        return statement;
    }
}
#endif