﻿#if UNITY_EDITOR
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using ExtendedAnimationManagement;

/// <summary>
/// Generates AnimatorMetadataLookupTable.cs and registers all AnimatorController assets.
/// </summary>
public class GenerateAnimatorMetadataLookupTable : IPreprocessBuild
{
    int IOrderedCallback.callbackOrder { get { return 0; } }
    private static string classComment = "<summary> Static class containing animator metadata definitions. Automatically generated. </summary>";
    private static string className = "AnimatorMetadataLookupTable";
    private static string outputName = "AnimatorMetadataLookupTable.cs";
    private static string path = "/Extended Animation Management/Scripts/Autogenerated/";
    private static CodeCompileUnit cu = new CodeCompileUnit();
    private static CodeGeneratorOptions options = new CodeGeneratorOptions();
    private static Microsoft.CSharp.CSharpCodeProvider csProvider = new Microsoft.CSharp.CSharpCodeProvider();
    private static StringWriter writer;
    private static bool built { get { return writer != null; } }
    private static int guidCount = 0;

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
            CodeMemberField lookupTableDeclaration = new CodeMemberField(typeof(AnimatorMetadata[]), "lookupTable");
            lookupTableDeclaration.Comments.Add(new CodeCommentStatement(new CodeComment("<summary> The table of animator metadata. Each animator controller automatically has a MetadataPusher attached to it, which has its index in this table. </summary>")));
            lookupTableDeclaration.Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static;
            lookupTableDeclaration.InitExpression = GenerateLookupTableAndRegisterAnimatorControllers();
            CodeTypeDeclaration classDeclaration = new CodeTypeDeclaration(className);
            classDeclaration.IsClass = true;
            classDeclaration.Attributes = MemberAttributes.Public;
            classDeclaration.Members.Add(lookupTableDeclaration);
            classDeclaration.Comments.Add(new CodeCommentStatement(classComment, true));
            CodeNamespace namespaceDeclaration = new CodeNamespace("ExtendedAnimationManagement");
            namespaceDeclaration.Types.Add(classDeclaration);
            writer = new StringWriter(new StringBuilder(512 * guidCount));
            options.VerbatimOrder = true;
            options.BlankLinesBetweenMembers = false;
            cu.Namespaces.Add(namespaceDeclaration);
            csProvider.GenerateCodeFromCompileUnit(cu, writer, options);
            FileStream fs = File.OpenWrite(Application.dataPath + path + outputName);
            StreamWriter sw = new StreamWriter(fs);
            fs.Position = 0;
            sw.Write(writer.ToString().Replace("public class AnimatorMetadataLookupTable", "public static class AnimatorMetadataLookupTable")); // this will never not be the worst
            sw.Dispose();
            fs.Dispose();
            AssetDatabase.Refresh();
            Debug.Log("Generated AnimatorMetadataLookupTable.cs");
        }
    }

    /// <summary>
    /// Pull every animator controller in the project folder, register it with its index in the lookup table, and
    /// return the C# declaration of the lookup table.
    /// </summary>
    /// <returns></returns>
    private static CodeArrayCreateExpression GenerateLookupTableAndRegisterAnimatorControllers ()
    {
        string[] guidList = AssetDatabase.FindAssets("t:RuntimeAnimatorController");
        guidCount = guidList.Length;
        CodeObjectCreateExpression[] metadataDeclarations = new CodeObjectCreateExpression[guidList.Length];
        for (int i = 0; i < guidList.Length; i++)
        {
            AnimatorController animatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GUIDToAssetPath(guidList[i]));
            RegisterAnimatorController(animatorController, i);
            metadataDeclarations[i] = DeclareMetadataFor(animatorController);
        }
        return new CodeArrayCreateExpression(typeof(AnimatorMetadata), metadataDeclarations);
    }

    /// <summary>
    /// Returns a CodeObjectCreateExpression that declares the metadata for the animatorController.
    /// </summary>
	private static CodeObjectCreateExpression DeclareMetadataFor (AnimatorController animatorController)
    {
        CodeArrayCreateExpression[] layerDeclarations = new CodeArrayCreateExpression[animatorController.layers.Length];
        for (int l = 0; l < animatorController.layers.Length; l++)
        {      
            ChildAnimatorState[] states = animatorController.layers[l].stateMachine.states;

            CodeObjectCreateExpression[] thisLayerDeclarationsArray = new CodeObjectCreateExpression[states.Length];
            for (int s = 0; s < states.Length; s++)
            {
                CodePrimitiveExpression fullPath = new CodePrimitiveExpression(animatorController.layers[l].name + "." + states[s].state.name);
                CodePrimitiveExpression name = new CodePrimitiveExpression(states[s].state.name);
                thisLayerDeclarationsArray[s] = new CodeObjectCreateExpression(typeof(AnimatorMetadata.StateMetadata), new CodeExpression[] { fullPath, name });
            }
            layerDeclarations[l] = new CodeArrayCreateExpression(typeof(AnimatorMetadata.StateMetadata[]), thisLayerDeclarationsArray);
        }
        return new CodeObjectCreateExpression(typeof(AnimatorMetadata), new CodeArrayCreateExpression(typeof(AnimatorMetadata.StateMetadata[][]), layerDeclarations));
    }

    /// <summary>
    /// Make sure the animator controller has a metadata pusher on layer 0, and
    /// associate it with the appropriate index in the metadata lookup table.
    /// </summary>
    private static void RegisterAnimatorController (AnimatorController animatorController, int index)
    {
        StateMachineBehaviour[] layer0Behaviours = animatorController.layers[0].stateMachine.behaviours;
        StateMachineBehaviour_MetadataPusher metadataPusher = null;
        for (int i = 0; i < layer0Behaviours.Length; i++)
        {
            if (layer0Behaviours[i] is StateMachineBehaviour_MetadataPusher)
            {
                metadataPusher = (StateMachineBehaviour_MetadataPusher)layer0Behaviours[i];
                break;
            }
        }
        if (metadataPusher == null) metadataPusher = animatorController.layers[0].stateMachine.AddStateMachineBehaviour<StateMachineBehaviour_MetadataPusher>();
        metadataPusher.tableIndex_SetAutomatically = index;
        EditorUtility.SetDirty(animatorController);
    }
}

#endif