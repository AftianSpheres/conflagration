#if UNITY_EDITOR
using System.CodeDom;
using System.Collections.Generic;
using System.Xml;
using CnfBattleSys;

namespace BattleActionTool
{
    /// <summary>
    /// Models a Subaction
    /// </summary>
    public class SubactionModel
    {
        public readonly BattleActionModel battleActionModel;
        public readonly XmlNode xmlNode;
        public BattleAction.Subaction subaction { get {
                return new BattleAction.Subaction(eventBlockModel.eventBlock, baseDamage, baseAccuracy, useAlternateTargetSet, atkStat, defStat,
                hitStat, evadeStat, damageDeterminantName, predicateName, successDeterminantName, categoryFlags, DumpEffectPackages(), damageTypes);
            } }
        public List<EffectPackageModel> effectPackageModels;
        public EventBlockModel eventBlockModel;
        public string subactionName;
        public int baseDamage;
        public float baseAccuracy;
        public bool useAlternateTargetSet;
        public LogicalStatType atkStat;
        public LogicalStatType defStat;
        public LogicalStatType hitStat;
        public LogicalStatType evadeStat;
        public DamageTypeFlags damageTypes;
        public BattleActionCategoryFlags categoryFlags;
        public string damageDeterminantName;
        public string predicateName;
        public string successDeterminantName;
        public string info;
        public const string name = "Subaction";
        private readonly XmlDocument doc;
        private CodeExpression predicateNameDeclaration;

        /// <summary>
        /// Creates new SubactionModel as child of battleActionModel
        /// </summary>
        public SubactionModel(BattleActionModel _battleActionModel)
        {
            battleActionModel = _battleActionModel;
            doc = battleActionModel.xmlNode.OwnerDocument;
            xmlNode = battleActionModel.xmlNode.AppendChild(doc.CreateNode(XmlNodeType.Element, name, doc.NamespaceURI));
            effectPackageModels = new List<EffectPackageModel>();
        }

        /// <summary>
        /// Populates the SubactionModel from an xml node.
        /// </summary>
        public SubactionModel(BattleActionModel _battleActionModel, XmlNode _node)
        {
            battleActionModel = _battleActionModel;
            xmlNode = _node;
            doc = _node.OwnerDocument;
            XmlNode infoNode = _node.Attributes.GetNamedItem("info");
            if (infoNode != null) info = infoNode.Value;
            XmlNode eventBlockNode = _node.SelectSingleNode("eventBlock");
            if (eventBlockNode != null) eventBlockModel = new EventBlockModel(eventBlockNode);
            XmlNodeList effectPackageModelNodes = _node.SelectNodes(EffectPackageModel.name);
            effectPackageModels = new List<EffectPackageModel>();
            for (int i = 0; i < effectPackageModelNodes.Count; i++) effectPackageModels.Add(new EffectPackageModel(this, effectPackageModelNodes[i]));
            BattleActionTool.ActOnNode(_node, "subactionName", (workingNode) => { subactionName = workingNode.InnerText; });
            BattleActionTool.ActOnNode(_node, "baseDamage", (workingNode) => { baseDamage = int.Parse(workingNode.InnerText); });
            BattleActionTool.ActOnNode(_node, "baseAccuracy", (workingNode) => { baseAccuracy = float.Parse(workingNode.InnerText); });
            BattleActionTool.ActOnNode(_node, "useAlternateTargetSet", (workingNode) => { useAlternateTargetSet = bool.Parse(workingNode.InnerText); });
            BattleActionTool.ActOnNode(_node, "atkStat", (workingNode) => { atkStat = DBTools.ParseLogicalStatType(workingNode.InnerText); });
            BattleActionTool.ActOnNode(_node, "defStat", (workingNode) => { defStat = DBTools.ParseLogicalStatType(workingNode.InnerText); });
            BattleActionTool.ActOnNode(_node, "hitStat", (workingNode) => { hitStat = DBTools.ParseLogicalStatType(workingNode.InnerText); });
            BattleActionTool.ActOnNode(_node, "evadeStat", (workingNode) => { evadeStat = DBTools.ParseLogicalStatType(workingNode.InnerText); });
            BattleActionTool.ActOnNode(_node, "damageTypes", (workingNode) => { damageTypes = DBTools.ParseDamageTypeFlags(workingNode.InnerText); });
            BattleActionTool.ActOnNode(_node, "categoryFlags", (workingNode) => { categoryFlags = DBTools.ParseBattleActionCategoryFlags(workingNode.InnerText); });
            BattleActionTool.ActOnNode(_node, "damageDeterminantName", (workingNode) => { damageDeterminantName = workingNode.InnerText; });
            BattleActionTool.ActOnNode(_node, "predicateName", (workingNode) => { predicateName = workingNode.InnerText; });
            BattleActionTool.ActOnNode(_node, "successDeterminantName", (workingNode) => { successDeterminantName = workingNode.InnerText; });
        }

        /// <summary>
        /// Dumps the contents of the model to a C# Subaction declaration.
        /// </summary>
        public CodeObjectCreateExpression DumpToCSDeclaration()
        {
            CodeExpression eventBlockDeclaration;
            if (eventBlockModel != null) eventBlockDeclaration = eventBlockModel.DumpToCSDeclaration();
            else eventBlockDeclaration = new CodePrimitiveExpression(null);
            CodePrimitiveExpression baseDamageDeclaration = new CodePrimitiveExpression(baseDamage);
            CodePrimitiveExpression baseAccuracyDeclaration = new CodePrimitiveExpression(baseAccuracy);
            CodePrimitiveExpression useAlternateTargetSetDeclaration = new CodePrimitiveExpression(useAlternateTargetSet);
            CodeFieldReferenceExpression atkStatDeclaration = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(LogicalStatType)), atkStat.ToString());
            CodeFieldReferenceExpression defStatDeclaration = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(LogicalStatType)), defStat.ToString());
            CodeFieldReferenceExpression hitStatDeclaration = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(LogicalStatType)), hitStat.ToString());
            CodeFieldReferenceExpression evadeStatDeclaration = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(LogicalStatType)), evadeStat.ToString());
            CodeFieldReferenceExpression damageTypesDeclaration = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(DamageTypeFlags)), damageTypes.ToString().Replace(", ", " | "));
            CodeFieldReferenceExpression categoryFlagsDeclaration = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(BattleActionCategoryFlags)), categoryFlags.ToString().Replace(", ", " | "));
            CodePrimitiveExpression damageDeterminantNameDeclaration = new CodePrimitiveExpression(damageDeterminantName);
            CodePrimitiveExpression predicateNameDeclaration = new CodePrimitiveExpression(predicateName);
            CodePrimitiveExpression successDeterminantNameDeclaration = new CodePrimitiveExpression(successDeterminantName);
            return new CodeObjectCreateExpression(typeof(BattleAction.Subaction), new CodeExpression[] { eventBlockDeclaration, baseDamageDeclaration, baseAccuracyDeclaration, useAlternateTargetSetDeclaration,
                                                  atkStatDeclaration, defStatDeclaration, hitStatDeclaration, evadeStatDeclaration, damageDeterminantNameDeclaration, predicateNameDeclaration,
                                                  successDeterminantNameDeclaration, categoryFlagsDeclaration, DumpEffectPackageArrayCreateExpression(), damageTypesDeclaration });
        }

        /// <summary>
        /// Dump the contents of the model to the XmlNode.
        /// </summary>
        public XmlNode DumpToXmlNode()
        {
            List<XmlNode> validChildren = new List<XmlNode>();
            if (eventBlockModel != null) validChildren.Add(eventBlockModel.DumpToXmlNode());
            for (int i = 0; i < effectPackageModels.Count; i++) validChildren.Add(effectPackageModels[i].DumpToXmlNode());
            BattleActionTool.HandleChildNode(xmlNode, "info", (node) => { node.Value = info; }, validChildren, XmlNodeType.Attribute);
            BattleActionTool.HandleChildNode(xmlNode, "subactionName", (node) => { node.InnerText = subactionName; }, validChildren);
            BattleActionTool.HandleChildNode(xmlNode, "baseDamage", (node) => { node.InnerText = baseDamage.ToString(); }, validChildren);
            BattleActionTool.HandleChildNode(xmlNode, "baseAccuracy", (node) => { node.InnerText = baseAccuracy.ToString(); }, validChildren);
            BattleActionTool.HandleChildNode(xmlNode, "useAlternateTargetSet", (node) => { node.InnerText = useAlternateTargetSet.ToString(); }, validChildren);
            BattleActionTool.HandleChildNode(xmlNode, "atkStat", (node) => { node.InnerText = atkStat.ToString(); }, validChildren);
            BattleActionTool.HandleChildNode(xmlNode, "defStat", (node) => { node.InnerText = defStat.ToString(); }, validChildren);
            BattleActionTool.HandleChildNode(xmlNode, "hitStat", (node) => { node.InnerText = hitStat.ToString(); }, validChildren);
            BattleActionTool.HandleChildNode(xmlNode, "evadeStat", (node) => { node.InnerText = evadeStat.ToString(); }, validChildren);
            BattleActionTool.HandleChildNode(xmlNode, "damageTypes", (node) => { node.InnerText = damageTypes.ToString(); }, validChildren);
            BattleActionTool.HandleChildNode(xmlNode, "categoryFlags", (node) => { node.InnerText = categoryFlags.ToString(); }, validChildren);
            BattleActionTool.HandleChildNode(xmlNode, "damageDeterminantName", (node) => { node.InnerText = damageDeterminantName; }, validChildren);
            BattleActionTool.HandleChildNode(xmlNode, "predicateName", (node) => { node.InnerText = predicateName; }, validChildren);
            BattleActionTool.HandleChildNode(xmlNode, "successDeterminantName", (node) => { node.InnerText = successDeterminantName; }, validChildren);
            BattleActionTool.CleanNode(xmlNode, validChildren);
            return xmlNode;
        }

        /// <summary>
        /// Dump EffectPackage refs from all models.
        /// </summary>
        private BattleAction.Subaction.EffectPackage[] DumpEffectPackages ()
        {
            List<BattleAction.Subaction.EffectPackage> effectPackagesList = new List<BattleAction.Subaction.EffectPackage>(effectPackageModels.Count);
            for (int i = 0; i < effectPackageModels.Count; i++) effectPackagesList[i] = effectPackageModels[i].effectPackage;
            return effectPackagesList.ToArray();
        }

        /// <summary>
        /// Get the array of declarations for the effect packages what're attached to this subaction
        /// </summary>
        private CodeArrayCreateExpression DumpEffectPackageArrayCreateExpression ()
        {
            CodeExpression[] effectPackageArrayContents = new CodeExpression[effectPackageModels.Count];
            for (int i = 0; i < effectPackageArrayContents.Length; i++) effectPackageArrayContents[i] = effectPackageModels[i].DumpToCSDeclaration();
            return new CodeArrayCreateExpression(typeof(BattleAction.Subaction.EffectPackage), effectPackageArrayContents);
        }
    }
}
#endif