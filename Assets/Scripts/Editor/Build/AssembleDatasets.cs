﻿#if UNITY_EDITOR
using System;
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
using GeneratedDatasets;

/// <summary>
/// Builds Scripts/Autogenerated/Datasets.cs
/// </summary>
public class AssembleDatasets : IPreprocessBuild
{
    int IOrderedCallback.callbackOrder { get { return 0; } }
    private static string datasetsClassComment = "<summary> Static class containing misc. datasets. Automatically generated. </summary>";
    private static string datasetsClassName = "Datasets";
    private static string factoriesClassComment = "<summary> Static class containing factory methods. Automatically generated. </summary>";
    private static string factoriesClassName = "Factories";
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
    /// Generate and fetch declaration for action lookup table.
    /// </summary>
    private static CodeMemberField GetActionArrayDeclaration ()
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
        return actionArrayDeclaration;
    }

    /// <summary>
    /// Generate and fetch declaration for datasets class.
    /// </summary>
    private static CodeTypeDeclaration GetDeclarationForDatasetsClass ()
    {
        CodeTypeDeclaration classDeclaration = new CodeTypeDeclaration(datasetsClassName);
        classDeclaration.IsClass = true;
        classDeclaration.Attributes = MemberAttributes.Static | MemberAttributes.Public;
        classDeclaration.Members.Add(GetActionArrayDeclaration());
        classDeclaration.Comments.Add(new CodeCommentStatement(datasetsClassComment, true));
        return classDeclaration;
    }

    /// <summary>
    /// Generate and fetch declaration for factories class.
    /// </summary>
    private static CodeTypeDeclaration GetDeclarationForFactoriesClass ()
    {
        CodeTypeDeclaration classDeclaration = new CodeTypeDeclaration(factoriesClassName);
        classDeclaration.IsClass = true;
        classDeclaration.Attributes = MemberAttributes.Static | MemberAttributes.Public;
        classDeclaration.Members.Add(GetFactoryFor(typeof(BattleCameraScript), typeof(BattleCameraScriptType)));
        classDeclaration.Comments.Add(new CodeCommentStatement(factoriesClassComment, true));
        return classDeclaration;
    }

    /// <summary>
    /// Generate an enumeration containing entries for each type derived from baseType.
    /// </summary>
    private static CodeTypeDeclaration GetEnumDeclarationFor(Type baseType)
    {
        Type[] derivedTypes = BuildTools.GetDerivedFrom(baseType);
        string enumName = baseType.Name + "Type";
        CodeTypeDeclaration enumDeclaration = new CodeTypeDeclaration(enumName);
        enumDeclaration.Members.Add(new CodeMemberField(enumName, "None"));
        enumDeclaration.IsEnum = true;
        enumDeclaration.Attributes = MemberAttributes.Public;
        for (int t = 0; t < derivedTypes.Length; t++) enumDeclaration.Members.Add(new CodeMemberField(enumName, derivedTypes[t].Name));
        return enumDeclaration;
    }

    /// <summary>
    /// Get a factory method that generates
    /// a lookup table that can be used to instantiate
    /// a class deriving from baseType.
    /// </summary>
    private static CodeMemberMethod GetFactoryFor(Type baseType, Type enumType)
    {
        Type[] derivedTypes = BuildTools.GetDerivedFrom(baseType);
        Func<string, bool> exists = (name) => { for (int i = 0; i < derivedTypes.Length; i++) if (derivedTypes[i].Name == name) return true; return false; };
        CodeMemberMethod methodDeclaration = new CodeMemberMethod();
        methodDeclaration.ReturnType = new CodeTypeReference(baseType);
        methodDeclaration.Parameters.Add(new CodeParameterDeclarationExpression(enumType, "tableEntry"));
        CodeStatement switchStatement = BuildTools.GenerateSwitchStatement(enumType, (name) => { if (exists(name)) return "new !!name!!()".Replace("!!name!!", name); else return string.Empty; }, "tableEntry", false, 3, "return null;");
        methodDeclaration.Statements.Add(switchStatement);
        methodDeclaration.Name = baseType.Name + "Factory";
        methodDeclaration.Comments.Add(new CodeCommentStatement("<summary>Factory that takes !!baseType!!Type entries and returns an instance of the corresponding derived type.</summary>".Replace("!!baseType!!", baseType.Name), true));
        methodDeclaration.Attributes = MemberAttributes.Public | MemberAttributes.Static;
        return methodDeclaration;
    }

    /// <summary>
    /// Generate the source file and save it.
    /// </summary>
    private static void GenerateSource ()
    {
        if (!built)
        {
            CodeNamespace namespaceDeclaration = new CodeNamespace("GeneratedDatasets");
            namespaceDeclaration.Types.Add(GetEnumDeclarationFor(typeof(BattleCameraScript)));
            namespaceDeclaration.Types.Add(GetDeclarationForFactoriesClass());
            namespaceDeclaration.Types.Add(GetDeclarationForDatasetsClass());      
            writer = new StringWriter(new StringBuilder(1024*1024*64));
            options.VerbatimOrder = true;
            options.BlankLinesBetweenMembers = false;
            cu.Namespaces.Add(namespaceDeclaration);
            csProvider.GenerateCodeFromCompileUnit(cu, writer, options);
            FileStream fs = File.OpenWrite(Application.dataPath + path + outputName);
            StreamWriter sw = new StreamWriter(fs);
            fs.Position = 0;
            sw.Write(writer.ToString().Replace("public class", "public static class")); // [PUKING INTENSIFIES]
            sw.Dispose();
            fs.Dispose();
            AssetDatabase.Refresh();
            Debug.Log("Generated Datasets.cs");
        }
    }
}
#endif