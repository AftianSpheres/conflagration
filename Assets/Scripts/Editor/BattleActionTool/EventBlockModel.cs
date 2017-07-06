#if UNITY_EDITOR
using System.CodeDom;
using System.Collections.Generic;
using System.Xml;
using CnfBattleSys;

namespace BattleActionTool
{
    /// <summary>
    /// Models an EventBlock.
    /// </summary>
    public class EventBlockModel
    {
        /// <summary>
        /// Models a single AnimEvent.
        /// </summary>
        public class AnimEventModel
        {
            public readonly EventBlockModel eventBlockModel;
            public readonly XmlNode xmlNode;
            public AnimEvent animEvent { get { return new AnimEvent(animEventType, fallbackType, targetType, flags, priority); } }
            public AnimEventType animEventType;
            public AnimEventType fallbackType;
            public BattleEventTargetType targetType;
            public AnimEvent.Flags flags;
            public int priority;
            public string info;
            const string name = "AnimEvent";
            private readonly XmlDocument doc;

            /// <summary>
            /// Creates new AnimEventModel as child of eventBlockModel.
            /// </summary>
            public AnimEventModel(EventBlockModel _eventBlockModel)
            {
                eventBlockModel = _eventBlockModel;
                doc = eventBlockModel.xmlNode.OwnerDocument;
                xmlNode = eventBlockModel.xmlNode.AppendChild(doc.CreateNode(XmlNodeType.Element, name, doc.NamespaceURI));
            }

            /// <summary>
            /// Populates the AnimEventModel from an xml node.
            /// </summary>
            public AnimEventModel(EventBlockModel _eventBlockModel, XmlNode _node)
            {
                eventBlockModel = _eventBlockModel;
                doc = eventBlockModel.xmlNode.OwnerDocument;
                xmlNode = _node;
                XmlNode infoNode = _node.Attributes.GetNamedItem("info");
                if (infoNode != null) info = infoNode.Value;
                BattleActionTool.ActOnNode(_node, "animEventType", (workingNode) => { animEventType = DBTools.ParseAnimEventType(workingNode.InnerText); });
                BattleActionTool.ActOnNode(_node, "fallbackType", (workingNode) => { fallbackType = DBTools.ParseAnimEventType(workingNode.InnerText); });
                BattleActionTool.ActOnNode(_node, "targetType", (workingNode) => { targetType = DBTools.ParseBattleEventTargetType(workingNode.InnerText); });
                BattleActionTool.ActOnNode(_node, "flags", (workingNode) => { flags = DBTools.ParseAnimEventFlags(workingNode.InnerText); });
                BattleActionTool.ActOnNode(_node, "priority", (workingNode) => { priority = int.Parse(workingNode.InnerText); });
            }

            /// <summary>
            /// Dumps the contents of the model to a C# AnimEvent declaration.
            /// </summary>
            public CodeObjectCreateExpression DumpToCSDeclaration()
            {
                CodeFieldReferenceExpression animEventTypeDeclaration = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(AnimEventType)), animEventType.ToString());
                CodeFieldReferenceExpression fallbackTypeDeclaration = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(AnimEventType)), fallbackType.ToString());
                CodeFieldReferenceExpression targetTypeDeclaration = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(BattleEventTargetType)), targetType.ToString().Replace(", ", " | "));
                CodeFieldReferenceExpression flagsDeclaration = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(AnimEvent.Flags)), flags.ToString().Replace(", ", " | "));
                CodePrimitiveExpression priorityDeclaration = new CodePrimitiveExpression(priority);
                return new CodeObjectCreateExpression(typeof(AnimEvent), new CodeExpression[] { animEventTypeDeclaration, fallbackTypeDeclaration, targetTypeDeclaration, flagsDeclaration, priorityDeclaration });
            }

            /// <summary>
            /// Dump the contents of the model to the XmlNode.
            /// </summary>
            public XmlNode DumpToXmlNode()
            {
                List<XmlNode> validChildren = new List<XmlNode>();
                BattleActionTool.HandleChildNode(xmlNode, "info", (node) => { node.Value = info; }, validChildren, XmlNodeType.Attribute);
                BattleActionTool.HandleChildNode(xmlNode, "animEventType", (node) => { node.InnerText = animEventType.ToString(); }, validChildren);
                BattleActionTool.HandleChildNode(xmlNode, "fallbackType", (node) => { node.InnerText = fallbackType.ToString(); }, validChildren);
                BattleActionTool.HandleChildNode(xmlNode, "targetType", (node) => { node.InnerText = targetType.ToString(); }, validChildren);
                BattleActionTool.HandleChildNode(xmlNode, "flags", (node) => { node.InnerText = flags.ToString(); }, validChildren);
                BattleActionTool.HandleChildNode(xmlNode, "priority", (node) => { node.InnerText = priority.ToString(); }, validChildren);
                BattleActionTool.CleanNode(xmlNode, validChildren);
                return xmlNode;
            }
        }

        /// <summary>
        /// Models a single AudioEvent.
        /// </summary>
        public class AudioEventModel
        {
            public readonly EventBlockModel eventBlockModel;
            public readonly XmlNode xmlNode;
            public AudioEvent audioEvent { get { return new AudioEvent(audioEventType, fallbackType, clipType, targetType, flags, priority); } }
            public AudioEventType audioEventType;
            public AudioEventType fallbackType;
            public AudioSourceType clipType;
            public BattleEventTargetType targetType;
            public AudioEvent.Flags flags;
            public int priority;
            public string info;
            const string name = "AudioEvent";
            private readonly XmlDocument doc;


            /// <summary>
            /// Creates new AudioEventModel as child of eventBlockModel.
            /// </summary>
            public AudioEventModel(EventBlockModel _eventBlockModel)
            {
                eventBlockModel = _eventBlockModel;
                doc = eventBlockModel.xmlNode.OwnerDocument;
                xmlNode = eventBlockModel.xmlNode.AppendChild(doc.CreateNode(XmlNodeType.Element, name, doc.NamespaceURI));
            }

            /// <summary>
            /// Populates the AudioEventModel from an xml node.
            /// </summary>
            public AudioEventModel(EventBlockModel _eventBlockModel, XmlNode _node)
            {
                eventBlockModel = _eventBlockModel;
                doc = eventBlockModel.xmlNode.OwnerDocument;
                xmlNode = _node;
                XmlNode infoNode = _node.Attributes.GetNamedItem("info");
                if (infoNode != null) info = infoNode.Value;
                BattleActionTool.ActOnNode(_node, "audioEventType", (workingNode) => { audioEventType = DBTools.ParseAudioEventType(workingNode.InnerText); });
                BattleActionTool.ActOnNode(_node, "fallbackType", (workingNode) => { fallbackType = DBTools.ParseAudioEventType(workingNode.InnerText); });
                BattleActionTool.ActOnNode(_node, "clipType", (workingNode) => { clipType = DBTools.ParseAudioSourceType(workingNode.InnerText); });
                BattleActionTool.ActOnNode(_node, "targetType", (workingNode) => { targetType = DBTools.ParseBattleEventTargetType(workingNode.InnerText); });
                BattleActionTool.ActOnNode(_node, "flags", (workingNode) => { flags = DBTools.ParseAudioEventFlags(workingNode.InnerText); });
                BattleActionTool.ActOnNode(_node, "priority", (workingNode) => { priority = int.Parse(workingNode.InnerText); });
            }

            /// <summary>
            /// Dumps the contents of the model to a C# AudioEvent declaration.
            /// </summary>
            public CodeObjectCreateExpression DumpToCSDeclaration()
            {
                CodeFieldReferenceExpression audioEventTypeDeclaration = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(AudioEventType)), audioEventType.ToString());
                CodeFieldReferenceExpression fallbackTypeDeclaration = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(AudioEventType)), fallbackType.ToString());
                CodeFieldReferenceExpression clipTypeDeclaration = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(AudioSourceType)), clipType.ToString());
                CodeFieldReferenceExpression targetTypeDeclaration = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(BattleEventTargetType)), targetType.ToString().Replace(", ", " | "));
                CodeFieldReferenceExpression flagsDeclaration = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(AudioEvent.Flags)), flags.ToString().Replace(", ", " | "));
                CodePrimitiveExpression priorityDeclaration = new CodePrimitiveExpression(priority);
                return new CodeObjectCreateExpression(typeof(AnimEvent), new CodeExpression[] { audioEventTypeDeclaration, fallbackTypeDeclaration, clipTypeDeclaration, targetTypeDeclaration, flagsDeclaration, priorityDeclaration });
            }

            /// <summary>
            /// Dump the contents of the model to the XmlNode.
            /// </summary>
            public XmlNode DumpToXmlNode()
            {
                List<XmlNode> validChildren = new List<XmlNode>();
                BattleActionTool.HandleChildNode(xmlNode, "info", (node) => { node.Value = info; }, validChildren, XmlNodeType.Attribute);
                BattleActionTool.HandleChildNode(xmlNode, "audioEventType", (node) => { node.InnerText = audioEventType.ToString(); }, validChildren);
                BattleActionTool.HandleChildNode(xmlNode, "fallbackType", (node) => { node.InnerText = fallbackType.ToString(); }, validChildren);
                BattleActionTool.HandleChildNode(xmlNode, "clipType", (node) => { node.InnerText = clipType.ToString(); }, validChildren);
                BattleActionTool.HandleChildNode(xmlNode, "targetType", (node) => { node.InnerText = targetType.ToString(); }, validChildren);
                BattleActionTool.HandleChildNode(xmlNode, "flags", (node) => { node.InnerText = flags.ToString(); }, validChildren);
                BattleActionTool.HandleChildNode(xmlNode, "priority", (node) => { node.InnerText = priority.ToString(); }, validChildren);
                BattleActionTool.CleanNode(xmlNode, validChildren);
                return xmlNode;
            }
        }

        /// <summary>
        /// Models a single FXEvent.
        /// </summary>
        public class FXEventModel
        {
            public readonly EventBlockModel eventBlockModel;
            public readonly XmlNode xmlNode;
            public FXEvent fxEvent { get { return new FXEvent(fxEventType, targetType, flags, priority); } }
            public FXEventType fxEventType;
            public BattleEventTargetType targetType;
            public FXEvent.Flags flags;
            public int priority;
            public string info;
            const string name = "FXEvent";
            private readonly XmlDocument doc;


            /// <summary>
            /// Creates new FXEventModel as child of eventBlockModel.
            /// </summary>
            public FXEventModel(EventBlockModel _eventBlockModel)
            {
                eventBlockModel = _eventBlockModel;
                doc = eventBlockModel.xmlNode.OwnerDocument;
                xmlNode = eventBlockModel.xmlNode.AppendChild(doc.CreateNode(XmlNodeType.Element, name, doc.NamespaceURI));
            }

            /// <summary>
            /// Populates the FXEventModel from an xml node.
            /// </summary>
            public FXEventModel(EventBlockModel _eventBlockModel, XmlNode _node)
            {
                eventBlockModel = _eventBlockModel;
                doc = eventBlockModel.xmlNode.OwnerDocument;
                xmlNode = _node;
                XmlNode infoNode = _node.Attributes.GetNamedItem("info");
                if (infoNode != null) info = infoNode.Value;
                BattleActionTool.ActOnNode(_node, "fxEventType", (workingNode) => { fxEventType = DBTools.ParseFXEventType(workingNode.InnerText); });
                BattleActionTool.ActOnNode(_node, "targetType", (workingNode) => { targetType = DBTools.ParseBattleEventTargetType(workingNode.InnerText); });
                BattleActionTool.ActOnNode(_node, "flags", (workingNode) => { flags = DBTools.ParseFXEventFlags(workingNode.InnerText); });
                BattleActionTool.ActOnNode(_node, "priority", (workingNode) => { priority = int.Parse(workingNode.InnerText); });
            }

            /// <summary>
            /// Dumps the contents of the model to a C# FXEvent declaration.
            /// </summary>
            public CodeObjectCreateExpression DumpToCSDeclaration()
            {
                CodeFieldReferenceExpression fxEventTypeDeclaration = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(FXEventType)), fxEventType.ToString());
                CodeFieldReferenceExpression targetTypeDeclaration = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(BattleEventTargetType)), targetType.ToString().Replace(", ", " | "));
                CodeFieldReferenceExpression flagsDeclaration = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(typeof(FXEvent.Flags)), flags.ToString().Replace(", ", " | "));
                CodePrimitiveExpression priorityDeclaration = new CodePrimitiveExpression(priority);
                return new CodeObjectCreateExpression(typeof(FXEvent), new CodeExpression[] { fxEventTypeDeclaration, targetTypeDeclaration, flagsDeclaration, priorityDeclaration });
            }

            /// <summary>
            /// Dump the contents of the model to the XmlNode.
            /// </summary>
            public XmlNode DumpToXmlNode()
            {
                List<XmlNode> validChildren = new List<XmlNode>();
                BattleActionTool.HandleChildNode(xmlNode, "info", (node) => { node.Value = info; }, validChildren, XmlNodeType.Attribute);
                BattleActionTool.HandleChildNode(xmlNode, "fxEventType", (node) => { node.InnerText = fxEventType.ToString(); }, validChildren);
                BattleActionTool.HandleChildNode(xmlNode, "targetType", (node) => { node.InnerText = targetType.ToString(); }, validChildren);
                BattleActionTool.HandleChildNode(xmlNode, "flags", (node) => { node.InnerText = flags.ToString(); }, validChildren);
                BattleActionTool.HandleChildNode(xmlNode, "priority", (node) => { node.InnerText = priority.ToString(); }, validChildren);
                BattleActionTool.CleanNode(xmlNode, validChildren);
                return xmlNode;
            }
        }

        public readonly EffectPackageModel parentEffectPackageModel;
        public readonly XmlNode xmlNode;
        public EventBlock eventBlock { get { return new EventBlock(DumpAnimEvents(), DumpAudioEvents(), DumpFXEvents()); } }
        public string name;
        public List<AnimEventModel> animEventModels = new List<AnimEventModel>(32);
        public List<AudioEventModel> audioEventModels = new List<AudioEventModel>(32);
        public List<FXEventModel> fxEventModels = new List<FXEventModel>(32);
        private XmlDocument doc;

        /// <summary>
        /// Creates an empty EventBlockModel.
        /// </summary>
        public EventBlockModel(XmlNode _parent, string _name)
        {
            name = _name;
            doc = _parent.OwnerDocument;
            xmlNode = _parent.AppendChild(doc.CreateNode(XmlNodeType.Element, name, doc.NamespaceURI));
        }

        /// <summary>
        /// Populates an EventBlockModel from an xml node.
        /// </summary>
        public EventBlockModel(XmlNode _node)
        {
            xmlNode = _node;
            doc = _node.OwnerDocument;
            name = _node.Name;
            XmlNodeList nodeList = _node.SelectNodes("animEvent");
            for (int i = 0; i < nodeList.Count; i++) animEventModels[i] = new AnimEventModel(this, nodeList[i]);
            nodeList = _node.SelectNodes("audioEvent");
            for (int i = 0; i < nodeList.Count; i++) audioEventModels[i] = new AudioEventModel(this, nodeList[i]);
            nodeList = _node.SelectNodes("fxEvent");
            for (int i = 0; i < nodeList.Count; i++) fxEventModels[i] = new FXEventModel(this, nodeList[i]);
        }

        /// <summary>
        /// Dumps the contents of the model to a C# EventBlock declaration.
        /// </summary>
        public CodeObjectCreateExpression DumpToCSDeclaration()
        {
            List<CodeObjectCreateExpression> objectCreateExpressions = new List<CodeObjectCreateExpression>(animEventModels.Count);
            for (int i = 0; i < animEventModels.Count; i++) objectCreateExpressions[i] = animEventModels[i].DumpToCSDeclaration();
            CodeArrayCreateExpression animEventsArrayDeclaration = new CodeArrayCreateExpression(typeof(AnimEvent), objectCreateExpressions.ToArray());
            objectCreateExpressions.Clear();
            for (int i = 0; i < audioEventModels.Count; i++) objectCreateExpressions[i] = audioEventModels[i].DumpToCSDeclaration();
            CodeArrayCreateExpression audioEventsArrayDeclaration = new CodeArrayCreateExpression(typeof(AudioEvent), objectCreateExpressions.ToArray());
            objectCreateExpressions.Clear();
            for (int i = 0; i < fxEventModels.Count; i++) objectCreateExpressions[i] = fxEventModels[i].DumpToCSDeclaration();
            CodeArrayCreateExpression fxEventsArrayDeclaration = new CodeArrayCreateExpression(typeof(FXEvent), objectCreateExpressions.ToArray());
            return new CodeObjectCreateExpression(typeof(EventBlock), new CodeExpression[] { animEventsArrayDeclaration, audioEventsArrayDeclaration, fxEventsArrayDeclaration });
        }

        /// <summary>
        /// Dump the contents of the model to the XmlNode.
        /// </summary>
        public XmlNode DumpToXmlNode()
        {
            List<XmlNode> validChildren = new List<XmlNode>();
            for (int i = 0; i < animEventModels.Count; i++) validChildren.Add(animEventModels[i].DumpToXmlNode());
            for (int i = 0; i < audioEventModels.Count; i++) validChildren.Add(audioEventModels[i].DumpToXmlNode());
            for (int i = 0; i < fxEventModels.Count; i++) validChildren.Add(fxEventModels[i].DumpToXmlNode());
            BattleActionTool.CleanNode(xmlNode, validChildren);
            return xmlNode;
        }

        /// <summary>
        /// Get an array of built anim events from the models included in this event block model.
        /// </summary>
        private AnimEvent[] DumpAnimEvents()
        {
            AnimEvent[] animEvents = new AnimEvent[animEventModels.Count];
            for (int i = 0; i < animEvents.Length; i++) animEvents[i] = animEventModels[i].animEvent;
            return animEvents;
        }

        /// <summary>
        /// Get an array of built audio events from the models included in this event block model.
        /// </summary>
        private AudioEvent[] DumpAudioEvents()
        {
            AudioEvent[] audioEvents = new AudioEvent[audioEventModels.Count];
            for (int i = 0; i < audioEvents.Length; i++) audioEvents[i] = audioEventModels[i].audioEvent;
            return audioEvents;
        }

        /// <summary>
        /// Get an array of built FX events from the models included in this event block model.
        /// </summary>
        private FXEvent[] DumpFXEvents()
        {
            FXEvent[] fxEvents = new FXEvent[fxEventModels.Count];
            for (int i = 0; i < fxEvents.Length; i++) fxEvents[i] = fxEventModels[i].fxEvent;
            return fxEvents;
        }
    }
}
#endif