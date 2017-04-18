using UnityEngine;
using System;
using System.Collections.Generic;
using System.Xml;

namespace CnfBattleSys
{
    /// <summary>
    /// Data storage static class.
    /// This is the formation table.
    /// </summary>
    public static class FormationDatabase
    {
        private static BattleFormation[] _formations;
        private static string[] sideNames = { "GenericEnemySide", "GenericAlliedSide", "GenericNeutralSide" };
        private static string[] sideNodes = { "//GenericEnemySide", "//GenericAlliedSide", "//GenericNeutralSide" };
        private static List<BattleFormation.FormationMember> listBuffer;

        /// <summary>
        /// Contains special-purpose formation defs.
        /// </summary>
        public static class SpecialFormations
        {
            /// <summary>
            /// Number of special formations.
            /// </summary>
            public const int count = 1;

            /// <summary>
            /// Invalid formation data.
            /// </summary>
            public static readonly BattleFormation defaultFormation = new BattleFormation(FormationType.InvalidFormation, VenueType.None, BGMTrackType.None, BattleFormationFlags.None, Vector2.zero, new BattleFormation.FormationMember[0]);

            /// <summary>
            /// "No formation." Not included in count, because it plugs into the table.
            /// </summary>
            public static readonly BattleFormation noneFormation = new BattleFormation(FormationType.None, VenueType.None, BGMTrackType.None, BattleFormationFlags.None, Vector2.zero, new BattleFormation.FormationMember[0]);


        }

        /// <summary>
        /// Loads in and parses all of the xml files, populates the formation dataset.
        /// This should only ever run once.
        /// </summary>
        public static void Load ()
        {
            XmlDocument doc = new XmlDocument();
            XmlNode workingNode = doc.DocumentElement;
            int c = Enum.GetValues(typeof(FormationType)).Length - SpecialFormations.count;
            _formations = new BattleFormation[c];
            for (int f = 1; f < c; f++) _formations[f] = ImportFormationWithID((FormationType)f, doc, workingNode);
        }

        /// <summary>
        /// Imports a formation from the xml file.
        /// </summary>
        private static BattleFormation ImportFormationWithID (FormationType formationType, XmlDocument doc, XmlNode workingNode)
        {
            const string formationDefsResourcePath = "Battle/FormationDefs/";
            if (listBuffer == null) listBuffer = new List<BattleFormation.FormationMember>();
            else listBuffer.Clear();
            TextAsset unreadFileBuffer = Resources.Load<TextAsset>(formationDefsResourcePath + formationType.ToString());
            if (unreadFileBuffer != null) doc.LoadXml(unreadFileBuffer.text);
            else
            {
                Debug.Log(formationType.ToString() + " has no formation def file, so the invalid formation placeholder was loaded instead.");
                return SpecialFormations.defaultFormation;
            }
            XmlNode rootNode = doc.DocumentElement;
            Action<string> actOnNode = (node) =>
            {
                workingNode = rootNode.SelectSingleNode(node);
                if (workingNode == null) throw new Exception(formationType.ToString() + " has no node " + node);
            };
            actOnNode("//venue");
            VenueType venue = DBTools.ParseVenueType(workingNode.InnerText);
            actOnNode("//bgm");
            BGMTrackType bgmTrack = DBTools.ParseBGMTrackType(workingNode.InnerText);
            actOnNode("//flags");
            BattleFormationFlags flags = DBTools.ParseBattleFormationFlags(workingNode.InnerText);
            actOnNode("//fieldSize");
            Vector2 fieldSize = DBTools.ParseVector2(workingNode.InnerText);
            actOnNode("//battlers");
            XmlNode sideNode = doc.DocumentElement;
            Func<string, bool> actONSideNode = (node) =>
            {
                sideNode = workingNode.SelectSingleNode(node);
                if (sideNode == null) return false; // formation doesn't have battlers on this side
                else return true; // able to get battlers from this side
            };
            for (int i = 0; i < sideNodes.Length; i++)
            {
                if (actONSideNode(sideNodes[i]))
                {
                    BattlerSideFlags side = DBTools.ParseBattlerSideFlags(sideNames[i]);
                    XmlNodeList battlers = sideNode.SelectNodes("//battler");
                    for (int b = 0; b < battlers.Count; b++)
                    {
                        BattlerData battler = BattlerDatabase.Get(DBTools.ParseBattlerType(battlers[i].SelectSingleNode("//battlerType").InnerText));
                        Vector2 pos = DBTools.ParseVector2(battlers[i].SelectSingleNode("//position").InnerText);
                        BattleStance startStance = StanceDatabase.Get(DBTools.ParseStanceType(battlers[i].SelectSingleNode("//startStance").InnerText));
                        listBuffer.Add(new BattleFormation.FormationMember(battler, pos, startStance, side));
                    }
                }
            }
            BattleFormation.FormationMember[] members = listBuffer.ToArray();
            return new BattleFormation(formationType, venue, bgmTrack, flags, fieldSize, members);
        }

        /// <summary>
        /// Gets a BattleFormation corresponding to formationID from the dataset.
        /// </summary>
        public static BattleFormation Get(FormationType formationID)
        {
            return _formations[(int)formationID];
        }
    }
}