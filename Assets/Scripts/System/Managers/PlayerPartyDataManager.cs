using UnityEngine;
using CnfBattleSys;
using Universe;

/// <summary>
/// Actually: let's hold off on this for a minute.
/// </summary>
public class PlayerPartyDataManager : Manager<PlayerPartyDataManager>
{
    /// <summary>
    /// PODS that stores the data we build the party formation from.
    /// </summary>
    public class PartyMember
    {
        public BattlerType battlerType { get; private set; }
        public int level { get; private set; }
        public int exp { get; private set; }
        public StanceType[] allStances { get; private set; }
        public StanceType[] setStances { get; private set; }
        public StanceType metaStance { get; private set; }
        public ActionType[][] allActionsForStances { get; private set; }
        public ActionType[][] setActionsForStances { get; private set; }

        public PartyMember (BattlerType _battlerType, int _level, int _exp, StanceType[] _allStances, StanceType[] _setStances, StanceType _metaStance, ActionType[][] _allActionsForStances, ActionType[][] _setActionsForStances)
        {
            battlerType = _battlerType;
            level = _level;
            exp = _exp;
            allStances = _allStances;
            setStances = _setStances;
            metaStance = _metaStance;
            allActionsForStances = _allActionsForStances;
            setActionsForStances = _setActionsForStances;
        }
    }

    const int numberOfActivePartyMembers = 4;
    const int numberOfPartyMembers = 7;
    public BattleFormation.FormationMember[] partyFormation { get; private set; }
    public PartyMember[] partyMembers { get; private set; }
    readonly static int[] partyMemberStartingLevels = { 1, 2, 3, 4, 5, 6, 7 };
    readonly static BattlerType[] partyMemberSpecies = { BattlerType.TestPCUnit, BattlerType.TestPCUnit, BattlerType.TestPCUnit, BattlerType.TestPCUnit, BattlerType.TestPCUnit, BattlerType.TestPCUnit, BattlerType.TestPCUnit };
    // what do I actually need to store?
    // levels/exp/stance sets/action sets/(evenutally) gear or we
    // build a formation at runtime, then build battlers from that

    /// <summary>
    /// MonoBehaviour.Awake ()
    /// </summary>
    void Awake ()
    {
        return;
        partyFormation = new BattleFormation.FormationMember[numberOfActivePartyMembers];
        partyMembers = new PartyMember[numberOfPartyMembers];
        for (int i = 0; i < partyMembers.Length; i++)
        {
            // We're not worried about doing party member shit "right" atm, so this is really hacky and just massages the battler data entries into the party member structures
            BattleStance[] stances = BattlerDatabase.Get(partyMemberSpecies[i]).stances;
            StanceType[] stanceTypes = new StanceType[stances.Length];
            for (int s = 0; s < stances.Length; s++) stanceTypes[s] = stances[s].stanceID;
            //partyMembers[i] = new PartyMember(partyMemberSpecies[i], partyMemberStartingLevels[i], GetExpForLevel(partyMemberStartingLevels[i]), stanceTypes, stanceTypes, StanceType.None,)
        }
    }
    
    /// <summary>
    /// Eventually this should actually run lv through an experience curve formula, but right now it just spits that right back out.
    /// </summary>
    static int GetExpForLevel (int lv)
    {
        return lv;
    }
}
