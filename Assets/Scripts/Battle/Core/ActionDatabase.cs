using UnityEngine;
using System;
using System.Collections.Generic;
using System.Xml;

namespace CnfBattleSys
{
    /// <summary>
    /// Static class that stores and handles the action datatable, plus utilities for getting icons and etc. based on action ID.
    /// </summary>
    public static class ActionDatabase
    {
        private readonly static EventBlock emptyEventBlock = new EventBlock(new AnimEvent[0], new AudioEvent[0], new FXEvent[0]);
        private static BattleAction[] _actions;
        private static readonly Dictionary<string, BattleAction.Subaction> defaultSubactionsDict = new Dictionary<string, BattleAction.Subaction>();
        const string actionIconsResourcePath = "Battle/2D/UI/AWIcon/Action/";

        /// <summary>
        /// Contains special-case action defs that exust outside of the main table we populate from the XML files.
        /// </summary>
        public static class SpecialActions
        {
            /// <summary>
            /// The number of special action defs. Since these have their own (less than zero) entries in the ActionType enum, we need to subtract the number of special actions from the total when determining how many spaces there need to be 
            /// </summary>
            public const int count = 2;

            /// <summary>
            /// The default battle action entry, used to populate invalid entries on the table or when we need a placeholder action entry somewhere else in the battle system.
            /// </summary>
            public static readonly BattleAction defaultBattleAction = new BattleAction(emptyEventBlock, emptyEventBlock, emptyEventBlock, ActionType.InvalidAction, 0, 0, 0, 0, 0, 0, TargetSideFlags.None, TargetSideFlags.None, 
                                                                                       ActionTargetType.None, ActionTargetType.None, BattleActionCategoryFlags.None, defaultSubactionsDict);

            /// <summary>
            /// Another empty placeholder battle action - all we care about with any of these placeholder actions is _identity_. They don't do anything.
            /// This actually gets plugged into the table, so don't count it as part of the special actions count above. None is index 0, not a negative index.
            /// </summary>
            public static readonly BattleAction noneBattleAction = new BattleAction(emptyEventBlock, emptyEventBlock, emptyEventBlock, ActionType.None, 0, 0, 0, 0, 0, 0, TargetSideFlags.None, TargetSideFlags.None, 
                                                                                    ActionTargetType.None, ActionTargetType.None, BattleActionCategoryFlags.None, defaultSubactionsDict);

            /// <summary>
            /// The entry for the "break own stance" entry, which is a placeholder just like the other two. We don't "execute" this action in the normal sense - 
            /// if you go into action execution with this action, you go through some hardcoded special-case behavior instead of executing an action def.
            /// </summary>
            public static readonly BattleAction selfStanceBreakAction = new BattleAction(emptyEventBlock, emptyEventBlock, emptyEventBlock, ActionType.INTERNAL_BreakOwnStance, 0, 0, 0, 0, 0, 0, TargetSideFlags.None, TargetSideFlags.None, 
                                                                                         ActionTargetType.None, ActionTargetType.None, BattleActionCategoryFlags.None, defaultSubactionsDict);
        }

        static ActionDatabase ()
        {
            Load();
        }

        /// <summary>
        /// Loads in and parses all of the xml files, populates the action dataset.
        /// This should only ever run once.
        /// </summary>
        public static void Load()
        {
            XmlDocument doc = new XmlDocument();
            XmlNode workingNode = doc.DocumentElement;
            int c = Enum.GetValues(typeof(ActionType)).Length - SpecialActions.count;
            _actions = new BattleAction[c];
            _actions[0] = SpecialActions.noneBattleAction;
            for (int a = 1; a < c; a++) _actions[a] = ImportActionDefWithID((ActionType)a, doc, workingNode);
        }

        /// <summary>
        /// This is basically dead and I'm just getting it to not-break-the-build until I get the new action datatable online.
        /// Loads in an action def from the XML file.
        /// </summary>
        private static BattleAction ImportActionDefWithID(ActionType actionID, XmlDocument doc, XmlNode workingNode)
        {
            const string actionDefsResourcePath = "Battle/ActionDefs/";
            TextAsset unreadFileBuffer = Resources.Load<TextAsset>(actionDefsResourcePath + actionID.ToString());
            if (unreadFileBuffer != null) doc.LoadXml(unreadFileBuffer.text);
            else
            {
                Debug.Log(actionID.ToString() + " has no action def file, so the invalid action placeholder was loaded instead.");
                return SpecialActions.defaultBattleAction;
            }
            XmlNode rootNode = doc.DocumentElement;
            Action<string> actOnNode = (node) =>
            {
                workingNode = rootNode.SelectSingleNode(node);
                if (workingNode == null) Util.Crash(new Exception(actionID.ToString() + " has no node " + node));
            };
            actOnNode("baseAOERadius");
            float baseAOERadius = float.Parse(workingNode.InnerText);
            actOnNode("baseDelay");
            float baseDelay = float.Parse(workingNode.InnerText);
            actOnNode("baseFollowthroughStanceChangeDelay");
            float baseFollowthroughStanceChangeDelay = float.Parse(workingNode.InnerText);
            actOnNode("baseMinimumTargetingDistance");
            float baseMinimumTargetingDistance = float.Parse(workingNode.InnerText);
            actOnNode("baseTargetingRange");
            float baseTargetingRange = float.Parse(workingNode.InnerText);
            actOnNode("baseSPCost");
            byte baseSPCost = byte.Parse(workingNode.InnerText);
            actOnNode("animSkip");
            EventBlock animSkip = DBTools.GetEventBlockFromXml(workingNode);
            actOnNode("onConclusion");
            EventBlock onConclusion = DBTools.GetEventBlockFromXml(workingNode);
            actOnNode("onStart");
            EventBlock onStart = DBTools.GetEventBlockFromXml(workingNode);
            actOnNode("targetingSideFlags");
            TargetSideFlags targetingSideFlags = DBTools.ParseTargetSideFlags(workingNode.InnerText);
            actOnNode("targetingType");
            ActionTargetType targetingType = DBTools.ParseActionTargetType(workingNode.InnerText);
            TargetSideFlags alternateTargetingSideFlags = TargetSideFlags.None;
            ActionTargetType alternateTargetType = ActionTargetType.None;
            workingNode = rootNode.SelectSingleNode("alternateTargets");
            if (workingNode != null)
            {
                XmlNode subNode = workingNode.SelectSingleNode("targetingType");
                if (subNode == null) Util.Crash(new Exception("Malformed action def: has alternate targets, but no alternate targeting type."));
                alternateTargetType = DBTools.ParseActionTargetType(subNode.InnerText);
                subNode = workingNode.SelectSingleNode("targetingSideFlags");
                if (subNode == null) Util.Crash(new Exception("Malformed action def: has alternate targets, but no alternate targeting side flags."));
                alternateTargetingSideFlags = DBTools.ParseTargetSideFlags(subNode.InnerText);
            }
            XmlNodeList SubactionsList = rootNode.SelectNodes("subaction");
            if (SubactionsList.Count < 1) Util.Crash(new Exception("Battle action " + actionID.ToString() + " has no defined Subactions!"));
            BattleAction.Subaction[] Subactions = new BattleAction.Subaction[SubactionsList.Count];
            for (int s = 0; s < Subactions.Length; s++)
            {
                XmlNode SubactionNode = SubactionsList[s];
                Subactions[s] = XmlNodeToSubaction(SubactionNode, workingNode, s, actionID);
                //if (Subactions[s].thisSubactionSuccessTiedToSubactionAtIndex > -1)
                //{
                //    if (Subactions[s].useAlternateTargetSet != Subactions[Subactions[s].thisSubactionSuccessTiedToSubactionAtIndex].useAlternateTargetSet)
                //    {
                //        if (alternateTargetType != ActionTargetType.Self && targetingType != ActionTargetType.Self) // we have a special case for tying multiple action successes to one on yourself or vice verse
                //            Util.Crash(new Exception("Illegal subaction config: tried to tie subaction " + s + " to subaction " + Subactions[s].thisSubactionSuccessTiedToSubactionAtIndex + ", but their target sets are mismatched."));
                //            // but if that's not true this will break horribly, so we crash to keep that from happening
                //    }
                //}
            }
            actOnNode("categoryFlags");
            BattleActionCategoryFlags categoryFlags = DBTools.ParseBattleActionCategoryFlags(workingNode.InnerText);
            Resources.UnloadAsset(unreadFileBuffer);
            return new BattleAction(animSkip, onConclusion, onStart, actionID, baseAOERadius, baseDelay, baseFollowthroughStanceChangeDelay, baseMinimumTargetingDistance, baseTargetingRange, baseSPCost, alternateTargetingSideFlags, targetingSideFlags,
                alternateTargetType, targetingType, categoryFlags, defaultSubactionsDict);

        }

        /// <summary>
        /// CORPSEY.
        /// Parses an XML node and spits out a Subaction.
        /// </summary>
        private static BattleAction.Subaction XmlNodeToSubaction(XmlNode SubactionNode, XmlNode workingNode, int index, ActionType actionID)
        {
            Func<string> exceptionSubactionIDStr = delegate { return "Action " + actionID.ToString() + ", Subaction " + index.ToString(); };
            Action<string> actOnNode = (node) =>
            {
                workingNode = SubactionNode.SelectSingleNode(node);
                if (workingNode == null) Util.Crash(new Exception(exceptionSubactionIDStr() + " has no node " + node));
            };
            XmlNodeList fxList = SubactionNode.SelectNodes("effectPackage");
            BattleAction.Subaction.EffectPackage[] fx = new BattleAction.Subaction.EffectPackage[fxList.Count];
            sbyte thisSubactionDamageTiedToSubactionAtIndex = -1;
            sbyte thisSubactionSuccessTiedToSubactionAtIndex = -1;
            if (fx != null)
            {
                for (int f = 0; f < fx.Length; f++)
                {
                    Func<string> exceptionFXPackageIDStr = delegate { return "Action " + actionID.ToString() + ", Subaction " + index.ToString() + ", FXpackage " + f.ToString(); };
                    XmlNode fxNode = fxList[f];
                    fx[f] = XmlNodeToEffectPackage(fxNode, workingNode, exceptionFXPackageIDStr, f);
                }
            }
            actOnNode("events");
            EventBlock eventBlock = DBTools.GetEventBlockFromXml(workingNode);
            actOnNode("baseDamage");
            int baseDamage = int.Parse(workingNode.InnerText);
            actOnNode("baseAccuracy");
            float baseAccuracy = float.Parse(workingNode.InnerText);
            actOnNode("useAlternateTargetSet");
            bool useAlternateTargetSet = bool.Parse(workingNode.InnerText);
            actOnNode("atkStat");
            LogicalStatType atkStat = DBTools.ParseLogicalStatType(workingNode.InnerText);
            actOnNode("defStat");
            LogicalStatType defStat = DBTools.ParseLogicalStatType(workingNode.InnerText);
            actOnNode("hitStat");
            LogicalStatType hitStat = DBTools.ParseLogicalStatType(workingNode.InnerText);
            actOnNode("evadeStat");
            LogicalStatType evadeStat = DBTools.ParseLogicalStatType(workingNode.InnerText);
            actOnNode("damageTypes");
            DamageTypeFlags damageTypes = DBTools.ParseDamageTypeFlags(workingNode.InnerText);
            actOnNode("categoryFlags");
            BattleActionCategoryFlags categoryFlags = DBTools.ParseBattleActionCategoryFlags(workingNode.InnerText);
            workingNode = SubactionNode.SelectSingleNode("//thisSubactionDamageTiedToSubactionAtIndex");
            if (workingNode != null)
            {
                thisSubactionDamageTiedToSubactionAtIndex = sbyte.Parse(workingNode.InnerText);
                if (thisSubactionDamageTiedToSubactionAtIndex < 0) Util.Crash(new Exception(exceptionSubactionIDStr() + " tries to tie itself to an invalid Subaction index!"));
                else if (thisSubactionDamageTiedToSubactionAtIndex >= index) Util.Crash(new Exception(exceptionSubactionIDStr() + " tries to tie itself to a Subaction index that doesn't precede it!"));
            }
            workingNode = SubactionNode.SelectSingleNode("//thisSubactionSuccessTiedToSubactionAtIndex");
            if (workingNode != null)
            {
                thisSubactionSuccessTiedToSubactionAtIndex = sbyte.Parse(workingNode.InnerText);
                if (thisSubactionSuccessTiedToSubactionAtIndex < 0) Util.Crash(new Exception(exceptionSubactionIDStr() + " tries to tie itself to an invalid Subaction index!"));
                else if (thisSubactionSuccessTiedToSubactionAtIndex >= index) Util.Crash(new Exception(exceptionSubactionIDStr() + " tries to tie itself to a Subaction index that doesn't precede it!"));
            }
            return default(BattleAction.Subaction);
            //return new BattleAction.Subaction(eventBlock, baseDamage, baseAccuracy, useAlternateTargetSet, atkStat, defStat, hitStat, evadeStat,
                //thisSubactionDamageTiedToSubactionAtIndex, thisSubactionSuccessTiedToSubactionAtIndex, categoryFlags, fx, damageTypes);
        }

        /// <summary>
        /// Parses the XML node defining an eEffectPackage's parameters and spits out an EffectPackage.
        /// </summary>
        private static BattleAction.Subaction.EffectPackage XmlNodeToEffectPackage(XmlNode effectNode, XmlNode workingNode, Func<string> exceptionFXPackageIDStr, int index)
        {
            float fxStrengthFloat = float.NaN;
            float fxLengthFloat = float.NaN;
            int fxStrengthInt = 0;
            byte fxLengthByte = 0;
            sbyte thisFXSuccessTiedToFXAtIndex = -1;
            Action<string> actOnNode = (node) =>
            {
                workingNode = effectNode.SelectSingleNode(node);
                if (workingNode == null) Util.Crash(new Exception(exceptionFXPackageIDStr() + " has no node " + node));
            };
            actOnNode("events");
            EventBlock eventBlock = DBTools.GetEventBlockFromXml(workingNode);
            actOnNode("effectType");
            SubactionEffectType effectType = DBTools.ParseSubactionFXType(workingNode.InnerText);
            actOnNode("hitStat");
            LogicalStatType hitStat = DBTools.ParseLogicalStatType(workingNode.InnerText);
            actOnNode("evadeStat");
            LogicalStatType evadeStat = DBTools.ParseLogicalStatType(workingNode.InnerText);
            bool applyEvenIfSubactionMisses = true;
            workingNode = effectNode.SelectSingleNode("applyEvenIfSubactionMisses");
            if (workingNode != null) applyEvenIfSubactionMisses = bool.Parse(workingNode.InnerText);
            actOnNode("baseSuccessRate");
            float baseSuccessRate = float.Parse(workingNode.InnerText);
            if (baseSuccessRate > 1.0f) baseSuccessRate = 1.0f;
            else if (baseSuccessRate < 0.0f) baseSuccessRate = 0.0f;
            workingNode = effectNode.SelectSingleNode("length_Float");
            if (workingNode != null) fxLengthFloat = float.Parse(workingNode.InnerText);
            else
            {
                actOnNode("length_Int");
                fxLengthByte = byte.Parse(workingNode.InnerText);
            }
            workingNode = effectNode.SelectSingleNode("strength_Float");
            if (workingNode != null) fxStrengthFloat = float.Parse(workingNode.InnerText);
            else
            {
                actOnNode("strength_Int");
                fxStrengthInt = int.Parse(workingNode.InnerText);
            }
            workingNode = effectNode.SelectSingleNode("tieSuccessToEffectIndex");
            if (workingNode != null)
            {
                thisFXSuccessTiedToFXAtIndex = sbyte.Parse(workingNode.InnerText);
                if (thisFXSuccessTiedToFXAtIndex < 0) Util.Crash(new Exception(exceptionFXPackageIDStr() + " tries to tie itself to an invalid FX index!"));
                else if (thisFXSuccessTiedToFXAtIndex >= index) Util.Crash(new Exception(exceptionFXPackageIDStr() + " tries to tie itself to an FX index that doesn't precede it!"));
            }
            return new BattleAction.Subaction.EffectPackage(eventBlock, effectType, hitStat, evadeStat, applyEvenIfSubactionMisses, baseSuccessRate, fxLengthFloat, fxStrengthFloat, fxLengthByte, fxStrengthInt, thisFXSuccessTiedToFXAtIndex, 0);
        }

        /// <summary>
        /// Gets a BattleAction corresponding to actionID from the dataset.
        /// </summary>
        public static BattleAction Get(ActionType actionID)
        {
            return _actions[(int)actionID];
        }

        /// <summary>
        /// Returns the Sprite from Resources/Battle/2D/UI/AWIcon/Action corresponding to this ID, if one exists,
        /// or the placeholder graphic otherwise.
        /// </summary>
        public static Sprite GetIconForActionID (ActionType actionID)
        {
            Sprite iconSprite = Resources.Load<Sprite>(actionIconsResourcePath + actionID.ToString());
            if (iconSprite == null) iconSprite = Resources.Load<Sprite>(actionIconsResourcePath + ActionType.InvalidAction.ToString());
            if (iconSprite == null) Util.Crash(new Exception("Couldn't get invalid action icon placeholder"));
            return iconSprite;
        }
    }
}