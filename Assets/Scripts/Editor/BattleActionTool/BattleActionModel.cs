#if UNITY_EDITOR
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using CnfBattleSys;
using UnityEditor;

namespace BattleActionTool
{
    /// <summary>
    /// Models a BattleAction.
    /// </summary>
    public class BattleActionModel
    {
        const string PATH = "Assets/_DATA_/BattleAction/";
        public readonly string filePath;
        public readonly XmlNode xmlNode;
        public List<string> subactionOrder = new List<string>();
        public List<SubactionModel> subactionModels = new List<SubactionModel>();
        public EventBlockModel animSkipModel;
        public EventBlockModel onConclusionModel;
        public EventBlockModel onStartModel;
        public ActionType actionID;
        public float baseAOERadius;
        public float baseDelay;
        public float baseFollowthroughStanceChangeDelay;
        public float baseMinimumTargetingDistance;
        public float baseTargetingRange;
        public byte baseSPCost;
        public TargetSideFlags alternateTargetSideFlags;
        public TargetSideFlags targetSideFlags;
        public ActionTargetType alternateTargetType;
        public ActionTargetType targetType;
        public BattleActionCategoryFlags categoryFlags;
        public string info;
        const string name = "BattleAction";
        private readonly XmlDocument doc;

        /// <summary>
        /// Creates new root BattleActionModel. If a definition for this action exists, load it in.
        /// </summary>
        public BattleActionModel(ActionType _actionID)
        {
            filePath = PATH + _actionID.ToString() +".xml";
            actionID = _actionID;
            doc = new XmlDocument();
            string xml = null;
            if (File.Exists(filePath))
            {
                StreamReader handle = File.OpenText(filePath);
                xml = handle.ReadToEnd();
                handle.Dispose();             
            }
            bool loaded = false;
            if (xml != null)
            {
                try
                {
                    doc.LoadXml(xml);
                    xmlNode = doc.DocumentElement;
                    loaded = true;
                }
                catch (XmlException) 
                {
                    loaded = false; // this is redundant, just for clarity - if you can't load the xml, you don't set loaded, and you don't try to set anything from the xml you didn't load
                }
            }
            if (loaded)
            {
                XmlNodeList subactionModelNodes = xmlNode.SelectNodes(SubactionModel.name);
                for (int i = 0; i < subactionModelNodes.Count; i++) subactionModels.Add(new SubactionModel(this, subactionModelNodes[i]));
                XmlNode infoNode = xmlNode.Attributes.GetNamedItem("info");
                if (infoNode != null) info = infoNode.Value;
                BattleActionTool.ActOnNode(xmlNode, "animSkip", (node) => { animSkipModel = new EventBlockModel(node); });
                BattleActionTool.ActOnNode(xmlNode, "onConclusion", (node) => { onConclusionModel = new EventBlockModel(node); });
                BattleActionTool.ActOnNode(xmlNode, "onStart", (node) => { onStartModel = new EventBlockModel(node); });
                BattleActionTool.ActOnNode(xmlNode, "baseAOERadius", (node) => { baseAOERadius = float.Parse(node.InnerText); });
                BattleActionTool.ActOnNode(xmlNode, "baseDelay", (node) => { baseDelay = float.Parse(node.InnerText); });
                BattleActionTool.ActOnNode(xmlNode, "baseFollowthroughStanceChangeDelay", (node) => { baseFollowthroughStanceChangeDelay = float.Parse(node.InnerText); });
                BattleActionTool.ActOnNode(xmlNode, "baseMinimumTargetingDistance", (node) => { baseMinimumTargetingDistance = float.Parse(node.InnerText); });
                BattleActionTool.ActOnNode(xmlNode, "baseTargetingRange", (node) => { baseTargetingRange = float.Parse(node.InnerText); });
                BattleActionTool.ActOnNode(xmlNode, "baseSPCost", (node) => { baseSPCost = byte.Parse(node.InnerText); });
                BattleActionTool.ActOnNode(xmlNode, "alternateTargetSideFlags", (node) => { alternateTargetSideFlags = DBTools.ParseTargetSideFlags(node.InnerText); });
                BattleActionTool.ActOnNode(xmlNode, "targetSideFlags", (node) => { targetSideFlags = DBTools.ParseTargetSideFlags(node.InnerText); });
                BattleActionTool.ActOnNode(xmlNode, "alternateTargetType", (node) => { alternateTargetType = DBTools.ParseActionTargetType(node.InnerText); });
                BattleActionTool.ActOnNode(xmlNode, "targetType", (node) => { targetType = DBTools.ParseActionTargetType(node.InnerText); });
                BattleActionTool.ActOnNode(xmlNode, "categoryFlags", (node) => { categoryFlags = DBTools.ParseBattleActionCategoryFlags(node.InnerText); });
                BattleActionTool.ActOnNode(xmlNode, "subactionOrder", (node) => { subactionOrder.AddRange(node.InnerText.Replace(" ", string.Empty).Split(',')); });
            }
            else
            {
                doc.CreateXmlDeclaration("1.0", "Unicode", "yes");
                xmlNode = doc.AppendChild(doc.CreateNode(XmlNodeType.Element, name, doc.NamespaceURI));      
            }
        }

        /// <summary>
        /// Dumps the contents of the model to a C# BattleAction declaration.
        /// </summary>
        public CodeObjectCreateExpression DumpToCSDeclaration ()
        {
            CodeExpression animSkipDeclaration;
            if (animSkipModel != null) animSkipDeclaration = animSkipModel.DumpToCSDeclaration();
            else animSkipDeclaration = new CodePrimitiveExpression(null);
            CodeExpression onConclusionDeclaration;
            if (onConclusionModel != null) onConclusionDeclaration = onConclusionModel.DumpToCSDeclaration();
            else onConclusionDeclaration = new CodePrimitiveExpression(null);
            CodeExpression onStartDeclaration;
            if (onStartModel != null) onStartDeclaration = onStartModel.DumpToCSDeclaration();
            else onStartDeclaration = new CodePrimitiveExpression(null);
            CodeFieldReferenceExpression actionIDDeclaration = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(ActionType)), actionID.ToString());
            CodePrimitiveExpression baseAOERadiusDeclaration = new CodePrimitiveExpression(baseAOERadius);
            CodePrimitiveExpression baseDelayDeclaration = new CodePrimitiveExpression(baseDelay);
            CodePrimitiveExpression baseFollowthroughStanceChangeDelayDeclaration = new CodePrimitiveExpression(baseFollowthroughStanceChangeDelay);
            CodePrimitiveExpression baseMinimumTargetingDistanceDeclaration = new CodePrimitiveExpression(baseMinimumTargetingDistance);
            CodePrimitiveExpression baseTargetingRangeDeclaration = new CodePrimitiveExpression(baseTargetingRange);
            CodePrimitiveExpression baseSPCostDeclaration = new CodePrimitiveExpression(baseSPCost);
            CodeExpression alternateTargetSideFlagsDeclaration = new CodeCastExpression(typeof(TargetSideFlags), new CodePrimitiveExpression((int)alternateTargetSideFlags));
            CodeExpression targetSideFlagsDeclaration = new CodeCastExpression(typeof(TargetSideFlags), new CodePrimitiveExpression((int)targetSideFlags));
            CodeExpression alternateTargetTypeDeclaration = new CodeCastExpression(typeof(ActionTargetType), new CodePrimitiveExpression((byte)alternateTargetType));
            CodeExpression targetTypeDeclaration = new CodeCastExpression(typeof(ActionTargetType), new CodePrimitiveExpression((byte)targetType));
            CodeExpression categoryFlagsDeclaration = new CodeCastExpression(typeof(BattleActionCategoryFlags), new CodePrimitiveExpression((int)categoryFlags));
            return new CodeObjectCreateExpression(typeof(BattleAction), new CodeExpression[] { animSkipDeclaration, onConclusionDeclaration, onStartDeclaration, actionIDDeclaration, baseAOERadiusDeclaration,
                                                  baseDelayDeclaration, baseFollowthroughStanceChangeDelayDeclaration, baseMinimumTargetingDistanceDeclaration, baseTargetingRangeDeclaration, baseSPCostDeclaration,
                                                  alternateTargetSideFlagsDeclaration, targetSideFlagsDeclaration, alternateTargetTypeDeclaration, targetTypeDeclaration, categoryFlagsDeclaration,
                                                  GetSubactionsArray() });
        }

        /// <summary>
        /// Dump the contents of the model to the XmlNode.
        /// This will call DumpToXmlNode on all child models, which
        /// do likewise for their children until the entire
        /// tree has conformed to the model representation.
        /// </summary>
        public XmlNode DumpToXmlNode ()
        {
            if (subactionModels.Count > subactionOrder.Count) throw new Exception(actionID + " has mismatch between subaction order count and no. of subactions. Can't compile or export to XML until corrected.");
            List<XmlNode> validChildren = new List<XmlNode>();
            if (animSkipModel != null)
            {
                validChildren.Add(animSkipModel.DumpToXmlNode());
            }
            if (onConclusionModel != null)
            {
                validChildren.Add(onConclusionModel.DumpToXmlNode());
            }
            if (onStartModel != null)
            {
                validChildren.Add(onStartModel.DumpToXmlNode());
            }
            for (int i = 0; i < subactionModels.Count; i++) validChildren.Add(subactionModels[i].DumpToXmlNode());
            BattleActionTool.HandleChildNode(xmlNode, "info", (node) => { node.Value = info; }, validChildren, XmlNodeType.Attribute);
            BattleActionTool.HandleChildNode(xmlNode, "baseAOERadius", (node) => { node.InnerText = baseAOERadius.ToString(); }, validChildren);
            BattleActionTool.HandleChildNode(xmlNode, "baseDelay", (node) => { node.InnerText = baseDelay.ToString(); }, validChildren);
            BattleActionTool.HandleChildNode(xmlNode, "baseFollowthroughStanceChangeDelay", (node) => { node.InnerText = baseFollowthroughStanceChangeDelay.ToString(); }, validChildren);
            BattleActionTool.HandleChildNode(xmlNode, "baseMinimumTargetingDistance", (node) => { node.InnerText = baseMinimumTargetingDistance.ToString(); }, validChildren);
            BattleActionTool.HandleChildNode(xmlNode, "baseTargetingRange", (node) => { node.InnerText = baseTargetingRange.ToString(); }, validChildren);
            BattleActionTool.HandleChildNode(xmlNode, "baseSPCost", (node) => { node.InnerText = baseSPCost.ToString(); }, validChildren);
            BattleActionTool.HandleChildNode(xmlNode, "alternateTargetSideFlags", (node) => { node.InnerText = alternateTargetSideFlags.ToString(); }, validChildren);
            BattleActionTool.HandleChildNode(xmlNode, "targetSideFlags", (node) => { node.InnerText = targetSideFlags.ToString(); }, validChildren);
            BattleActionTool.HandleChildNode(xmlNode, "alternateTargetType", (node) => { node.InnerText = alternateTargetType.ToString(); }, validChildren);
            BattleActionTool.HandleChildNode(xmlNode, "targetType", (node) => { node.InnerText = targetType.ToString(); }, validChildren);
            BattleActionTool.HandleChildNode(xmlNode, "categoryFlags", (node) => { node.InnerText = categoryFlags.ToString(); }, validChildren);
            BattleActionTool.HandleChildNode(xmlNode, "subactionOrder", (node) => { node.InnerText = GetSubactionOrderString(); }, validChildren);
            BattleActionTool.CleanNode(xmlNode, validChildren);
            return xmlNode;
        }

        /// <summary>
        /// Spit out the final XML for this BattleActionModel.
        /// </summary>
        public void SaveToFile ()
        {
            DumpToXmlNode(); // Conform the XML representation to the current state of the model.
            FileStream fs;
            if (File.Exists(filePath)) fs = File.OpenWrite(filePath);
            else fs = File.Create(filePath);
            fs.SetLength(0); // empty this mofo
            doc.Save(fs);
            fs.Dispose();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Turns subaction order list into a single CSV string.
        /// </summary>
        private string GetSubactionOrderString ()
        {
            string r = string.Empty;
            for (int i = 0; i < subactionOrder.Count; i++)
            {
                r += subactionOrder[i];
                if (i + 1 < subactionOrder.Count) r += ",";
            }
            return r;
        }

        /// <summary>
        /// Build the subaction array.
        /// </summary>
        private CodeArrayCreateExpression GetSubactionsArray ()
        {
            if (subactionModels.Count > subactionOrder.Count) throw new Exception(actionID + " has mismatch between subaction order count and no. of subactions. Can't compile or export to XML until corrected.");
            CodeObjectCreateExpression[] subactionDeclarations = new CodeObjectCreateExpression[subactionOrder.Count];
            for (int i = 0; i < subactionOrder.Count; i++) subactionDeclarations[i] = FindSubaction(subactionOrder[i]).DumpToCSDeclaration();
            return new CodeArrayCreateExpression(typeof(BattleAction.Subaction), subactionDeclarations);
        }

        /// <summary>
        /// Get the subaction model of the given name.
        /// </summary>
        public SubactionModel FindSubaction(string subactionName)
        {
            for (int s = 0; s < subactionModels.Count; s++)
            {
                if (subactionModels[s].subactionName == subactionName) return subactionModels[s];
            }
            throw new Exception("Couldn't find subaction by name of " + subactionName);
        }

        /// <summary>
        /// Get the index of the subaction by the given name.
        /// </summary>
        public int GetIndexForSubactionOfName (string subactionName)
        {
            if (subactionName == string.Empty) return -1; 
            for (int s = 0; s < subactionModels.Count; s++)
            {
                if (subactionModels[s].subactionName == subactionName) return s;
            }
            throw new Exception("Couldn't find subaction by name of " + subactionName);
        }
    }
}
#endif