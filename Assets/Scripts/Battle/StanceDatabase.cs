using System;
using System.Xml;
using UnityEngine;

namespace CnfBattleSys
{
    /// <summary>
    /// Static class that loads, stores, and fetches from the stance dataset
    /// </summary>
    public static class StanceDatabase
    {
        private static BattleStance[] _stances;
        private static readonly BattleStance defaultStance = new BattleStance(StanceType.InvalidStance, AnimEventType.None, AnimEventType.None, AnimEventType.None, AnimEventType.None, AnimEventType.None, new BattleAction[0], ActionDatabase.defaultBattleAction,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, new Battler.Resistances_Raw(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));

        /// <summary>
        /// Loads in and parses all the xml files for the stance dataset.
        /// This should only ever run once.
        /// </summary>
        private static void LoadStances ()
        {
            XmlDocument doc = new XmlDocument();
            XmlNode workingNode = doc.DocumentElement;
            int c = Enum.GetValues(typeof(StanceType)).Length - 1;
            _stances = new BattleStance[c];
            for (int s = 0; s < c; s++) _stances[s] = ImportStanceDefWithID((StanceType)s, doc, workingNode);
        }

        /// <summary>
        /// Loads in a stance def from the xml file.
        /// </summary>
        private static BattleStance ImportStanceDefWithID (StanceType stanceID, XmlDocument doc, XmlNode workingNode)
        {
            const string stanceDefsResourcePath = "Battle/StanceDefs/";
            TextAsset unreadFileBuffer = Resources.Load<TextAsset>(stanceDefsResourcePath + stanceID.ToString());
            if (unreadFileBuffer != null) doc.LoadXml(unreadFileBuffer.text);
            else
            {
                Debug.Log(stanceID.ToString() + " has no stance def file, so the invalid stance placeholder was loaded instead.");
                return defaultStance;
            }
            XmlNode rootNode = doc.DocumentElement;
            XmlNode resNode = rootNode.SelectSingleNode("resistances");
            Action<string> actOnNode = (node) =>
            {
                workingNode = rootNode.SelectSingleNode(node);
                if (workingNode == null) throw new Exception(stanceID.ToString() + " has no node " + node);
            };
            actOnNode("animEvent_Idle");
            AnimEventType animEvent_Idle = DBTools.ParseAnimEventType(workingNode.InnerText);
            actOnNode("animEvent_Move");
            AnimEventType animEvent_Move = DBTools.ParseAnimEventType(workingNode.InnerText);
            actOnNode("animEvent_Hit");
            AnimEventType animEvent_Hit = DBTools.ParseAnimEventType(workingNode.InnerText);
            actOnNode("animEvent_Break");
            AnimEventType animEvent_Break = DBTools.ParseAnimEventType(workingNode.InnerText);
            actOnNode("animEvent_Dodge");
            AnimEventType animEvent_Dodge = DBTools.ParseAnimEventType(workingNode.InnerText);
            actOnNode("actions");
            XmlNodeList actionNodes = workingNode.SelectNodes("action");
            BattleAction[] actionSet = new BattleAction[actionNodes.Count];
            for (int i = 0; i < actionNodes.Count; i++)
            {
                actionSet[i] = ActionDatabase.Get(BattleAction.ParseToActionID(actionNodes[i].InnerText));
            }
            actOnNode("counterattackAction");
            BattleAction counterattackAction = ActionDatabase.Get(BattleAction.ParseToActionID(workingNode.InnerText));
            actOnNode("moveDelayBonus");
            float moveDelayBonus = float.Parse(workingNode.InnerText);
            actOnNode("moveDelayMultiplier");
            float moveDelayMultiplier = float.Parse(workingNode.InnerText);
            actOnNode("moveDistBonus");
            float moveDistBonus = float.Parse(workingNode.InnerText);
            actOnNode("moveDistMultiplier");
            float moveDistMultiplier = float.Parse(workingNode.InnerText);
            actOnNode("stanceChangeDelayBonus");
            float stanceChangeDelayBonus = float.Parse(workingNode.InnerText);
            actOnNode("stanceChangeDelayMultiplier");
            float stanceChangeDelayMultiplier = float.Parse(workingNode.InnerText);
            actOnNode("statMultis/MaxHP");
            float statMultiplier_MaxHP = float.Parse(workingNode.InnerText);
            actOnNode("statMultis/ATK");
            float statMultiplier_ATK = float.Parse(workingNode.InnerText);
            actOnNode("statMultis/DEF");
            float statMultiplier_DEF = float.Parse(workingNode.InnerText);
            actOnNode("statMultis/MATK");
            float statMultiplier_MATK = float.Parse(workingNode.InnerText);
            actOnNode("statMultis/MDEF");
            float statMultiplier_MDEF = float.Parse(workingNode.InnerText);
            actOnNode("statMultis/SPE");
            float statMultiplier_SPE = float.Parse(workingNode.InnerText);
            actOnNode("statMultis/HIT");
            float statMultiplier_HIT = float.Parse(workingNode.InnerText);
            actOnNode("statMultis/EVA");
            float statMultiplier_EVA = float.Parse(workingNode.InnerText);
            actOnNode("statBonus/MaxHP");
            int statBonus_MaxHP = int.Parse(workingNode.InnerText);
            actOnNode("statBonus/ATK");
            int statBonus_ATK = int.Parse(workingNode.InnerText);
            actOnNode("statBonus/DEF");
            int statBonus_DEF = int.Parse(workingNode.InnerText);
            actOnNode("statBonus/MATK");
            int statBonus_MATK = int.Parse(workingNode.InnerText);
            actOnNode("statBonus/MDEF");
            int statBonus_MDEF = int.Parse(workingNode.InnerText);
            actOnNode("statBonus/SPE");
            int statBonus_SPE = int.Parse(workingNode.InnerText);
            actOnNode("statBonus/HIT");
            int statBonus_HIT = int.Parse(workingNode.InnerText);
            actOnNode("statBonus/EVA");
            int statBonus_EVA = int.Parse(workingNode.InnerText);
            actOnNode("maxSP");
            int maxSP = int.Parse(workingNode.InnerText);
            Battler.Resistances_Raw resistances = DBTools.GetResistancesFromXML(resNode, workingNode);
            Resources.UnloadAsset(unreadFileBuffer);
            return new BattleStance(stanceID, animEvent_Idle, animEvent_Move, animEvent_Hit, animEvent_Break, animEvent_Dodge, actionSet, counterattackAction, moveDelayBonus, moveDelayMultiplier, moveDistBonus, moveDistMultiplier,
                stanceChangeDelayBonus, stanceChangeDelayMultiplier, statMultiplier_MaxHP, statMultiplier_ATK, statMultiplier_DEF, statMultiplier_MATK, statMultiplier_MDEF, statMultiplier_SPE, statMultiplier_HIT, statMultiplier_EVA,
                statBonus_MaxHP, statBonus_ATK, statBonus_DEF, statBonus_MATK, statBonus_MDEF, statBonus_SPE, statBonus_HIT, statBonus_EVA, maxSP, resistances);
            
        }

        /// <summary>
        /// Gets a BattleStance corresponding to stanceID from the dataset.
        /// </summary>
        public static BattleStance Get (StanceType stanceID)
        {
            if (_stances == null) LoadStances();
            return _stances[(int)stanceID];
        }
    }
}
