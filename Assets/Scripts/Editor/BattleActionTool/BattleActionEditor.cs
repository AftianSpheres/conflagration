using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CnfBattleSys;
using BattleActionTool;

public class BattleActionEditor : EditorWindow
{
    ActionType selectedAction;
    BattleActionModel battleActionModel;
    GUIStyle style = new GUIStyle();
    Dictionary<EffectPackageModel, bool[]> effectPackageModelStrengthLengthRepresentations;
    Dictionary<EffectPackageModel, bool> effectPackageModelOpenStatuses;
    Dictionary<EventBlockModel, bool> eventBlockModelOpenStatuses;
    Dictionary<SubactionModel, bool> subactionModelOpenStatuses;
    List<EffectPackageModel> effectPackageModelsToRemove;
    List<EventBlockModel.AnimEventModel> animEventModelsToRemove;
    List<EventBlockModel.AudioEventModel> audioEventModelsToRemove;
    List<EventBlockModel.FXEventModel> fxEventModelsToRemove;
    List<SubactionModel> subactionModelsToRemove;
    Vector2 scrollPos = Vector2.zero;

    /// <summary>
    /// EditorWindow.Init ()
    /// </summary>
    [MenuItem("Window/Datasets/BattleAction Editor")]
    static void Init ()
    {
        BattleActionEditor battleActionEditor = (BattleActionEditor)GetWindow(typeof(BattleActionEditor));
        battleActionEditor.Show();
    }

    /// <summary>
    /// EditorWindow.OnGUI ()
    /// </summary>
    void OnGUI()
    {
        if (battleActionModel == null) ChangeSelectedAction(selectedAction);
        style.richText = true;
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, new GUILayoutOption[] { GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true) });
        EditorGUILayout.LabelField("<b>Edit Action: " + selectedAction + "</b>", style);
        EditorGUILayout.BeginHorizontal();
        ActionType a = (ActionType)EditorGUILayout.EnumPopup(selectedAction);
        if (a != selectedAction) ChangeSelectedAction(a);
        if (battleActionModel != null && GUILayout.Button("Build and save")) battleActionModel.SaveToFile();
        EditorGUILayout.EndHorizontal();
        if (battleActionModel != null)
        {
            battleActionModel.info = EditorGUILayout.TextArea(battleActionModel.info, new GUILayoutOption[] { GUILayout.MaxHeight(32) });
            battleActionModel.categoryFlags = (BattleActionCategoryFlags)EditorGUILayout.EnumMaskPopup("Category Flags", battleActionModel.categoryFlags);
            Field("Base Stamina Cost", ref battleActionModel.baseSPCost);
            Field("Base AOE Radius", ref battleActionModel.baseAOERadius);
            Field("Base Delay", ref battleActionModel.baseDelay);
            Field("Base Followthrough Stance Change Delay", ref battleActionModel.baseFollowthroughStanceChangeDelay);
            Field("Base Minimum Targeting Distance", ref battleActionModel.baseMinimumTargetingDistance);
            Field("Base Targeting Range", ref battleActionModel.baseTargetingRange);
            battleActionModel.targetSideFlags = (TargetSideFlags)EditorGUILayout.EnumMaskPopup("Primary Target Side Flags", battleActionModel.targetSideFlags);
            battleActionModel.alternateTargetSideFlags = (TargetSideFlags)EditorGUILayout.EnumMaskPopup("Alternate Target Side Flags", battleActionModel.alternateTargetSideFlags);
            battleActionModel.targetType = (ActionTargetType)EditorGUILayout.EnumPopup("Primary Targeting Type", battleActionModel.targetType);
            battleActionModel.alternateTargetType = (ActionTargetType)EditorGUILayout.EnumPopup("Alternate Targeting Type", battleActionModel.alternateTargetType);
            EventBlockPanel("AnimSkip", battleActionModel.animSkipModel, () => { battleActionModel.animSkipModel = new EventBlockModel(battleActionModel.xmlNode, "animSkip"); }, () => { battleActionModel.animSkipModel = null; });
            EventBlockPanel("OnStart", battleActionModel.onStartModel, () => { battleActionModel.onStartModel = new EventBlockModel(battleActionModel.xmlNode, "onStart"); }, () => { battleActionModel.onStartModel = null; });
            EventBlockPanel("OnConclusion", battleActionModel.onConclusionModel, () => { battleActionModel.onConclusionModel = new EventBlockModel(battleActionModel.xmlNode, "onConclusion"); }, () => { battleActionModel.onConclusionModel = null; });
            subactionModelsToRemove.Clear();
            for (int i = 0; i < battleActionModel.subactionModels.Count; i++) SubactionPanel("Subaction " + i, battleActionModel.subactionModels[i], () => { subactionModelsToRemove.Add(battleActionModel.subactionModels[i]); });
            for (int i = 0; i < subactionModelsToRemove.Count; i++) battleActionModel.subactionModels.Remove(subactionModelsToRemove[i]);
            if (GUILayout.Button("Add new Subaction")) battleActionModel.subactionModels.Add(new SubactionModel(battleActionModel));
        }
        else
        {
            EditorGUILayout.LabelField("(This action ID is for internal use, and should be left undefined.)");
        }
        GUILayout.EndScrollView();

    }

    /// <summary>
    /// Change the selected action ID.
    /// </summary>
    void ChangeSelectedAction(ActionType newSelection)
    {
        selectedAction = newSelection;
        switch (newSelection)
        {
            case ActionType.None:
            case ActionType.INTERNAL_BreakOwnStance:
            case ActionType.InvalidAction:
                battleActionModel = null; // these ids represent "fake" actions that don't have normal definitions
                break;
            default:
                battleActionModel = new BattleActionModel(selectedAction);
                break;
        }
        effectPackageModelStrengthLengthRepresentations = new Dictionary<EffectPackageModel, bool[]>();
        effectPackageModelOpenStatuses = new Dictionary<EffectPackageModel, bool>();
        eventBlockModelOpenStatuses = new Dictionary<EventBlockModel, bool>();
        subactionModelOpenStatuses = new Dictionary<SubactionModel, bool>();
        effectPackageModelsToRemove = new List<EffectPackageModel>();
        animEventModelsToRemove = new List<EventBlockModel.AnimEventModel>();
        audioEventModelsToRemove = new List<EventBlockModel.AudioEventModel>();
        fxEventModelsToRemove = new List<EventBlockModel.FXEventModel>();
        subactionModelsToRemove = new List<SubactionModel>();
    }

    /// <summary>
    /// Adds a set of widgets for the given effect package model.
    /// </summary>
    void EffectPackagePanel (string label, EffectPackageModel effectPackageModel, Action removeCallback)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(16);
        EditorGUILayout.BeginVertical();
        if (!effectPackageModelOpenStatuses.ContainsKey(effectPackageModel)) effectPackageModelOpenStatuses.Add(effectPackageModel, true);
        if (!effectPackageModelStrengthLengthRepresentations.ContainsKey(effectPackageModel))
        {
            effectPackageModelStrengthLengthRepresentations.Add(effectPackageModel, new bool[2]);
            effectPackageModelStrengthLengthRepresentations[effectPackageModel][0] = effectPackageModel.length_Float != 0; // if length is represented as a float this is true
            effectPackageModelStrengthLengthRepresentations[effectPackageModel][1] = effectPackageModel.strength_Float != 0; // if strength is represented as a float this is true
        }
        effectPackageModelOpenStatuses[effectPackageModel] = EditorGUILayout.BeginToggleGroup(label, effectPackageModelOpenStatuses[effectPackageModel]);
        if (effectPackageModelOpenStatuses[effectPackageModel])
        {
            effectPackageModel.info = EditorGUILayout.TextArea(effectPackageModel.info, new GUILayoutOption[] { GUILayout.MaxHeight(32) });
            effectPackageModel.subactionEffectType = (SubactionEffectType)EditorGUILayout.EnumPopup("Effect Type", effectPackageModel.subactionEffectType);
            effectPackageModel.hitStat = (LogicalStatType)EditorGUILayout.EnumPopup("Logical Hit Stat", effectPackageModel.hitStat);
            effectPackageModel.evadeStat = (LogicalStatType)EditorGUILayout.EnumPopup("Logical Evade Stat", effectPackageModel.evadeStat);
            Field("Success Determinant Index", ref effectPackageModel.tieSuccessToEffectIndex);
            Field("Base Success Rate", ref effectPackageModel.baseSuccessRate);
            Field("Base AI Score Value", ref effectPackageModel.baseAIScoreValue);
            EditorGUILayout.BeginHorizontal();
            bool lenAsFloat = effectPackageModelStrengthLengthRepresentations[effectPackageModel][0];
            if (lenAsFloat)
            {
                effectPackageModel.length_Byte = 0;
                Field("Length", ref effectPackageModel.length_Float);
                if (GUILayout.Button("Switch to byte")) effectPackageModelStrengthLengthRepresentations[effectPackageModel][0] = false;
            }
            else
            {
                effectPackageModel.length_Float = 0;
                Field("Length", ref effectPackageModel.length_Byte);
                if (GUILayout.Button("Switch to float")) effectPackageModelStrengthLengthRepresentations[effectPackageModel][0] = true;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            bool strAsFloat = effectPackageModelStrengthLengthRepresentations[effectPackageModel][1];
            if (strAsFloat)
            {
                effectPackageModel.strength_Int = 0;
                Field("Strength", ref effectPackageModel.strength_Float);
                if (GUILayout.Button("Switch to int")) effectPackageModelStrengthLengthRepresentations[effectPackageModel][1] = false;
            }
            else
            {
                effectPackageModel.strength_Float = 0;
                Field("Strength", ref effectPackageModel.strength_Int);
                if (GUILayout.Button("Switch to float")) effectPackageModelStrengthLengthRepresentations[effectPackageModel][1] = true;
            }
            EditorGUILayout.EndHorizontal();
            effectPackageModel.applyEvenIfSubactionMisses = EditorGUILayout.Toggle("Apply even if subaction misses", effectPackageModel.applyEvenIfSubactionMisses);
            EventBlockPanel("Event Block", effectPackageModel.eventBlockModel, () => { effectPackageModel.eventBlockModel = new EventBlockModel(effectPackageModel.xmlNode, "eventBlock"); }, () => { effectPackageModel.eventBlockModel = null; });
            if (GUILayout.Button("Delete EffectPackage"))
            {
                effectPackageModelOpenStatuses.Remove(effectPackageModel);
                effectPackageModelStrengthLengthRepresentations.Remove(effectPackageModel);
                removeCallback();
            }
        }
        EditorGUILayout.EndToggleGroup();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Separator();
    }

    /// <summary>
    /// Adds a set of widgets for the given event block model.
    /// </summary>
    void EventBlockPanel (string label, EventBlockModel eventBlockModel, Action createCallback, Action removeCallback)
    {
        EditorGUILayout.BeginHorizontal();
        if (eventBlockModel == null)
        {
            if (GUILayout.Button("Create EventBlock " + label)) createCallback();
        }
        else
        {
            GUILayout.Space(10);
            EditorGUILayout.BeginVertical();
            if (!eventBlockModelOpenStatuses.ContainsKey(eventBlockModel)) eventBlockModelOpenStatuses.Add(eventBlockModel, true);      
            eventBlockModelOpenStatuses[eventBlockModel] = EditorGUILayout.BeginToggleGroup(label, eventBlockModelOpenStatuses[eventBlockModel]);
            if (eventBlockModelOpenStatuses[eventBlockModel])
            {
                if (GUILayout.Button("Remove"))
                {
                    eventBlockModelOpenStatuses.Remove(eventBlockModel);
                    removeCallback();
                }
                // Anim event models
                animEventModelsToRemove.Clear();
                for (int i = 0; i < eventBlockModel.animEventModels.Count; i++)
                {
                    EditorGUILayout.LabelField("AnimEvent " + i, style);
                    eventBlockModel.animEventModels[i].info = EditorGUILayout.TextArea(eventBlockModel.animEventModels[i].info, new GUILayoutOption[] { GUILayout.MaxHeight(32) });
                    eventBlockModel.animEventModels[i].animEventType = (AnimEventType)EditorGUILayout.EnumPopup("AnimEvent Type", eventBlockModel.animEventModels[i].animEventType);
                    eventBlockModel.animEventModels[i].fallbackType = (AnimEventType)EditorGUILayout.EnumPopup("Fallback Type", eventBlockModel.animEventModels[i].fallbackType);
                    eventBlockModel.animEventModels[i].flags = (AnimEvent.Flags)EditorGUILayout.EnumMaskPopup("AnimEvent Flags", eventBlockModel.animEventModels[i].flags);
                    Field("Priority", ref eventBlockModel.animEventModels[i].priority);
                    if (GUILayout.Button("Remove")) animEventModelsToRemove.Add(eventBlockModel.animEventModels[i]);
                    EditorGUILayout.Separator();
                }
                for (int i = 0; i < animEventModelsToRemove.Count; i++) eventBlockModel.animEventModels.Remove(animEventModelsToRemove[i]);
                if (GUILayout.Button("Add AnimEvent")) eventBlockModel.animEventModels.Add(new EventBlockModel.AnimEventModel(eventBlockModel));
                EditorGUILayout.Separator();
                // Audio event models
                audioEventModelsToRemove.Clear();
                for (int i = 0; i < eventBlockModel.audioEventModels.Count; i++)
                {
                    EditorGUILayout.LabelField("<b>AudioEvent " + i + "</b>");
                    eventBlockModel.audioEventModels[i].info = EditorGUILayout.TextArea(eventBlockModel.audioEventModels[i].info, new GUILayoutOption[] { GUILayout.MaxHeight(32) });
                    eventBlockModel.audioEventModels[i].audioEventType = (AudioEventType)EditorGUILayout.EnumPopup("AudioEvent Type", eventBlockModel.audioEventModels[i].audioEventType);
                    eventBlockModel.audioEventModels[i].fallbackType = (AudioEventType)EditorGUILayout.EnumPopup("Fallback Type", eventBlockModel.audioEventModels[i].fallbackType);
                    eventBlockModel.audioEventModels[i].clipType = (AudioSourceType)EditorGUILayout.EnumPopup("Clip Type", eventBlockModel.audioEventModels[i].clipType);
                    eventBlockModel.audioEventModels[i].flags = (AudioEvent.Flags)EditorGUILayout.EnumMaskPopup("AudioEvent Flags", eventBlockModel.audioEventModels[i].flags);
                    Field("Priority", ref eventBlockModel.audioEventModels[i].priority);
                    if (GUILayout.Button("Remove")) audioEventModelsToRemove.Add(eventBlockModel.audioEventModels[i]);
                    EditorGUILayout.Separator();
                }
                for (int i = 0; i < audioEventModelsToRemove.Count; i++) eventBlockModel.audioEventModels.Remove(audioEventModelsToRemove[i]);
                if (GUILayout.Button("Add AudioEvent")) eventBlockModel.audioEventModels.Add(new EventBlockModel.AudioEventModel(eventBlockModel));
                EditorGUILayout.Separator();
                // FX event models
                fxEventModelsToRemove.Clear();
                for (int i = 0; i < eventBlockModel.fxEventModels.Count; i++)
                {
                    EditorGUILayout.LabelField("<b>FXEvent " + i + "</b>");
                    eventBlockModel.fxEventModels[i].info = EditorGUILayout.TextArea(eventBlockModel.fxEventModels[i].info, new GUILayoutOption[] { GUILayout.MaxHeight(32) });
                    eventBlockModel.fxEventModels[i].fxEventType = (FXEventType)EditorGUILayout.EnumPopup("FXEvent Type", eventBlockModel.fxEventModels[i].fxEventType);
                    eventBlockModel.fxEventModels[i].flags = (FXEvent.Flags)EditorGUILayout.EnumMaskPopup("FXEvent Flags", eventBlockModel.fxEventModels[i].flags);
                    Field("Priority", ref eventBlockModel.fxEventModels[i].priority);
                    if (GUILayout.Button("Remove")) fxEventModelsToRemove.Add(eventBlockModel.fxEventModels[i]);
                    EditorGUILayout.Separator();
                }
                for (int i = 0; i < fxEventModelsToRemove.Count; i++) eventBlockModel.fxEventModels.Remove(fxEventModelsToRemove[i]);
                if (GUILayout.Button("Add FXEvent")) eventBlockModel.fxEventModels.Add(new EventBlockModel.FXEventModel(eventBlockModel));
            }
            EditorGUILayout.EndToggleGroup();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Separator();
        }
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Make a horizontal bool field with label.
    /// </summary>
    void Field (string label, ref bool field)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(label);
        bool b = EditorGUILayout.Toggle(field);
        if (b != field) field = b;
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Make a horizontal byte field with label.
    /// </summary>
    void Field (string label, ref byte field)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(label);
        int i = EditorGUILayout.IntField(field);
        if (i > byte.MaxValue) i = byte.MaxValue;
        if (i < byte.MinValue) i = byte.MinValue;
        byte b = (byte)i;
        if (b != field) field = b;
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Make a horizontal float field with label.
    /// </summary>
    void Field (string label, ref float field)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(label);
        float f = EditorGUILayout.FloatField(field);
        if (f != field) field = f;
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Make a horizontal int field with label.
    /// </summary>
    void Field (string label, ref int field)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(label);
        int i = EditorGUILayout.IntField(field);
        if (i != field) field = i;
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Make a horizontal sbyte field with label.
    /// </summary>
    void Field(string label, ref sbyte field)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(label);
        int i = EditorGUILayout.IntField(field);
        if (i > sbyte.MaxValue) i = sbyte.MaxValue;
        if (i < sbyte.MinValue) i = sbyte.MinValue;
        sbyte b = (sbyte)i;
        if (b != field) field = b;
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Make a horizontal string field with label.
    /// </summary>
    void Field (string label, ref string field)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(label);
        string s = EditorGUILayout.TextField(field);
        if (s != field) field = s;
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Adds a set of widgets for the given subaction model.
    /// </summary>
    void SubactionPanel(string label, SubactionModel subactionModel, Action removeCallback)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(16);
        EditorGUILayout.BeginVertical();
        if (!subactionModelOpenStatuses.ContainsKey(subactionModel)) subactionModelOpenStatuses.Add(subactionModel, true);
        subactionModelOpenStatuses[subactionModel] = EditorGUILayout.BeginToggleGroup(label + ": " + subactionModel.subactionName, subactionModelOpenStatuses[subactionModel]);
        if (subactionModelOpenStatuses[subactionModel])
        {
            subactionModel.info = EditorGUILayout.TextArea(subactionModel.info, new GUILayoutOption[] { GUILayout.MaxHeight(32) });
            Field("Subaction Name", ref subactionModel.subactionName);
            Field("Predicate Name", ref subactionModel.predicateName);
            Field("Success Determinant Name", ref subactionModel.successDeterminantName);
            Field("Damage Determinant Name", ref subactionModel.damageDeterminantName);
            Field("Base Damage", ref subactionModel.baseDamage);
            Field("Base Accuracy", ref subactionModel.baseAccuracy);
            Field("Use Alternate Targeting Info", ref subactionModel.useAlternateTargetSet);
            subactionModel.damageTypes = (DamageTypeFlags)EditorGUILayout.EnumMaskPopup("Damage Types", subactionModel.damageTypes);
            subactionModel.categoryFlags = (BattleActionCategoryFlags)EditorGUILayout.EnumMaskPopup("Category Flags", subactionModel.categoryFlags);
            subactionModel.atkStat = (LogicalStatType)EditorGUILayout.EnumPopup("Logical Attack Stat", subactionModel.atkStat);
            subactionModel.defStat = (LogicalStatType)EditorGUILayout.EnumPopup("Logical Defense Stat", subactionModel.defStat);
            subactionModel.hitStat = (LogicalStatType)EditorGUILayout.EnumPopup("Logical Hit Stat", subactionModel.hitStat);
            subactionModel.evadeStat = (LogicalStatType)EditorGUILayout.EnumPopup("Logical Evade Stat", subactionModel.evadeStat);
            EventBlockPanel("Event Block", subactionModel.eventBlockModel, () => { subactionModel.eventBlockModel = new EventBlockModel(subactionModel.xmlNode, "eventBlock"); }, () => { subactionModel.eventBlockModel = null; });
            effectPackageModelsToRemove.Clear();
            for (int i = 0; i < subactionModel.effectPackageModels.Count; i++) EffectPackagePanel("Effect Package " + i, subactionModel.effectPackageModels[i], () => { effectPackageModelsToRemove.Add(subactionModel.effectPackageModels[i]); });
            for (int i = 0; i < effectPackageModelsToRemove.Count; i++) subactionModel.effectPackageModels.Remove(effectPackageModelsToRemove[i]);
            if (GUILayout.Button("Add new EffectPackage")) subactionModel.effectPackageModels.Add(new EffectPackageModel(subactionModel));
            if (GUILayout.Button("Delete subaction"))
            {
                subactionModelOpenStatuses.Remove(subactionModel);
                removeCallback();
            }
        }
        EditorGUILayout.EndToggleGroup();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Separator();
    }

}
