namespace CnfBattleSys
{
    /// <summary>
    /// Data storage class that stores raw base stats, etc. for the unit dataset.
    /// Battler objects work by copying their own state out of the global
    /// BattlerData instance you point them at. We just build a table out of these
    /// things and say "you are thing" and then Battler becomes thing.
    /// </summary>
    public class BattlerData
    {
        public readonly bool isFixedStats; // if this is true, we use our base stats directly and level doesn't matter; if false, we calculate our stats based on those values.
        public readonly BattlerAIType aiType;
        public readonly BattlerAIFlags aiFlags;
        public readonly int level;
        public readonly float size;
        public readonly float stepTime;
        public readonly float yOffset;
        public readonly BattlerModelType modelType;
        public readonly BattleStance[] stances;
        public readonly BattleStance metaStance;
        public readonly int baseHP;
        public readonly int baseATK;
        public readonly int baseDEF;
        public readonly int baseMATK;
        public readonly int baseMDEF;
        public readonly int baseSPE;
        public readonly int baseHIT;
        public readonly int baseEVA;
        public readonly Battler.Resistances_Raw resistances;
    }
}