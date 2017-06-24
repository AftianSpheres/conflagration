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

        /// <summary>
        /// Special-case battler data definitions.
        /// </summary>
        public static class SpecialBattlers
        {
            /// <summary>
            /// Number of special battler data entries.
            /// </summary>
            public const int count = 1;

            /// <summary>
            /// "Invalid data" battler data entry.
            /// </summary>
            public static readonly BattlerData defaultBattler = new BattlerData(BattlerType.InvalidUnit, true, BattlerAIType.None, BattlerAIFlags.None, 0, 0, 1, 0, new BattleStance[0], StanceDatabase.SpecialStances.defaultStance,
                10, 1, 1, 1, 1, 1, 1, 1, 0, 0, new BattlerData.Growths(1, 1, 1, 1, 1, 1, 1, 1), new Battler.Resistances_Raw(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1), AudioEventResolverTableType.None, 1);

            /// <summary>
            /// "No battler" battler data entry. This gets plugged into the table, and shouldn't be included in count.
            /// </summary>
            public static readonly BattlerData noneBattler = new BattlerData(BattlerType.None, true, BattlerAIType.None, BattlerAIFlags.None, 0, 0, 1, 0, new BattleStance[0], StanceDatabase.SpecialStances.defaultStance,
                10, 1, 1, 1, 1, 1, 1, 1, 0, 0, new BattlerData.Growths(1, 1, 1, 1, 1, 1, 1, 1), new Battler.Resistances_Raw(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1), AudioEventResolverTableType.None, 1);
        }

        static BattlerDatabase()
        {
            Load();
        }

        /// <summary>
        /// Loads in and parses all the xml files, parses the unit dataset.
        /// This should only ever run once.
        /// </summary>
        public static void Load ()
        {
            XmlDocument doc = new XmlDocument();
            XmlNode workingNode = doc.DocumentElement;
            int c = Enum.GetValues(typeof(BattlerType)).Length - SpecialBattlers.count;
            _battlerData = new BattlerData[c];
            _battlerData[0] = SpecialBattlers.noneBattler;
            for (int b = 1; b < c; b++) _battlerData[b] = ImportUnitDefWithID((BattlerType)b, doc, workingNode);
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
                return SpecialBattlers.defaultBattler;
            }
            XmlNode rootNode = doc.DocumentElement;
            Action<string> actOnNode = (node) =>
            {
                workingNode = rootNode.SelectSingleNode(node);
                if (workingNode == null) Util.Crash(new Exception(battlerType.ToString() + " has no node " + node));
            };
            bool isFixedStats = false;
            actOnNode("statsType");
            if (workingNode.InnerText == "fixed") isFixedStats = true;
            else if (workingNode.InnerText == "scaling") isFixedStats = false;
            else Util.Crash(new Exception(workingNode.InnerText + " isn't a valid statsType option"));
            actOnNode("aiType");
            BattlerAIType aiType = DBTools.ParseBattlerAIType(workingNode.InnerText);
            actOnNode("aiFlags");
            BattlerAIFlags aiFlags = DBTools.ParseBattlerAIFlags(workingNode.InnerText);
            actOnNode("level");
            byte level = byte.Parse(workingNode.InnerText);
            actOnNode("size");
            float size = float.Parse(workingNode.InnerText);
            actOnNode("stepTime");
            float stepTime = float.Parse(workingNode.InnerText);
            actOnNode("yOffset");
            float yOffset = float.Parse(workingNode.InnerText);
            actOnNode("audioEventResolverTable");
            AudioEventResolverTableType audioEventResolverTableType = DBTools.ParseAudioEventResolverTableType(workingNode.InnerText);
            actOnNode("stances");
            XmlNodeList stanceNodes = workingNode.SelectNodes("stance");
            BattleStance[] stances = new BattleStance[stanceNodes.Count];
            for (int i = 0; i < stances.Length; i++) stances[i] = StanceDatabase.Get(DBTools.ParseStanceType(stanceNodes[i].InnerText));
            actOnNode("metaStance");
            BattleStance metaStance = StanceDatabase.Get(DBTools.ParseStanceType(workingNode.InnerText));
            actOnNode("baseStats/MaxHP");
            int baseHP = int.Parse(workingNode.InnerText);
            actOnNode("baseStats/ATK");
            ushort baseATK = ushort.Parse(workingNode.InnerText);
            actOnNode("baseStats/DEF");
            ushort baseDEF = ushort.Parse(workingNode.InnerText);
            actOnNode("baseStats/MATK");
            ushort baseMATK = ushort.Parse(workingNode.InnerText);
            actOnNode("baseStats/MDEF");
            ushort baseMDEF = ushort.Parse(workingNode.InnerText);
            actOnNode("baseStats/SPE");
            ushort baseSPE = ushort.Parse(workingNode.InnerText);
            actOnNode("baseStats/EVA");
            ushort baseEVA = ushort.Parse(workingNode.InnerText);
            actOnNode("baseStats/HIT");
            ushort baseHIT = ushort.Parse(workingNode.InnerText);
            actOnNode("baseMoveDist");
            float baseMoveDist = float.Parse(workingNode.InnerText);
            actOnNode("baseMoveDelay");
            float baseMoveDelay = float.Parse(workingNode.InnerText);
            actOnNode("fxScale");
            float fxScale = float.Parse(workingNode.InnerText);
            XmlNode secondaryNode = rootNode.SelectSingleNode("resistances");
            Battler.Resistances_Raw resistances = DBTools.GetResistancesFromXML(secondaryNode, workingNode);
            BattlerData.Growths growths;
            if (isFixedStats)
            {
                growths = new BattlerData.Growths(1, 1, 1, 1, 1, 1, 1, 1);
            }
            else
            {
                secondaryNode = rootNode.SelectSingleNode("growths");
                growths = DBTools.GetGrowthsFromXML(secondaryNode, workingNode);
            }
            Resources.UnloadAsset(unreadFileBuffer);
            return new BattlerData(battlerType, isFixedStats, aiType, aiFlags, level, size, stepTime, yOffset, stances, metaStance,
                baseHP, baseATK, baseDEF, baseMATK, baseMDEF, baseSPE, baseHIT, baseEVA, baseMoveDist, baseMoveDelay, growths, resistances, audioEventResolverTableType, fxScale);
        }

        /// <summary>
        /// Gets battler data for battlerType
        /// </summary>
        public static BattlerData Get (BattlerType battlerType)
        {
            return _battlerData[(int)battlerType];
        }
    }

}