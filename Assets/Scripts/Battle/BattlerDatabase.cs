using UnityEngine;
using System;
using System.Xml;

namespace CnfBattleSys
{
    /// <summary>
    /// Data storage static class.
    /// Loads, stores, and fetches unit data.
    /// </summary>
    public static class BattlerDatabase
    {
        private static BattlerData[] _battlerData;
        private static readonly BattlerData defaultBattler;

        /// <summary>
        /// Loads in and parses all the xml files, parses the unit dataset.
        /// This should only ever run once.
        /// </summary>
        private static void Load ()
        {
            XmlDocument doc = new XmlDocument();
            XmlNode workingNode = doc.DocumentElement;
            int c = Enum.GetValues(typeof(BattlerType)).Length - 1;
            _battlerData = new BattlerData[c];
            //for (int a = 0; a < c; a++) _actions[a] = ImportActionDefWithID((ActionType)a, doc, workingNode);
        }

        /// <summary>
        /// Loads in a battler definition from the XML file.
        /// </summary>
        private static BattlerData ImportUnitDefWithID (BattlerType battlerType, XmlDocument doc, XmlNode workingNode)
        {
            const string unitDefsResourcePath = "Battle/UnitDefs/";
            TextAsset unreadFileBuffer = Resources.Load<TextAsset>(unitDefsResourcePath + battlerType.ToString());
            if (unreadFileBuffer != null) doc.LoadXml(unreadFileBuffer.text);
            else
            {
                Debug.Log(battlerType.ToString() + " has no unit def file, so the invalid battler placeholder was loaded instead.");
                return defaultBattler;
            }
            XmlNode rootNode = doc.DocumentElement;
            Action<string> actOnNode = (node) =>
            {
                workingNode = rootNode.SelectSingleNode(node);
                if (workingNode == null) throw new Exception(battlerType.ToString() + " has no node " + node);
            };
            bool isFixedStats;
            actOnNode("statsType");
            if (workingNode.InnerText == "fixed") isFixedStats = true;
            else if (workingNode.InnerText == "scaling") isFixedStats = false;
            else throw new Exception(workingNode.InnerText + " isn't a valid statsType option");
            actOnNode("aiType");
            BattlerAIType aiType = DBTools.ParseBattlerAIType(workingNode.InnerText);
            throw new Exception("not done yet lol");
        }
    }

}