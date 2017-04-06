using UnityEngine;
using System;
using System.Xml;

namespace CnfBattleSys
{
    public class ActionDatabase
    {
        private static BattleAction[] _actions;
        private static readonly BattleAction.Subaction[] defaultSubactionArray = { new BattleAction.Subaction(0, 0, false, AnimEventType.None, AnimEventType.None, LogicalStatType.None, LogicalStatType.None, LogicalStatType.None, LogicalStatType.None, -1, -1, BattleActionCategoryFlags.None,new BattleAction.Subaction.FXPackage[0], DamageTypeFlags.None) };
        public static readonly BattleAction defaultBattleAction = new BattleAction(ActionType.InvalidAction, 0, 0, 0, 0, 0, 0, TargetSideFlags.None, TargetSideFlags.None, ActionTargetType.None, ActionTargetType.None, AnimEventType.None, AnimEventType.None, AnimEventType.None, AnimEventType.None, AnimEventType.None,
                                                       BattleActionCategoryFlags.None, defaultSubactionArray);

        /// <summary>
        /// Loads in and parses all of the xml files, populates the action dataset.
        /// This should only ever run once.
        /// </summary>
        public static void Load()
        {
            XmlDocument doc = new XmlDocument();
            XmlNode workingNode = doc.DocumentElement;
            int c = Enum.GetValues(typeof(ActionType)).Length - 1;
            _actions = new BattleAction[c];
            for (int a = 0; a < c; a++) _actions[a] = ImportActionDefWithID((ActionType)a, doc, workingNode);
        }

        /// <summary>
        /// Loads in an action def from the XML file.
        /// </summary>
        private static BattleAction ImportActionDefWithID(ActionType actionID, XmlDocument doc, XmlNode workingNode)
        {
            const string actionDefsResourcePath = "Battle/ActionDefs/";
            Debug.Log("Loading action def from XML file: " + actionDefsResourcePath + actionID.ToString()); // unconditional logging is OK because this is going to move to pre-build before I'd need to strip it anyhow
            TextAsset unreadFileBuffer = Resources.Load<TextAsset>(actionDefsResourcePath + actionID.ToString());
            if (unreadFileBuffer != null) doc.LoadXml(unreadFileBuffer.text);
            else
            {
                Debug.Log(actionID.ToString() + " has no action def file, so the invalid action placeholder was loaded instead.");
                return defaultBattleAction;
            }
            XmlNode rootNode = doc.DocumentElement;
            Action<string> actOnNode = (node) =>
            {
                workingNode = rootNode.SelectSingleNode(node);
                if (workingNode == null) throw new Exception(actionID.ToString() + " has no node " + node);
            };
            actOnNode("//baseAOERadius");
            float baseAOERadius = float.Parse(workingNode.InnerText);
            actOnNode("//baseDelay");
            float baseDelay = float.Parse(workingNode.InnerText);
            actOnNode("//baseFollowthroughStanceChangeDelay");
            float baseFollowthroughStanceChangeDelay = float.Parse(workingNode.InnerText);
            actOnNode("//baseMinimumTargetingDistance");
            float baseMinimumTargetingDistance = float.Parse(workingNode.InnerText);
            actOnNode("//baseTargetingRange");
            float baseTargetingRange = float.Parse(workingNode.InnerText);
            actOnNode("//baseSPCost");
            byte baseSPCost = byte.Parse(workingNode.InnerText);
            actOnNode("//targetingSideFlags");
            TargetSideFlags targetingSideFlags = DBTools.ParseTargetSideFlags(workingNode.InnerText);
            actOnNode("//targetingType");
            ActionTargetType targetingType = DBTools.ParseActionTargetType(workingNode.InnerText);
            TargetSideFlags alternateTargetingSideFlags = TargetSideFlags.None;
            ActionTargetType alternateTargetType = ActionTargetType.None;
            workingNode = rootNode.SelectSingleNode("//alternateTargets");
            if (workingNode != null)
            {
                XmlNode subNode = workingNode.SelectSingleNode("//targetingType");
                if (subNode == null) throw new Exception("Malformed action def: has alternate targets, but no alternate targeting type.");
                alternateTargetType = DBTools.ParseActionTargetType(subNode.InnerText);
                subNode = workingNode.SelectSingleNode("//targetingSideFlags");
                if (subNode == null) throw new Exception("Malformed action def: has alternate targets, but no alternate targeting side flags.");
                alternateTargetingSideFlags = DBTools.ParseTargetSideFlags(subNode.InnerText);
            }
            XmlNodeList SubactionsList = rootNode.SelectNodes("//subaction");
            if (SubactionsList.Count < 1) throw new Exception("Battle action " + actionID.ToString() + " has no defined Subactions!");
            BattleAction.Subaction[] Subactions = new BattleAction.Subaction[SubactionsList.Count];
            for (int s = 0; s < Subactions.Length; s++)
            {
                XmlNode SubactionNode = SubactionsList[s];
                Subactions[s] = XmlNodeToSubaction(SubactionNode, workingNode, s, actionID);
                if (Subactions[s].thisSubactionSuccessTiedToSubactionAtIndex > -1)
                {
                    if (Subactions[s].useAlternateTargetSet != Subactions[Subactions[s].thisSubactionSuccessTiedToSubactionAtIndex].useAlternateTargetSet)
                    {
                        if (alternateTargetType != ActionTargetType.Self && targetingType != ActionTargetType.Self) // we have a special case for tying multiple action successes to one on yourself or vice verse
                            throw new Exception("Illegal subaction config: tried to tie subaction " + s + " to subaction " + Subactions[s].thisSubactionSuccessTiedToSubactionAtIndex + ", but their target sets are mismatched.");
                            // but if that's not true this will break horribly, so we crash to keep that from happening
                    }
                }
            }
            actOnNode("//animSkipTargetHitAnim");
            AnimEventType animSkipTargetHitAnim = DBTools.ParseAnimEventType(workingNode.InnerText);
            actOnNode("//onActionEndTargetAnim");
            AnimEventType onActionEndTargetAnim = DBTools.ParseAnimEventType(workingNode.InnerText);
            actOnNode("//onActionEndUserAnim");
            AnimEventType onActionEndUserAnim = DBTools.ParseAnimEventType(workingNode.InnerText);
            actOnNode("//onActionUseTargetAnim");
            AnimEventType onActionUseTargetAnim = DBTools.ParseAnimEventType(workingNode.InnerText);
            actOnNode("//onActionUseUserAnim");
            AnimEventType onActionUseUserAnim = DBTools.ParseAnimEventType(workingNode.InnerText);
            actOnNode("//categoryFlags");
            BattleActionCategoryFlags categoryFlags = DBTools.ParseBattleActionCategoryFlags(workingNode.InnerText);
            Resources.UnloadAsset(unreadFileBuffer);
            return new BattleAction(actionID, baseAOERadius, baseDelay, baseFollowthroughStanceChangeDelay, baseMinimumTargetingDistance, baseTargetingRange, baseSPCost, alternateTargetingSideFlags, targetingSideFlags,
                alternateTargetType, targetingType, animSkipTargetHitAnim, onActionEndTargetAnim, onActionEndUserAnim, onActionUseTargetAnim, onActionUseUserAnim, categoryFlags, Subactions);

        }

        /// <summary>
        /// Parses an XML node and spits out a Subaction.
        /// </summary>
        private static BattleAction.Subaction XmlNodeToSubaction(XmlNode SubactionNode, XmlNode workingNode, int index, ActionType actionID)
        {
            Func<string> exceptionSubactionIDStr = delegate { return "Action " + actionID.ToString() + ", Subaction " + index.ToString(); };
            Action<string> actOnNode = (node) =>
            {
                workingNode = SubactionNode.SelectSingleNode(node);
                if (workingNode == null) throw new Exception(exceptionSubactionIDStr() + " has no node " + node);
            };
            XmlNodeList fxList = SubactionNode.SelectNodes("//fxPackage");
            BattleAction.Subaction.FXPackage[] fx = new BattleAction.Subaction.FXPackage[fxList.Count];
            sbyte thisSubactionDamageTiedToSubactionAtIndex = -1;
            sbyte thisSubactionSuccessTiedToSubactionAtIndex = -1;
            if (fx != null)
            {
                for (int f = 0; f < fx.Length; f++)
                {
                    Func<string> exceptionFXPackageIDStr = delegate { return "Action " + actionID.ToString() + ", Subaction " + index.ToString() + ", FXpackage " + f.ToString(); };
                    XmlNode fxNode = fxList[f];
                    fx[f] = XmlNodeToFXPackage(fxNode, workingNode, exceptionFXPackageIDStr, f);
                }
            }
            actOnNode("//baseDamage");
            int baseDamage = int.Parse(workingNode.InnerText);
            actOnNode("//baseAccuracy");
            float baseAccuracy = float.Parse(workingNode.InnerText);
            actOnNode("//useAlternateTargetSet");
            bool useAlternateTargetSet = bool.Parse(workingNode.InnerText);
            actOnNode("//onSubactionHitTargetAnim");
            AnimEventType onSubactionHitTargetAnim = DBTools.ParseAnimEventType(workingNode.InnerText);
            actOnNode("//onSubactionExecuteUserAnim");
            AnimEventType onSubactionExecuteUserAnim = DBTools.ParseAnimEventType(workingNode.InnerText);
            actOnNode("//atkStat");
            LogicalStatType atkStat = DBTools.ParseLogicalStatType(workingNode.InnerText);
            actOnNode("//defStat");
            LogicalStatType defStat = DBTools.ParseLogicalStatType(workingNode.InnerText);
            actOnNode("//hitStat");
            LogicalStatType hitStat = DBTools.ParseLogicalStatType(workingNode.InnerText);
            actOnNode("//evadeStat");
            LogicalStatType evadeStat = DBTools.ParseLogicalStatType(workingNode.InnerText);
            actOnNode("//damageTypes");
            DamageTypeFlags damageTypes = DBTools.ParseDamageTypeFlags(workingNode.InnerText);
            actOnNode("//categoryFlags");
            BattleActionCategoryFlags categoryFlags = DBTools.ParseBattleActionCategoryFlags(workingNode.InnerText);
            workingNode = SubactionNode.SelectSingleNode("//thisSubactionDamageTiedToSubactionAtIndex");
            if (workingNode != null)
            {
                thisSubactionDamageTiedToSubactionAtIndex = sbyte.Parse(workingNode.InnerText);
                if (thisSubactionDamageTiedToSubactionAtIndex < 0) throw new Exception(exceptionSubactionIDStr() + " tries to tie itself to an invalid Subaction index!");
                else if (thisSubactionDamageTiedToSubactionAtIndex >= index) throw new Exception(exceptionSubactionIDStr() + " tries to tie itself to a Subaction index that doesn't precede it!");
            }
            workingNode = SubactionNode.SelectSingleNode("//thisSubactionSuccessTiedToSubactionAtIndex");
            if (workingNode != null)
            {
                thisSubactionSuccessTiedToSubactionAtIndex = sbyte.Parse(workingNode.InnerText);
                if (thisSubactionSuccessTiedToSubactionAtIndex < 0) throw new Exception(exceptionSubactionIDStr() + " tries to tie itself to an invalid Subaction index!");
                else if (thisSubactionSuccessTiedToSubactionAtIndex >= index) throw new Exception(exceptionSubactionIDStr() + " tries to tie itself to a Subaction index that doesn't precede it!");
            }
            return new BattleAction.Subaction(baseDamage, baseAccuracy, useAlternateTargetSet, onSubactionHitTargetAnim, onSubactionExecuteUserAnim, atkStat, defStat, hitStat, evadeStat,
                thisSubactionDamageTiedToSubactionAtIndex, thisSubactionSuccessTiedToSubactionAtIndex, categoryFlags ,fx, damageTypes);
        }

        /// <summary>
        /// Parses the XML node defining an FXPackage's parameters and spits out an FXPackage.
        /// </summary>
        private static BattleAction.Subaction.FXPackage XmlNodeToFXPackage(XmlNode fxNode, XmlNode workingNode, Func<string> exceptionFXPackageIDStr, int index)
        {
            float fxStrengthFloat = float.NaN;
            float fxLengthFloat = float.NaN;
            int fxStrengthInt = 0;
            byte fxLengthByte = 0;
            sbyte thisFXSuccessTiedToFXAtIndex = -1;
            Action<string> actOnNode = (node) =>
            {
                workingNode = fxNode.SelectSingleNode(node);
                if (workingNode == null) throw new Exception(exceptionFXPackageIDStr() + " has no node " + node);
            };
            actOnNode("//fxType");
            SubactionFXType fxType = DBTools.ParseSubactionFXType(workingNode.InnerText);
            actOnNode("//fxHitStat");
            LogicalStatType fxHitStat = DBTools.ParseLogicalStatType(workingNode.InnerText);
            actOnNode("//fxEvadeStat");
            LogicalStatType fxEvadeStat = DBTools.ParseLogicalStatType(workingNode.InnerText);
            bool applyEvenIfSubactionMisses = true;
            workingNode = fxNode.SelectSingleNode("//applyEvenIfSubactionMisses");
            if (workingNode != null) applyEvenIfSubactionMisses = bool.Parse(workingNode.InnerText);
            actOnNode("//baseSuccessRate");
            float baseSuccessRate = float.Parse(workingNode.InnerText);
            if (baseSuccessRate > 1.0f) baseSuccessRate = 1.0f;
            else if (baseSuccessRate < 0.0f) baseSuccessRate = 0.0f;
            workingNode = fxNode.SelectSingleNode("//fxLength_Float");
            if (workingNode != null) fxLengthFloat = float.Parse(workingNode.InnerText);
            else
            {
                actOnNode("//fxLength_Int");
                fxLengthByte = byte.Parse(workingNode.InnerText);
            }
            workingNode = fxNode.SelectSingleNode("//fxStrength_Float");
            if (workingNode != null) fxStrengthFloat = float.Parse(workingNode.InnerText);
            else
            {
                actOnNode("//fxStrength_Int");
                fxStrengthInt = int.Parse(workingNode.InnerText);
            }
            workingNode = fxNode.SelectSingleNode("//thisFXSuccessTiedToFXAtIndex");
            if (workingNode != null)
            {
                thisFXSuccessTiedToFXAtIndex = sbyte.Parse(workingNode.InnerText);
                if (thisFXSuccessTiedToFXAtIndex < 0) throw new Exception(exceptionFXPackageIDStr() + " tries to tie itself to an invalid FX index!");
                else if (thisFXSuccessTiedToFXAtIndex >= index) throw new Exception(exceptionFXPackageIDStr() + " tries to tie itself to an FX index that doesn't precede it!");
            }
            return new BattleAction.Subaction.FXPackage(fxType, fxHitStat, fxEvadeStat, applyEvenIfSubactionMisses, baseSuccessRate, fxLengthFloat, fxStrengthFloat, fxLengthByte, fxStrengthInt, thisFXSuccessTiedToFXAtIndex);
        }

        /// <summary>
        /// Gets a BattleAction corresponding to actionID from the dataset.
        /// </summary>
        public static BattleAction Get(ActionType actionID)
        {
            return _actions[(int)actionID];
        }
    }
}