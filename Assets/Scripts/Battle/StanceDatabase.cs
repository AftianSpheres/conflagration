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

        /// <summary>
        /// Special-case stance defs.
        /// </summary>
        public static class SpecialStances
        {
            /// <summary>
            /// Number od special stances.
            /// </summary>
            public const int count = 1;

            /// <summary>
            /// Default stance entry.
            /// </summary>
            public static readonly BattleStance defaultStance = new BattleStance(StanceType.InvalidStance, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                new BattleAction[0], ActionDatabase.SpecialActions.defaultBattleAction, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
                new Battler.Resistances_Raw(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));

            /// <summary>
            /// Stance entry representing "no stance."
            /// This gets plugged into the table. Don't count it as part of the special stance count, as such.
            /// </summary>
            public static readonly BattleStance noneStance = new BattleStance(StanceType.None, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                new BattleAction[0], ActionDatabase.SpecialActions.defaultBattleAction, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
                new Battler.Resistances_Raw(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));

        }

        /// <summary>
        /// Loads in and parses all the xml files for the stance dataset.
        /// This should only ever run once.
        /// </summary>
        public static void Load ()
        {
            XmlDocument doc = new XmlDocument();
            XmlNode workingNode = doc.DocumentElement;
            int c = Enum.GetValues(typeof(StanceType)).Length - SpecialStances.count;
            _stances = new BattleStance[c];
            _stances[0] = SpecialStances.noneStance;
            for (int s = 1; s < c; s++) _stances[s] = ImportStanceDefWithID((StanceType)s, doc, workingNode);
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
                return SpecialStances.defaultStance;
            }
            XmlNode rootNode = doc.DocumentElement;
            XmlNode resNode = rootNode.SelectSingleNode("//resistances");
            Action<string> actOnNode = (node) =>
            {
                workingNode = rootNode.SelectSingleNode(node);
                if (workingNode == null) throw new Exception(stanceID.ToString() + " has no node " + node);
            };
            actOnNode("//animNames/break");
            string animName_Break = workingNode.InnerText;
            actOnNode("//animNames/die");
            string animName_Die = workingNode.InnerText;
            actOnNode("//animNames/dodge");
            string animName_Dodge = workingNode.InnerText;
            actOnNode("//animNames/idle");
            string animName_Idle = workingNode.InnerText;
            actOnNode("//animNames/heal");
            string animName_Heal = workingNode.InnerText;
            actOnNode("//animNames/hit");
            string animName_Hit = workingNode.InnerText;
            actOnNode("//animNames/move");
            string animName_Move = workingNode.InnerText;
            actOnNode("//actions");
            XmlNodeList actionNodes = workingNode.SelectNodes("//action");
            BattleAction[] actionSet = new BattleAction[actionNodes.Count];
            for (int i = 0; i < actionNodes.Count; i++)
            {
                actionSet[i] = ActionDatabase.Get(DBTools.ParseActionType(actionNodes[i].InnerText));
            }
            actOnNode("//counterattackAction");
            BattleAction counterattackAction = ActionDatabase.Get(DBTools.ParseActionType(workingNode.InnerText));
            actOnNode("//moveDelayBonus");
            float moveDelayBonus = float.Parse(workingNode.InnerText);
            actOnNode("//moveDelayMultiplier");
            float moveDelayMultiplier = float.Parse(workingNode.InnerText);
            actOnNode("//moveDistBonus");
            float moveDistBonus = float.Parse(workingNode.InnerText);
            actOnNode("//moveDistMultiplier");
            float moveDistMultiplier = float.Parse(workingNode.InnerText);
            actOnNode("//stanceChangeDelayBonus");
            float stanceChangeDelayBonus = float.Parse(workingNode.InnerText);
            actOnNode("//stanceChangeDelayMultiplier");
            float stanceChangeDelayMultiplier = float.Parse(workingNode.InnerText);
            actOnNode("//statMultis/MaxHP");
            float statMultiplier_MaxHP = float.Parse(workingNode.InnerText);
            actOnNode("//statMultis/ATK");
            float statMultiplier_ATK = float.Parse(workingNode.InnerText);
            actOnNode("//statMultis/DEF");
            float statMultiplier_DEF = float.Parse(workingNode.InnerText);
            actOnNode("//statMultis/MATK");
            float statMultiplier_MATK = float.Parse(workingNode.InnerText);
            actOnNode("//statMultis/MDEF");
            float statMultiplier_MDEF = float.Parse(workingNode.InnerText);
            actOnNode("//statMultis/SPE");
            float statMultiplier_SPE = float.Parse(workingNode.InnerText);
            actOnNode("//statMultis/HIT");
            float statMultiplier_HIT = float.Parse(workingNode.InnerText);
            actOnNode("//statMultis/EVA");
            float statMultiplier_EVA = float.Parse(workingNode.InnerText);
            actOnNode("//statBonus/MaxHP");
            int statBonus_MaxHP = int.Parse(workingNode.InnerText);
            actOnNode("//statBonus/ATK");
            short statBonus_ATK = short.Parse(workingNode.InnerText);
            actOnNode("//statBonus/DEF");
            short statBonus_DEF = short.Parse(workingNode.InnerText);
            actOnNode("//statBonus/MATK");
            short statBonus_MATK = short.Parse(workingNode.InnerText);
            actOnNode("//statBonus/MDEF");
            short statBonus_MDEF = short.Parse(workingNode.InnerText);
            actOnNode("//statBonus/SPE");
            short statBonus_SPE = short.Parse(workingNode.InnerText);
            actOnNode("//statBonus/HIT");
            short statBonus_HIT = short.Parse(workingNode.InnerText);
            actOnNode("//statBonus/EVA");
            short statBonus_EVA = short.Parse(workingNode.InnerText);
            actOnNode("//maxSP");
            byte maxSP = byte.Parse(workingNode.InnerText);
            Battler.Resistances_Raw resistances = DBTools.GetResistancesFromXML(resNode, workingNode);
            Resources.UnloadAsset(unreadFileBuffer);
            return new BattleStance(stanceID, animName_Break, animName_Die, animName_Dodge, animName_Heal, animName_Hit, animName_Idle, animName_Move, actionSet, counterattackAction, moveDelayBonus, moveDelayMultiplier, 
                moveDistBonus, moveDistMultiplier, stanceChangeDelayBonus, stanceChangeDelayMultiplier, statMultiplier_MaxHP, statMultiplier_ATK, statMultiplier_DEF, statMultiplier_MATK, statMultiplier_MDEF, statMultiplier_SPE, 
                statMultiplier_HIT, statMultiplier_EVA, statBonus_MaxHP, statBonus_ATK, statBonus_DEF, statBonus_MATK, statBonus_MDEF, statBonus_SPE, statBonus_HIT, statBonus_EVA, maxSP, resistances);
            
        }

        /// <summary>
        /// Gets a BattleStance corresponding to stanceID from the dataset.
        /// </summary>
        public static BattleStance Get (StanceType stanceID)
        {
            return _stances[(int)stanceID];
        }
    }
}
