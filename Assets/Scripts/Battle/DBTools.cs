﻿using UnityEngine;
using System;
using System.Xml;

namespace CnfBattleSys
{
    /// <summary>
    /// Static class that implements the shared parsers and shit for the action/stance/battler databases.
    /// </summary>
    public static class DBTools
    {
        /// <summary>
        /// Takes a string, spits out an ActionTargetType.
        /// </summary>
        public static ActionTargetType ParseActionTargetType(string s)
        {
            return (ActionTargetType)Enum.Parse(typeof(ActionTargetType), s);
        }

        /// <summary>
        /// Takes a string, spits out the corresponding AnimEventType.
        /// </summary>
        public static AnimEventType ParseAnimEventType(string s)
        {
            return (AnimEventType)Enum.Parse(typeof(AnimEventType), s);
        }

        /// <summary>
        /// Takes a string, spits out the corresponding LogicalStatType.
        /// </summary>
        public static LogicalStatType ParseLogicalStatType(string s)
        {
            return (LogicalStatType)Enum.Parse(typeof(LogicalStatType), s);
        }

        /// <summary>
        /// Takes a string, spits out the corresponding SubactionFXType.
        /// </summary>
        public static SubactionFXType ParseSubactionFXType(string s)
        {
            return (SubactionFXType)Enum.Parse(typeof(SubactionFXType), s);
        }

        /// <summary>
        /// Takes a CSV string, spits out target side bitflags.
        /// </summary>
        public static TargetSideFlags ParseTargetSideFlags(string s)
        {
            return (TargetSideFlags)Enum.Parse(typeof(TargetSideFlags), s);
        }

        /// <summary>
        /// Takes a CSV string, spits out damage type bitflags.
        /// </summary>
        public static DamageTypeFlags ParseDamageTypeFlags(string s)
        {
            return (DamageTypeFlags)Enum.Parse(typeof(DamageTypeFlags), s);
        }

        /// <summary>
        /// Parses a BattlerType.
        /// </summary>
        public static BattlerType ParseBattlerType(string s)
        {
            return (BattlerType)Enum.Parse(typeof(BattlerType), s);
        }

        /// <summary>
        /// Parses a BattlerAIType.
        /// </summary>
        public static BattlerAIType ParseBattlerAIType(string s)
        {
            return (BattlerAIType)Enum.Parse(typeof(BattlerAIType), s);
        }

        /// <summary>
        /// Parses CSV string into BattlerAIFlags bitflags
        /// </summary>
        public static BattlerAIFlags ParseBattlerAIFlags (string s)
        {
            return (BattlerAIFlags)Enum.Parse(typeof(BattlerAIFlags), s);
        }

        /// <summary>
        /// Parses CSV string into BattlerSideFlags bitflags
        /// </summary>
        public static BattlerSideFlags ParseBattlerSideFlags(string s)
        {
            return (BattlerSideFlags)Enum.Parse(typeof(BattlerSideFlags), s);
        }

        /// <summary>
        /// Parse BattlerModelType from string
        /// </summary>
        public static BattlerModelType ParseBattlerModelType (string s)
        {
            return (BattlerModelType)Enum.Parse(typeof(BattlerModelType), s);
        }

        /// <summary>
        /// Parses StanceType from string
        /// </summary>
        public static StanceType ParseStanceType (string s)
        {
            return (StanceType)Enum.Parse(typeof(StanceType), s);
        }

        /// <summary>
        /// Parse for action ID
        /// </summary>
        public static ActionType ParseActionType(string s)
        {
            return (ActionType)Enum.Parse(typeof(ActionType), s);
        }

        /// <summary>
        /// Parse for venue type.
        /// </summary>
        public static VenueType ParseVenueType (string s)
        {
            return (VenueType)Enum.Parse(typeof(VenueType), s);
        }

        /// <summary>
        /// Parse for BGM track type.
        /// </summary>
        public static BGMTrackType ParseBGMTrackType (string s)
        {
            return (BGMTrackType)Enum.Parse(typeof(BGMTrackType), s);
        }

        /// <summary>
        /// CSV string => battle formation bitflags
        /// </summary>
        public static BattleFormationFlags ParseBattleFormationFlags (string s)
        {
            return (BattleFormationFlags)Enum.Parse(typeof(BattleFormationFlags), s);
        }

        /// <summary>
        /// Turns a string of format (float), (float) into a vector2.
        /// </summary>
        public static Vector2 ParseVector2 (string s)
        {
            string[] substrings = s.Split(',');
            if (substrings.Length > 2) throw new Exception("Unrecognized vector2-as-string encoding: " + s);
            return new Vector2(float.Parse(substrings[0]), float.Parse(substrings[1]));
        }

        /// <summary>
        /// Gets growths info out of growth node's children.
        /// </summary>
        public static BattlerData.Growths GetGrowthsFromXML (XmlNode growthsNode, XmlNode workingNode)
        {
            Action<string> actOnNode = (node) =>
            {
                workingNode = growthsNode.SelectSingleNode(node);
                if (workingNode == null) throw new Exception("Malformed growths set in xml file - missing node  " + node);
            };
            actOnNode("HP");
            float HP = float.Parse(workingNode.InnerText);
            actOnNode("ATK");
            float ATK = float.Parse(workingNode.InnerText);
            actOnNode("DEF");
            float DEF = float.Parse(workingNode.InnerText);
            actOnNode("MATK");
            float MATK = float.Parse(workingNode.InnerText);
            actOnNode("MDEF");
            float MDEF = float.Parse(workingNode.InnerText);
            actOnNode("SPE");
            float SPE = float.Parse(workingNode.InnerText);
            actOnNode("HIT");
            float HIT = float.Parse(workingNode.InnerText);
            actOnNode("EVA");
            float EVA = float.Parse(workingNode.InnerText);
            return new BattlerData.Growths(HP, ATK, DEF, MATK, MDEF, SPE, EVA, HIT);
        }
 
        /// <summary>
        /// Gets resistance info out of resistances node's children.
        /// </summary>
        public static Battler.Resistances_Raw GetResistancesFromXML(XmlNode resNode, XmlNode workingNode)
        {
            float r_global = 1;
            float r_magic = 1;
            float r_strike = 1;
            float r_slash = 1;
            float r_thrust = 1;
            float r_fire = 1;
            float r_earth = 1;
            float r_air = 1;
            float r_water = 1;
            float r_light = 1;
            float r_dark = 1;
            float r_bio = 1;
            float r_sound = 1;
            float r_psyche = 1;
            float r_reality = 1;
            float r_time = 1;
            float r_space = 1;
            float r_ice = 1;
            float r_electric = 1;
            float r_spirit = 1;
            if (resNode != null)
            {
                workingNode = resNode.SelectSingleNode("global");
                if (workingNode != null) r_global = float.Parse(workingNode.InnerText);
                workingNode = resNode.SelectSingleNode("magic");
                if (workingNode != null) r_magic = float.Parse(workingNode.InnerText);
                workingNode = resNode.SelectSingleNode("strike");
                if (workingNode != null) r_strike = float.Parse(workingNode.InnerText);
                workingNode = resNode.SelectSingleNode("slash");
                if (workingNode != null) r_slash = float.Parse(workingNode.InnerText);
                workingNode = resNode.SelectSingleNode("thrust");
                if (workingNode != null) r_thrust = float.Parse(workingNode.InnerText);
                workingNode = resNode.SelectSingleNode("fire");
                if (workingNode != null) r_fire = float.Parse(workingNode.InnerText);
                workingNode = resNode.SelectSingleNode("earth");
                if (workingNode != null) r_earth = float.Parse(workingNode.InnerText);
                workingNode = resNode.SelectSingleNode("air");
                if (workingNode != null) r_air = float.Parse(workingNode.InnerText);
                workingNode = resNode.SelectSingleNode("water");
                if (workingNode != null) r_water = float.Parse(workingNode.InnerText);
                workingNode = resNode.SelectSingleNode("light");
                if (workingNode != null) r_light = float.Parse(workingNode.InnerText);
                workingNode = resNode.SelectSingleNode("dark");
                if (workingNode != null) r_dark = float.Parse(workingNode.InnerText);
                workingNode = resNode.SelectSingleNode("bio");
                if (workingNode != null) r_bio = float.Parse(workingNode.InnerText);
                workingNode = resNode.SelectSingleNode("sound");
                if (workingNode != null) r_sound = float.Parse(workingNode.InnerText);
                workingNode = resNode.SelectSingleNode("psyche");
                if (workingNode != null) r_psyche = float.Parse(workingNode.InnerText);
                workingNode = resNode.SelectSingleNode("reality");
                if (workingNode != null) r_reality = float.Parse(workingNode.InnerText);
                workingNode = resNode.SelectSingleNode("time");
                if (workingNode != null) r_time = float.Parse(workingNode.InnerText);
                workingNode = resNode.SelectSingleNode("space");
                if (workingNode != null) r_space = float.Parse(workingNode.InnerText);
                workingNode = resNode.SelectSingleNode("ice");
                if (workingNode != null) r_ice = float.Parse(workingNode.InnerText);
                workingNode = resNode.SelectSingleNode("electric");
                if (workingNode != null) r_electric = float.Parse(workingNode.InnerText);
                workingNode = resNode.SelectSingleNode("spirit");
                if (workingNode != null) r_spirit = float.Parse(workingNode.InnerText);
            }
            return new Battler.Resistances_Raw(r_global, r_magic, r_strike, r_slash, r_thrust, r_fire, r_earth, r_air, r_water, r_light, r_dark, r_bio, r_sound, r_psyche, r_reality, r_time, r_space, r_electric, r_ice, r_spirit);
        }
    }
}
