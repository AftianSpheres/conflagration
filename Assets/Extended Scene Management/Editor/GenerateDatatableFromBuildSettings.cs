﻿#if UNITY_EDITOR
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using ExtendedSceneManagement;

/// <summary>
/// Automatically generates SceneDatatable before build.
/// </summary>
public class GenerateDatatableFromBuildSettings : IPreprocessBuild
{
    /// <summary>
    /// Models scene metadata.
    /// </summary>
    private struct DatatableEntry
    {
        public int buildIndex;
        public string convertedPath;
        public string deconvertedPath;
        public string sceneName;
        public SceneRing sceneRing;

        public DatatableEntry(int i)
        {
            buildIndex = i;
            convertedPath = SceneUtility.GetScenePathByBuildIndex(i);
            deconvertedPath = ExtendedScene.DeconvertPath(convertedPath);
            string[] substrings = deconvertedPath.Split('/');
            sceneName = substrings[substrings.Length - 1];
            sceneRing = SceneRing.None;
            for (int r = 1; r > int.MinValue; r = r << 1)
            {
                if (deconvertedPath.Contains("/" + (SceneRing)r + "/" + sceneName))
                {
                    sceneRing = (SceneRing)r;
                    break;
                }
            }
        }
    }

    int IOrderedCallback.callbackOrder { get { return 0; } }
    private static string classComment = "<summary> Static class containing scene metadata definitions. Automatically generated. </summary>";
    private static string className = "SceneDatatable";
    private static string outputName = "SceneDatatable.cs";
    private static string path = "/Extended Scene Management/Scripts/Autogenerated/";
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
        GenerateDatatable();
    }

    /// <summary>
    /// OnPostProcessScene ()
    /// </summary>
    [PostProcessScene]
    public static void OnPostProcessScene()
    {
        GenerateDatatable();
    }

    /// <summary>
    /// Clears the StringWriter to force a rebuild,
    /// then rebuilds.
    /// </summary>
    public static void Refire()
    {
        writer = null;
        GenerateDatatable();
    }

    /// <summary>
    /// Generates SceneDatatable.cs
    /// </summary>
    private static void GenerateDatatable ()
    {
        if (!built)
        {
            writer = new StringWriter(new StringBuilder(512 * SceneManager.sceneCountInBuildSettings));
            options.VerbatimOrder = true;
            options.BlankLinesBetweenMembers = false;
            CodeNamespace thisNamespace = new CodeNamespace("ExtendedSceneManagement");
            CodeTypeDeclaration datatableClass = new CodeTypeDeclaration();
            datatableClass.Name = className;
            datatableClass.IsClass = true;
            datatableClass.Attributes = MemberAttributes.Public;
            datatableClass.Comments.Add(new CodeCommentStatement(new CodeComment(classComment, true)));
            CodeMemberField metadataArray = new CodeMemberField();
            metadataArray.Name = "metadata";
            metadataArray.Comments.Add(new CodeCommentStatement("<summary> Scene metadata array. Scene names are aliases to this. </summary>", true));
            metadataArray.Type = new CodeTypeReference(typeof(SceneMetadata[]));
            metadataArray.Attributes = MemberAttributes.Public | MemberAttributes.Static | MemberAttributes.Final;
            CodeObjectCreateExpression[] metadataArrayInitSubexpressions = new CodeObjectCreateExpression[SceneManager.sceneCountInBuildSettings];
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                DatatableEntry datatableEntry = new DatatableEntry(i);
                CodeMemberProperty thisMetadata = new CodeMemberProperty();
                thisMetadata.Name = datatableEntry.sceneName;
                thisMetadata.Type = new CodeTypeReference(typeof(SceneMetadata));
                thisMetadata.Attributes = MemberAttributes.Public | MemberAttributes.Static;
                thisMetadata.HasGet = true;
                thisMetadata.GetStatements.Add(new CodeMethodReturnStatement(new CodeArrayIndexerExpression(new CodeFieldReferenceExpression(null, "metadata"), new CodePrimitiveExpression(i))));
                thisMetadata.HasSet = false;
                metadataArrayInitSubexpressions[i] = new CodeObjectCreateExpression(thisMetadata.Type, new CodeExpression[] { new CodePrimitiveExpression(i), new CodePrimitiveExpression(datatableEntry.sceneName),
                new CodePrimitiveExpression(datatableEntry.convertedPath), new CodePrimitiveExpression((int)datatableEntry.sceneRing) }); // new SceneMetadata(index, name, path, SceneRing);
                thisMetadata.Comments.Add(new CodeCommentStatement(new CodeComment("<summary> Scene " + i + ": " + datatableEntry.sceneName + " in SceneRing." + datatableEntry.sceneRing + " </summary>", true)));
                datatableClass.Members.Add(thisMetadata);
            }
            metadataArray.InitExpression = new CodeArrayCreateExpression(metadataArray.Type, metadataArrayInitSubexpressions);
            datatableClass.Members.Add(metadataArray);
            thisNamespace.Types.Add(datatableClass);
            cu.Namespaces.Add(thisNamespace);
            csProvider.GenerateCodeFromCompileUnit(cu, writer, options);
            FileStream fs = File.OpenWrite(Application.dataPath + path + outputName);
            StreamWriter sw = new StreamWriter(fs);
            fs.Position = 0;
            sw.Write(writer.ToString().Replace("public class SceneDatatable", "public static class SceneDatatable")); // I did it, I found the Most Hacks
            sw.Dispose();
            fs.Dispose();
            AssetDatabase.Refresh();
            Debug.Log("Generated SceneDatatable.cs");
        }
    }
}
#endif