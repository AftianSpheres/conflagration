#if UNITY_EDITOR
using System.CodeDom;
using System.Collections.Generic;
using System.Xml;
using CnfBattleSys;

namespace BattleActionTool
{
    /// <summary>
    /// Models an EffectPackage.
    /// </summary>
    public class EffectPackageModel
    {
        public readonly SubactionModel subactionModel;
        public readonly XmlNode xmlNode;
        public BattleAction.Subaction.EffectPackage effectPackage
        {
            get
            {
                return new BattleAction.Subaction.EffectPackage(eventBlockModel.eventBlock, subactionEffectType, hitStat, evadeStat, applyEvenIfSubactionMisses,
                baseSuccessRate, length_Float, strength_Float, length_Byte, strength_Int, tieSuccessToEffectIndex, baseAIScoreValue);
            }
        }
        public EventBlockModel eventBlockModel;
        public EffectPackageType subactionEffectType;
        public LogicalStatType hitStat;
        public LogicalStatType evadeStat;
        public bool applyEvenIfSubactionMisses;
        public float baseAIScoreValue;
        public float baseSuccessRate;
        public float length_Float;
        public float strength_Float;
        public byte length_Byte;
        public int strength_Int;
        public sbyte tieSuccessToEffectIndex;
        public string info;
        public const string name = "EffectPackage";
        private readonly XmlDocument doc;

        /// <summary>
        /// Creates new AnimEventModel as child of subactionModel.
        /// </summary>
        public EffectPackageModel(SubactionModel _subactionModel)
        {
            subactionModel = _subactionModel;
            doc = subactionModel.xmlNode.OwnerDocument;
            xmlNode = subactionModel.xmlNode.AppendChild(doc.CreateNode(XmlNodeType.Element, name, doc.NamespaceURI));
        }

        /// <summary>
        /// Populates the EffectPackagetModel from an xml node.
        /// </summary>
        public EffectPackageModel(SubactionModel _subactionModel, XmlNode _node)
        {
            subactionModel = _subactionModel;
            xmlNode = _node;
            doc = _node.OwnerDocument;
            XmlNode infoNode = _node.Attributes.GetNamedItem("info");
            if (infoNode != null) info = infoNode.Value;
            XmlNode eventBlockNode = _node.SelectSingleNode("eventBlock");
            if (eventBlockNode != null) eventBlockModel = new EventBlockModel(eventBlockNode);
            BattleActionTool.ActOnNode(_node, "subactionEffectType", (workingNode) => { subactionEffectType = DBTools.ParseSubactionFXType(workingNode.InnerText); });
            BattleActionTool.ActOnNode(_node, "hitStat", (workingNode) => { hitStat = DBTools.ParseLogicalStatType(workingNode.InnerText); });
            BattleActionTool.ActOnNode(_node, "evadeStat", (workingNode) => { evadeStat = DBTools.ParseLogicalStatType(workingNode.InnerText); });
            BattleActionTool.ActOnNode(_node, "applyEvenIfSubactionMisses", (workingNode) => { applyEvenIfSubactionMisses = bool.Parse(workingNode.InnerText); });
            BattleActionTool.ActOnNode(_node, "baseAIScoreValue", (workingNode) => { baseAIScoreValue = float.Parse(workingNode.InnerText); });
            BattleActionTool.ActOnNode(_node, "baseSuccessRate", (workingNode) => { baseSuccessRate = float.Parse(workingNode.InnerText); });
            BattleActionTool.ActOnNode(_node, "length_Float", (workingNode) => { length_Float = float.Parse(workingNode.InnerText); });
            BattleActionTool.ActOnNode(_node, "strength_Float", (workingNode) => { strength_Float = float.Parse(workingNode.InnerText); });
            BattleActionTool.ActOnNode(_node, "length_Byte", (workingNode) => { length_Byte = byte.Parse(workingNode.InnerText); });
            BattleActionTool.ActOnNode(_node, "strength_Int", (workingNode) => { strength_Int = int.Parse(workingNode.InnerText); });
            BattleActionTool.ActOnNode(_node, "tieSuccessToEffectIndex", (workingNode) => { tieSuccessToEffectIndex = sbyte.Parse(workingNode.InnerText); });
        }

        /// <summary>
        /// Dumps the contents of the model to a C# EffectPackage declaration.
        /// </summary>
        public CodeObjectCreateExpression DumpToCSDeclaration()
        {
            CodeExpression eventBlockDeclaration;
            if (eventBlockModel != null) eventBlockDeclaration = eventBlockModel.DumpToCSDeclaration();
            else eventBlockDeclaration = new CodePrimitiveExpression(null);
            CodeFieldReferenceExpression subactionEffectTypeDeclaration = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(EffectPackageType)), subactionEffectType.ToString());
            CodeFieldReferenceExpression hitStatDeclaration = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(LogicalStatType)), hitStat.ToString());
            CodeFieldReferenceExpression evadeStatDeclaration = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(LogicalStatType)), evadeStat.ToString());
            CodePrimitiveExpression applyEvenIfSubactionMissesDeclaration = new CodePrimitiveExpression(applyEvenIfSubactionMisses);
            CodePrimitiveExpression baseAIScoreValueDeclaration = new CodePrimitiveExpression(baseAIScoreValue);
            CodePrimitiveExpression baseSuccessRateDeclaration = new CodePrimitiveExpression(baseSuccessRate);
            CodePrimitiveExpression length_FloatDeclaration = new CodePrimitiveExpression(length_Float);
            CodePrimitiveExpression strength_FloatDeclaration = new CodePrimitiveExpression(strength_Float);
            CodePrimitiveExpression length_ByteDeclaration = new CodePrimitiveExpression(length_Byte);
            CodePrimitiveExpression strength_IntDeclaration = new CodePrimitiveExpression(strength_Int);
            CodePrimitiveExpression tieSuccessToEffectIndexDeclaration = new CodePrimitiveExpression(tieSuccessToEffectIndex);
            return new CodeObjectCreateExpression(typeof(BattleAction.Subaction.EffectPackage), new CodeExpression[] { eventBlockDeclaration, subactionEffectTypeDeclaration, hitStatDeclaration, evadeStatDeclaration,
                                                  applyEvenIfSubactionMissesDeclaration, baseSuccessRateDeclaration, length_FloatDeclaration, strength_FloatDeclaration, length_ByteDeclaration, strength_IntDeclaration,
                                                  tieSuccessToEffectIndexDeclaration, baseAIScoreValueDeclaration });
        }

        /// <summary>
        /// Dump the contents of the model to the XmlNode.
        /// </summary>
        public XmlNode DumpToXmlNode()
        {    
            List<XmlNode> validChildren = new List<XmlNode>();
            if (eventBlockModel != null)
            {
                validChildren.Add(eventBlockModel.DumpToXmlNode());
            }
            BattleActionTool.HandleChildNode(xmlNode, "info", (node) => { node.Value = info; }, validChildren, XmlNodeType.Attribute);
            BattleActionTool.HandleChildNode(xmlNode, "subactionEffectType", (node) => { node.InnerText = subactionEffectType.ToString(); }, validChildren);
            BattleActionTool.HandleChildNode(xmlNode, "hitStat", (node) => { node.InnerText = hitStat.ToString(); }, validChildren);
            BattleActionTool.HandleChildNode(xmlNode, "evadeStat", (node) => { node.InnerText = evadeStat.ToString(); }, validChildren);
            BattleActionTool.HandleChildNode(xmlNode, "applyEvenIfSubactionMisses", (node) => { node.InnerText = applyEvenIfSubactionMisses.ToString(); }, validChildren);
            BattleActionTool.HandleChildNode(xmlNode, "baseAIScoreValue", (node) => { node.InnerText = baseAIScoreValue.ToString(); }, validChildren);
            BattleActionTool.HandleChildNode(xmlNode, "baseSuccessRate", (node) => { node.InnerText = baseSuccessRate.ToString(); }, validChildren);
            BattleActionTool.HandleChildNode(xmlNode, "length_Float", (node) => { node.InnerText = length_Float.ToString(); }, validChildren);
            BattleActionTool.HandleChildNode(xmlNode, "strength_Float", (node) => { node.InnerText = strength_Float.ToString(); }, validChildren);
            BattleActionTool.HandleChildNode(xmlNode, "length_Byte", (node) => { node.InnerText = length_Byte.ToString(); }, validChildren);
            BattleActionTool.HandleChildNode(xmlNode, "strength_Int", (node) => { node.InnerText = strength_Int.ToString(); }, validChildren);
            BattleActionTool.HandleChildNode(xmlNode, "tieSuccessToEffectIndex", (node) => { node.InnerText = tieSuccessToEffectIndex.ToString(); }, validChildren);
            BattleActionTool.CleanNode(xmlNode, validChildren);
            return xmlNode;
        }
    }
}
#endif