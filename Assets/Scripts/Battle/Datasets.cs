using System;
using System.Xml;
using UnityEngine;

namespace CnfBattleSys
{
    /// <summary>
    /// Static class that loads, stores, and fetches from the action and stance datasets.
    /// </summary>
    public static class Datasets
    {
        private static BattleAction[] _actions;
        private static BattleStance[] _stances;
        private static readonly BattleAction.Subaction[] defaultSubactionArray = { new BattleAction.Subaction(0, 0, AnimEventType.None, AnimEventType.None, LogicalStatType.None, LogicalStatType.None, LogicalStatType.None, LogicalStatType.None, -1, -1, new BattleAction.Subaction.FXPackage[0], DamageTypeFlags.None) };
        private static readonly BattleAction defaultBattleAction = new BattleAction(ActionType.InvalidAction, 0, 0, 0, 0, 0, 0, TargetSideFlags.None, ActionTargetType.None, AnimEventType.None, AnimEventType.None, AnimEventType.None, AnimEventType.None, AnimEventType.None,
                                                       defaultSubactionArray);
        private static readonly BattleStance defaultStance = new BattleStance(StanceType.InvalidStance, AnimEventType.None, AnimEventType.None, AnimEventType.None, AnimEventType.None, AnimEventType.None, new BattleAction[0], defaultBattleAction,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, new Battler.Resistances_Raw(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
        /// <summary>
        /// Loads in and parses all of the xml files, populates the action dataset.
        /// This should only ever run once.
        /// </summary>
        private static void LoadActions ()
        {
            XmlDocument doc = new XmlDocument();
            XmlNode workingNode = doc.DocumentElement;
            _actions = new BattleAction[BattleUtility.numberOfActionEntries + 1]; 
            for (int a = 0; a <= BattleUtility.numberOfActionEntries; a++) _actions[a] = ImportActionDefWithID((ActionType)a, doc, workingNode);
        }

        /// <summary>
        /// Loads in and parses all the xml files for the stance dataset.
        /// This should only ever run once.
        /// </summary>
        private static void LoadStances ()
        {
            XmlDocument doc = new XmlDocument();
            XmlNode workingNode = doc.DocumentElement;
            _stances = new BattleStance[BattleUtility.numberOfStanceEntries + 1];
            for (int s = 0; s <= BattleUtility.numberOfStanceEntries; s++) _stances[s] = ImportStanceDefWithID((StanceType)s, doc, workingNode);
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
            AnimEventType animEvent_Idle = ParseAnimEventType(workingNode.InnerText);
            actOnNode("animEvent_Move");
            AnimEventType animEvent_Move = ParseAnimEventType(workingNode.InnerText);
            actOnNode("animEvent_Hit");
            AnimEventType animEvent_Hit = ParseAnimEventType(workingNode.InnerText);
            actOnNode("animEvent_Break");
            AnimEventType animEvent_Break = ParseAnimEventType(workingNode.InnerText);
            actOnNode("animEvent_Dodge");
            AnimEventType animEvent_Dodge = ParseAnimEventType(workingNode.InnerText);
            actOnNode("actions");
            XmlNodeList actionNodes = workingNode.SelectNodes("action");
            BattleAction[] actionSet = new BattleAction[actionNodes.Count];
            for (int i = 0; i < actionNodes.Count; i++)
            {
                actionSet[i] = GetAction(BattleAction.ParseToActionID(actionNodes[i].InnerText));
            }
            actOnNode("counterattackAction");
            BattleAction counterattackAction = GetAction(BattleAction.ParseToActionID(workingNode.InnerText));
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
            Battler.Resistances_Raw resistances = GetResistancesFromXML(resNode, workingNode);
            Resources.UnloadAsset(unreadFileBuffer);
            return new BattleStance(stanceID, animEvent_Idle, animEvent_Move, animEvent_Hit, animEvent_Break, animEvent_Dodge, actionSet, counterattackAction, moveDelayBonus, moveDelayMultiplier, moveDistBonus, moveDistMultiplier,
                stanceChangeDelayBonus, stanceChangeDelayMultiplier, statMultiplier_MaxHP, statMultiplier_ATK, statMultiplier_DEF, statMultiplier_MATK, statMultiplier_MDEF, statMultiplier_SPE, statMultiplier_HIT, statMultiplier_EVA,
                statBonus_MaxHP, statBonus_ATK, statBonus_DEF, statBonus_MATK, statBonus_MDEF, statBonus_SPE, statBonus_HIT, statBonus_EVA, maxSP, resistances);
            
        }

        /// <summary>
        /// Loads in an action def from the XML file.
        /// </summary>
        private static BattleAction ImportActionDefWithID (ActionType actionID, XmlDocument doc, XmlNode workingNode)
        {
            const string actionDefsResourcePath = "Battle/ActionDefs/";
            TextAsset unreadFileBuffer = Resources.Load<TextAsset>(actionDefsResourcePath + actionID.ToString());
            if (unreadFileBuffer != null) doc.LoadXml(unreadFileBuffer.text);
            else
            {
                Debug.Log(actionID.ToString() + " has no action def file, so the invalid action placeholder was loaded instead.");
                return defaultBattleAction;
            }
            XmlNode rootNode = doc.DocumentElement;
            Action<string> actOnNode = (node) =>
            {
                workingNode = rootNode.SelectSingleNode(node);
                if (workingNode == null) throw new Exception(actionID.ToString() + " has no node " + node);
            };
            XmlNodeList SubactionsList = rootNode.SelectNodes("//subaction");
            if (SubactionsList.Count < 1) throw new Exception("Battle action " + actionID.ToString() + " has no defined Subactions!");
            BattleAction.Subaction[] Subactions = new BattleAction.Subaction[SubactionsList.Count];
            for (int s = 0; s < Subactions.Length; s++)
            {
                XmlNode SubactionNode = SubactionsList[s];
                Subactions[s] = XmlNodeToSubaction(SubactionNode, workingNode, s, actionID);
            }
            actOnNode("//baseAOERadius");
            float baseAOERadius = float.Parse(workingNode.InnerText);
            actOnNode("//baseDelay");
            float baseDelay = float.Parse(workingNode.InnerText);
            actOnNode("//baseFollowthroughStanceChangeDelay");
            float baseFollowthroughStanceChangeDelay = float.Parse(workingNode.InnerText);
            actOnNode("//baseMinimumTargetingDistance");
            float baseMinimumTargetingDistance = float.Parse(workingNode.InnerText);
            actOnNode("//baseTargetingRange");
            float baseTargetingRange = float.Parse(workingNode.InnerText);
            actOnNode("//baseSPCost");
            int baseSPCost = int.Parse(workingNode.InnerText);
            actOnNode("//targetingSideFlags");
            TargetSideFlags targetingSideFlags = ParseTargetSideFlags(workingNode.InnerText.Split(' '));
            actOnNode("//targetingType");
            ActionTargetType targetingType = ParseActionTargetType(workingNode.InnerText);
            actOnNode("//animSkipTargetHitAnim");
            AnimEventType animSkipTargetHitAnim = ParseAnimEventType(workingNode.InnerText);
            actOnNode("//onActionEndTargetAnim");
            AnimEventType onActionEndTargetAnim = ParseAnimEventType(workingNode.InnerText);
            actOnNode("//onActionEndUserAnim");
            AnimEventType onActionEndUserAnim = ParseAnimEventType(workingNode.InnerText);
            actOnNode("//onActionUseTargetAnim");
            AnimEventType onActionUseTargetAnim = ParseAnimEventType(workingNode.InnerText);
            actOnNode("//onActionUseUserAnim");
            AnimEventType onActionUseUserAnim = ParseAnimEventType(workingNode.InnerText);
            Resources.UnloadAsset(unreadFileBuffer);
            return new BattleAction(actionID, baseAOERadius, baseDelay, baseFollowthroughStanceChangeDelay, baseMinimumTargetingDistance, baseTargetingRange, baseSPCost, targetingSideFlags, targetingType,
                                animSkipTargetHitAnim, onActionEndTargetAnim, onActionEndUserAnim, onActionUseTargetAnim, onActionUseUserAnim, Subactions);

        }

        /// <summary>
        /// Gets resistance info out of resistances node's children.
        /// </summary>
        private static Battler.Resistances_Raw GetResistancesFromXML (XmlNode resNode, XmlNode workingNode)
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

        /// <summary>
        /// Parses an XML node and spits out a Subaction.
        /// </summary>
        private static BattleAction.Subaction XmlNodeToSubaction (XmlNode SubactionNode, XmlNode workingNode, int index, ActionType actionID)
        {
            Func<string> exceptionSubactionIDStr = delegate { return "Action " + actionID.ToString() + ", Subaction " + index.ToString(); };
            Action<string> actOnNode = (node) =>
            {
                workingNode = SubactionNode.SelectSingleNode(node);
                if (workingNode == null) throw new Exception(exceptionSubactionIDStr() + " has no node " + node);
            };
            XmlNodeList fxList = SubactionNode.SelectNodes("//fxPackage");
            BattleAction.Subaction.FXPackage[] fx = new BattleAction.Subaction.FXPackage[fxList.Count];
            int thisSubactionDamageTiedToSubactionAtIndex = -1;
            int thisSubactionSuccessTiedToSubactionAtIndex = -1;
            if (fx != null)
            {
                for (int f = 0; f < fx.Length; f++)
                {
                    Func<string> exceptionFXPackageIDStr = delegate { return "Action " + actionID.ToString() + ", Subaction " + index.ToString() + ", FXpackage " + f.ToString(); };
                    XmlNode fxNode = fxList[f];
                    fx[f] = XmlNodeToFXPackage(fxNode, workingNode, exceptionFXPackageIDStr, f);
                }
            }
            actOnNode("//baseDamage");
            int baseDamage = int.Parse(workingNode.InnerText);
            actOnNode("//baseAccuracy");
            float baseAccuracy = float.Parse(workingNode.InnerText);
            actOnNode("//onSubactionHitTargetAnim");
            AnimEventType onSubactionHitTargetAnim = ParseAnimEventType(workingNode.InnerText);
            actOnNode("//onSubactionExecuteUserAnim");
            AnimEventType onSubactionExecuteUserAnim = ParseAnimEventType(workingNode.InnerText);
            actOnNode("//atkStat");
            LogicalStatType atkStat = ParseLogicalStatType(workingNode.InnerText);
            actOnNode("//defStat");
            LogicalStatType defStat = ParseLogicalStatType(workingNode.InnerText);
            actOnNode("//hitStat");
            LogicalStatType hitStat = ParseLogicalStatType(workingNode.InnerText);
            actOnNode("//evadeStat");
            LogicalStatType evadeStat = ParseLogicalStatType(workingNode.InnerText);
            actOnNode("//damageTypes");
            DamageTypeFlags damageTypes = ParseDamageTypeFlags(workingNode.InnerText.Split(' '));
            workingNode = SubactionNode.SelectSingleNode("//thisSubactionDamageTiedToSubactionAtIndex");
            if (workingNode != null)
            {
                thisSubactionDamageTiedToSubactionAtIndex = int.Parse(workingNode.InnerText);
                if (thisSubactionDamageTiedToSubactionAtIndex < 0) throw new Exception(exceptionSubactionIDStr() + " tries to tie itself to an invalid Subaction index!");
                else if (thisSubactionDamageTiedToSubactionAtIndex >= index) throw new Exception(exceptionSubactionIDStr() + " tries to tie itself to a Subaction index that doesn't precede it!");
            }
            workingNode = SubactionNode.SelectSingleNode("//thisSubactionSuccessTiedToSubactionAtIndex");
            if (workingNode != null)
            {
                thisSubactionSuccessTiedToSubactionAtIndex = int.Parse(workingNode.InnerText);
                if (thisSubactionSuccessTiedToSubactionAtIndex < 0) throw new Exception(exceptionSubactionIDStr() + " tries to tie itself to an invalid Subaction index!");
                else if (thisSubactionSuccessTiedToSubactionAtIndex >= index) throw new Exception(exceptionSubactionIDStr() + " tries to tie itself to a Subaction index that doesn't precede it!");
            }
            return new BattleAction.Subaction(baseDamage, baseAccuracy, onSubactionHitTargetAnim, onSubactionExecuteUserAnim, atkStat, defStat, hitStat, evadeStat, 
                thisSubactionDamageTiedToSubactionAtIndex, thisSubactionSuccessTiedToSubactionAtIndex, fx, damageTypes);
        }

        /// <summary>
        /// Parses the XML node defining an FXPackage's parameters and spits out an FXPackage.
        /// </summary>
        private static BattleAction.Subaction.FXPackage XmlNodeToFXPackage (XmlNode fxNode, XmlNode workingNode, Func<string> exceptionFXPackageIDStr, int index)
        {
            float fxStrengthFloat = float.NaN;
            float fxLengthFloat = float.NaN;
            int fxStrengthInt = 0;
            int fxLengthInt = 0;
            int thisFXSuccessTiedToFXAtIndex = -1;
            Action<string> actOnNode = (node) =>
            {
                workingNode = fxNode.SelectSingleNode(node);
                if (workingNode == null) throw new Exception(exceptionFXPackageIDStr() + " has no node " + node);
            };
            actOnNode("//fxType");
            SubactionFXType fxType = ParseSubactionFXType(workingNode.InnerText);
            actOnNode("//fxHitStat");
            LogicalStatType fxHitStat = ParseLogicalStatType(workingNode.InnerText);
            actOnNode("//fxEvadeStat");
            LogicalStatType fxEvadeStat = ParseLogicalStatType(workingNode.InnerText);
            bool applyEvenIfSubactionMisses = true;
            workingNode = fxNode.SelectSingleNode("//applyEvenIfSubactionMisses");
            if (workingNode != null) applyEvenIfSubactionMisses = bool.Parse(workingNode.InnerText);
            actOnNode("//baseSuccessRate");
            float baseSuccessRate = float.Parse(workingNode.InnerText);
            if (baseSuccessRate > 1.0f) baseSuccessRate = 1.0f;
            else if (baseSuccessRate < 0.0f) baseSuccessRate = 0.0f;
            workingNode = fxNode.SelectSingleNode("//fxLength_Float");
            if (workingNode != null) fxLengthFloat = float.Parse(workingNode.InnerText);
            else
            {
                actOnNode("//fxLength_Int");
                fxLengthInt = int.Parse(workingNode.InnerText);
            }
            workingNode = fxNode.SelectSingleNode("//fxStrength_Float");
            if (workingNode != null) fxStrengthFloat = float.Parse(workingNode.InnerText);
            else
            {
                actOnNode("//fxStrength_Int");
                fxStrengthInt = int.Parse(workingNode.InnerText);
            }
            workingNode = fxNode.SelectSingleNode("//thisFXSuccessTiedToFXAtIndex");
            if (workingNode != null)
            {
                thisFXSuccessTiedToFXAtIndex = int.Parse(workingNode.InnerText);
                if (thisFXSuccessTiedToFXAtIndex < 0) throw new Exception(exceptionFXPackageIDStr() + " tries to tie itself to an invalid FX index!");
                else if (thisFXSuccessTiedToFXAtIndex >= index) throw new Exception(exceptionFXPackageIDStr() + " tries to tie itself to an FX index that doesn't precede it!");
            }
            return new BattleAction.Subaction.FXPackage(fxType, fxHitStat, fxEvadeStat, applyEvenIfSubactionMisses, baseSuccessRate, fxLengthFloat, fxStrengthFloat, fxLengthInt, fxStrengthInt, thisFXSuccessTiedToFXAtIndex);
        }

        /// <summary>
        /// Takes a string, spits out an ActionTargetType.
        /// </summary>
        private static ActionTargetType ParseActionTargetType (string s)
        {
            switch (s)
            {
                case "None":
                    return ActionTargetType.None;
                case "SingleTarget":
                    return ActionTargetType.SingleTarget;
                case "AllTargetsInRange":
                    return ActionTargetType.AllTargetsInRange;
                case "CircularAOE":
                    return ActionTargetType.CircularAOE;
                case "AllTargetsAlongLinearCorridor":
                    return ActionTargetType.AllTargetsAlongLinearCorridor;
                case "Self":
                    return ActionTargetType.Self;
                default:
                    throw new Exception(s + " either isn't a valid ActionTargetType entry, or hasn't been added to the parser yet.");
            }
        }

        /// <summary>
        /// Takes a string, spits out the corresponding AnimEventType.
        /// </summary>
        private static AnimEventType ParseAnimEventType (string s)
        {
            switch (s)
            {
                case "None":
                    return AnimEventType.None;
                case "TestAnim_OnHit":
                    return AnimEventType.TestAnim_OnHit;
                case "TestAnim_OnUse":
                    return AnimEventType.TestAnim_OnUse;
                case "TestStance_Idle":
                    return AnimEventType.TestStance_Idle;
                case "TestStance_Move":
                    return AnimEventType.TestStance_Move;
                case "TestStance_Hit":
                    return AnimEventType.TestStance_Hit;
                case "TestStance_Break":
                    return AnimEventType.TestStance_Break;
                case "TestStance_Dodge":
                    return AnimEventType.TestStance_Dodge;
                default:
                    throw new Exception(s + " either isn't a valid AnimEventType entry, or hasn't been added to the parser yet.");
            }
        }

        /// <summary>
        /// Takes a string, spits out the corresponding LogicalStatType.
        /// </summary>
        private static LogicalStatType ParseLogicalStatType (string s)
        {
            switch (s)
            {
                case "None":
                    return LogicalStatType.None;
                case "Stat_ATK":
                    return LogicalStatType.Stat_ATK;
                case "Stat_DEF":
                    return LogicalStatType.Stat_DEF;
                case "Stat_MATK":
                    return LogicalStatType.Stat_MATK;
                case "Stat_MDEF":
                    return LogicalStatType.Stat_MDEF;
                case "Stat_SPE":
                    return LogicalStatType.Stat_SPE;
                case "Stat_HIT":
                    return LogicalStatType.Stat_HIT;
                case "Stat_EVA":
                    return LogicalStatType.Stat_EVA;
                case "Stats_ATKDEF":
                    return LogicalStatType.Stats_ATKDEF;
                case "Stats_ATKMATK":
                    return LogicalStatType.Stats_ATKMATK;
                case "Stats_MATKMDEF":
                    return LogicalStatType.Stats_MATKMDEF;
                case "Stats_DEFMDEF":
                    return LogicalStatType.Stats_DEFMDEF;
                case "Stats_ATKSPE":
                    return LogicalStatType.Stats_ATKSPE;
                case "Stats_MATKSPE":
                    return LogicalStatType.Stats_MATKSPE;
                case "Stats_ATKHIT":
                    return LogicalStatType.Stats_ATKHIT;
                case "Stats_MATKHIT":
                    return LogicalStatType.Stats_MATKHIT;
                case "Stats_DEFEVA":
                    return LogicalStatType.Stats_DEFEVA;
                case "Stats_MDEFEVA":
                    return LogicalStatType.Stats_MDEFEVA;
                case "Stats_All":
                    return LogicalStatType.Stats_All;
                case "Stat_MaxHP":
                    return LogicalStatType.Stat_MaxHP;
                case "Stat_CurrentSP":
                    return LogicalStatType.Stat_CurrentSP;
                default:
                    throw new Exception(s + " either isn't a valid LogicalStatType entry, or hasn't been added to the parser yet.");
            }
        }

        /// <summary>
        /// Takes a string, spits out the corresponding SubactionFXType.
        /// </summary>
        private static SubactionFXType ParseSubactionFXType (string s)
        {
            switch (s)
            {
                case "PushTargetBackward":
                    return SubactionFXType.PushTargetBackward;
                case "Buff_STR":
                    return SubactionFXType.Buff_STR;
                default:
                    throw new Exception(s + " either isn't a valid SubactionFXType entry, or hasn't been added to the parser yet.");
            }
        }

        /// <summary>
        /// Takes an array of strings, spits out target side bitflags.
        /// </summary>
        private static TargetSideFlags ParseTargetSideFlags (string[] s)
        {
            TargetSideFlags result = TargetSideFlags.None;
            for (int i = 0; i < s.Length; i++)
            {
                switch (s[i])
                {
                    case "None":
                        return TargetSideFlags.None;
                    case "MySide":
                        result |= TargetSideFlags.MySide;
                        break;
                    case "MyFriends":
                        result |= TargetSideFlags.MyFriends;
                        break;
                    case "MyEnemies":
                        result |= TargetSideFlags.MyEnemies;
                        break;
                    case "Neutral":
                        result |= TargetSideFlags.Neutral;
                        break;
                    default:
                        throw new Exception(s[i] + " either isn't a valid TargetSideFlags entry, or hasn't been added to the parser yet.");
                }
            }
            return result;
        }

        /// <summary>
        /// Takes an array of strings, spits out damage type bitflags.
        /// </summary>
        private static DamageTypeFlags ParseDamageTypeFlags (string[] s)
        {
            DamageTypeFlags result = DamageTypeFlags.None;
            for (int i = 0; i < s.Length; i++)
            {
                switch (s[i])
                {
                    case "None":
                        return DamageTypeFlags.None;
                    case "Magic":
                        result |= DamageTypeFlags.Magic;
                        break;
                    case "Strike":
                        result |= DamageTypeFlags.Strike;
                        break;
                    case "Slash":
                        result |= DamageTypeFlags.Slash;
                        break;
                    case "Thrust":
                        result |= DamageTypeFlags.Thrust;
                        break;
                    case "Fire":
                        result |= DamageTypeFlags.Fire;
                        break;
                    case "Earth":
                        result |= DamageTypeFlags.Earth;
                        break;
                    case "Air":
                        result |= DamageTypeFlags.Air;
                        break;
                    case "Water":
                        result |= DamageTypeFlags.Water;
                        break;
                    case "Light":
                        result |= DamageTypeFlags.Light;
                        break;
                    case "Dark":
                        result |= DamageTypeFlags.Dark;
                        break;
                    case "Bio":
                        result |= DamageTypeFlags.Bio;
                        break;
                    case "Psyche":
                        result |= DamageTypeFlags.Psyche;
                        break;
                    case "Sound":
                        result |= DamageTypeFlags.Sound;
                        break;
                    case "Reality":
                        result |= DamageTypeFlags.Reality;
                        break;
                    case "Time":
                        result |= DamageTypeFlags.Time;
                        break;
                    case "Space":
                        result |= DamageTypeFlags.Space;
                        break;
                    case "Electric":
                        result |= DamageTypeFlags.Electric;
                        break;
                    case "Ice":
                        result |= DamageTypeFlags.Ice;
                        break;
                    case "Spirit":
                        result |= DamageTypeFlags.Spirit;
                        break;
                    default:
                        throw new Exception(s[i] + " either isn't a valid DamageTypeFlags entry, or hasn't been added to the parser yet.");
                }
            }
            return result;
        }

        /// <summary>
        /// Gets a BattleAction corresponding to actionID from the dataset.
        /// </summary>
        public static BattleAction GetAction (ActionType actionID)
        {
            if (_actions == null) LoadActions();
            return _actions[(int)actionID];
        }

        /// <summary>
        /// Gets a BattleStance corresponding to stanceID from the dataset.
        /// </summary>
        public static BattleStance GetStance (StanceType stanceID)
        {
            if (_stances == null) LoadStances();
            return _stances[(int)stanceID];
        }
    }
}
