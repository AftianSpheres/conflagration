#if UNITY_EDITOR
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using CnfBattleSys;

/// <summary>
/// Builds Scripts/Battle/Core/Battler/ApplyEffect
/// </summary>
public class AssembleEffectTable : IPreprocessBuild
{
    int IOrderedCallback.callbackOrder { get { return 0; } }
    private static string outputName = "ApplyEffect.cs";
    private static string path = "/Scripts/Battle/Core/Battler/";
    private static CodeCompileUnit cu = new CodeCompileUnit();
    private static CodeGeneratorOptions options = new CodeGeneratorOptions();
    private static Microsoft.CSharp.CSharpCodeProvider csProvider = new Microsoft.CSharp.CSharpCodeProvider();
    private static StringWriter writer;
    private static bool built { get { return writer != null; } }

    /// <summary>
    /// IPreprocessBuild.OnPreprocessBuild (target, path)
    /// </summary>
    void IPreprocessBuild.OnPreprocessBuild(BuildTarget target, string path)
    {
        GenerateSource();
    }

    /// <summary>
    /// OnPostProcessScene ()
    /// </summary>
    [PostProcessScene]
    public static void OnPostProcessScene()
    {
        GenerateSource();
    }

    /// <summary>
    /// Clears the StringWriter to force a rebuild,
    /// then rebuilds.
    /// </summary>
    public static void Refire()
    {
        writer = null;
        GenerateSource();
    }

    /// <summary>
    /// Generate the source file and save it.
    /// </summary>
    private static void GenerateSource()
    {
        if (!built)
        {
            CodeNamespace namespaceDeclaration = new CodeNamespace("CnfBattleSys");
            namespaceDeclaration.Imports.Add(new CodeNamespaceImport("UnityEngine"));
            CodeTypeDeclaration battlerTypeDeclaration = new CodeTypeDeclaration("Battler");
            battlerTypeDeclaration.IsClass = true;
            battlerTypeDeclaration.IsPartial = true;
            battlerTypeDeclaration.Attributes = MemberAttributes.Public;
            namespaceDeclaration.Types.Add(battlerTypeDeclaration);
            CodeMemberMethod methodDeclaration = new CodeMemberMethod();
            methodDeclaration.Name = "ApplyEffect";
            methodDeclaration.Attributes = MemberAttributes.Public;
            methodDeclaration.ReturnType = new CodeTypeReference(typeof(void));
            methodDeclaration.Parameters.Add(new CodeParameterDeclarationExpression(typeof(BattleAction.Subaction.EffectPackage), "effect"));
            methodDeclaration.Comments.Add(new CodeCommentStatement("<summary>Passes the given EffectPackage through to the appropriate method to handle applying its effect. Automatically generated.</summary>", true));
            Func<string, bool> exists = (name) => { if (typeof(Battler).GetMember(name).Length > 0) return true; else return false; };
            CodeStatement switchStatement = BuildTools.GenerateSwitchStatement(typeof(EffectPackageType), (name) => { if (exists(name)) return name + "(effect)"; else return string.Empty; }, "effect.effectType", true, 3, 
                "Debug.Log(\"Unhandled EffectPackageType: \" + effect.effectType);");
            methodDeclaration.Statements.Add(switchStatement);
            battlerTypeDeclaration.Members.Add(methodDeclaration);
            writer = new StringWriter(new StringBuilder(1024 * 1024 * 64));
            options.VerbatimOrder = true;
            options.BlankLinesBetweenMembers = false;
            cu.Namespaces.Add(namespaceDeclaration);
            csProvider.GenerateCodeFromCompileUnit(cu, writer, options);
            FileStream fs = File.OpenWrite(Application.dataPath + path + outputName);
            StreamWriter sw = new StreamWriter(fs);
            fs.Position = 0;
            sw.Write(writer.ToString());
            sw.Dispose();
            fs.Dispose();
            AssetDatabase.Refresh();
            Debug.Log("Generated Battler/ApplyEffect.cs");
        }
    }
}
#endif