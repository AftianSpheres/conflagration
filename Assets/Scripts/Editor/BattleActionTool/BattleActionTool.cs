#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace BattleActionTool
{
    /// <summary>
    /// Methods for use in parsing battle action definitions and maintaining the models.
    /// </summary>
    public static class BattleActionTool
    {
        /// <summary>
        /// Select given node, and if it exists, attempt to execute callback.
        /// </summary>
        public static void ActOnNode(XmlNode parent, string xpath, Action<XmlNode> callback)
        {
            XmlNode workingNode = parent.SelectSingleNode(xpath);
            if (workingNode != null)
            {
                try { callback(workingNode); }
                catch { Debug.LogError("Node " + parent.Name + " had an invalid value set in child node " + workingNode.Name); }
            }
        }

        /// <summary>
        /// Removes all invalid children of parent node.
        /// </summary>
        public static void CleanNode(XmlNode parent, List<XmlNode> validChildren)
        {
            for (int i = parent.ChildNodes.Count; i > -1; i--)
            {
                if (parent.ChildNodes[i] == null) continue;
                XmlNode node = parent.ChildNodes[i];
                if (!validChildren.Contains(node))
                {
                    //Debug.Log("Invalid child " + node.Name + " in " + parent.Name);
                    parent.RemoveChild(node);
                }
            }
        }

        /// <summary>
        /// Select the node if it exists; create it if it doesn't.
        /// </summary>
        public static XmlNode HandleChildNode(XmlNode parent, string name, Action<XmlNode> callback, List<XmlNode> validChildren, XmlNodeType nodeType = XmlNodeType.Element)
        {
            XmlNode node = parent.SelectSingleNode(name);
            if (node == null)
            {
                if (nodeType == XmlNodeType.Attribute) node = parent.Attributes.Append(parent.OwnerDocument.CreateAttribute(name));
                else node = parent.AppendChild(parent.OwnerDocument.CreateNode(nodeType, name, parent.NamespaceURI));
            }
            callback(node);
            validChildren.Add(node);
            return node;
        }
    }
}
#endif