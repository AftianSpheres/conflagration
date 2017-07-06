﻿#if UNITY_EDITOR
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using BattleActionTool;
using CnfBattleSys;

/// <summary>
/// Builds Scripts/Autogenerated/Datasets.cs
/// </summary>
public class AssembleDatasets : IPreprocessBuild
{
    int IOrderedCallback.callbackOrder { get { return 0; } }
    private static string classComment = "<summary> Static class containing misc. datasets. Automatically generated. </summary>";
    private static string className = "Datasets";
    private static string outputName = "Datasets.cs";
    private static string path = "/Scripts/Autogenerated/";
    private static string battleActionsComment = "<summary> BattleAction definition lookup table. Automatically generated. Basically an unreadable mess, but that's ok - use ActionDatabase.Get() to grab entries out of this. Don't try to work out what's what yourself! </summary>";
    private static CodeCompileUnit cu = new CodeCompileUnit();
    private static CodeGeneratorOptions options = new CodeGeneratorOptions();
    private static Microsoft.CSharp.CSharpCodeProvider csProvider = new Microsoft.CSharp.CSharpCodeProvider();
    private static StringWriter writer;
    private static bool built { get { return writer != null; } }

    /// <summary>
    /// IPreprocessBuild.OnPreprocessBuild (target, path)
    /// </summary>
    void IPreprocessBuild.OnPreprocessBuild (BuildTarget target, string path)
    {
        GenerateSource();
    }

    /// <summary>
    /// OnPostProcessScene ()
    /// </summary>
    [PostProcessScene]
    public static void OnPostProcessScene ()
    {
        GenerateSource();
    }

    /// <summary>
    /// Clears the StringWriter to force a rebuild,
    /// then rebuilds.
    /// </summary>
    public static void Refire ()
    {
        writer = null;
        GenerateSource();
    }

    /// <summary>
    /// Generate the source file and save it.
    /// </summary>
    private static void GenerateSource ()
    {
        if (!built)
        {
            CodeObjectCreateExpression[] actionDeclarations = new CodeObjectCreateExpression[ActionDatabase.count];
            for (int i = 0; i < actionDeclarations.Length; i++)
            {
                BattleActionModel model = new BattleActionModel((ActionType)i);
                actionDeclarations[i] = model.DumpToCSDeclaration();
            }
            CodeMemberField actionArrayDeclaration = new CodeMemberField(typeof(BattleAction[]), "battleActions");
            actionArrayDeclaration.Attributes = MemberAttributes.Static | MemberAttributes.Public | MemberAttributes.Final;
            actionArrayDeclaration.InitExpression = new CodeArrayCreateExpression(typeof(BattleAction), actionDeclarations);
            actionArrayDeclaration.Comments.Add(new CodeCommentStatement(battleActionsComment, true));
            CodeTypeDeclaration classDeclaration = new CodeTypeDeclaration(className);
            classDeclaration.IsClass = true;
            classDeclaration.Attributes = MemberAttributes.Static | MemberAttributes.Public;
            classDeclaration.Members.Add(actionArrayDeclaration);
            classDeclaration.Comments.Add(new CodeCommentStatement(classComment, true));
            CodeNamespace namespaceDeclaration = new CodeNamespace("GeneratedDatasets");
            namespaceDeclaration.Types.Add(classDeclaration);
            writer = new StringWriter(new StringBuilder(512 * actionDeclarations.Length));
            options.VerbatimOrder = true;
            options.BlankLinesBetweenMembers = false;
            cu.Namespaces.Add(namespaceDeclaration);
            csProvider.GenerateCodeFromCompileUnit(cu, writer, options);
            FileStream fs = File.OpenWrite(Application.dataPath + path + outputName);
            StreamWriter sw = new StreamWriter(fs);
            fs.Position = 0;
            sw.Write(writer.ToString().Replace("public class Datasets", "public static class Datasets")); // [PUKING INTENSIFIES]
            sw.Dispose();
            fs.Dispose();
            AssetDatabase.Refresh();
            Debug.Log("Generated Datasets.cs");
        }
    }
}
#endif