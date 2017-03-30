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
        /// <summary>
        /// Struct that stores growths for a specific BattlerData instance.
        /// </summary>
        public struct Growths
        {
            public readonly float HP;
            public readonly float ATK;
            public readonly float DEF;
            public readonly float MATK;
            public readonly float MDEF;
            public readonly float SPE;
            public readonly float EVA;
            public readonly float HIT;

            /// <summary>
            /// Constructor. Only called by BattlerDatabase.
            /// </summary>
            public Growths (float _hp, float _atk, float _def, float _matk, float _mdef, float _spe, float _eva, float _hit)
            {
                HP = _hp;
                ATK = _atk;
                DEF = _def;
                MATK = _matk;
                MDEF = _mdef;
                SPE = _spe;
                EVA = _eva;
                HIT = _hit;
            }
        }

        public readonly BattlerType battlerType;
        public readonly bool isFixedStats; // if this is true, we use our base stats directly and level doesn't matter; if false, we calculate our stats based on those values.
        public readonly BattlerAIType aiType;
        public readonly BattlerAIFlags aiFlags;
        public readonly byte level;
        public readonly float size;
        public readonly float stepTime;
        public readonly float yOffset;
        public readonly BattlerModelType modelType;
        public readonly BattleStance[] stances;
        public readonly BattleStance metaStance;
        public readonly int baseHP;
        public readonly ushort baseATK;
        public readonly ushort baseDEF;
        public readonly ushort baseMATK;
        public readonly ushort baseMDEF;
        public readonly ushort baseSPE;
        public readonly ushort baseHIT;
        public readonly ushort baseEVA;
        public readonly float baseMoveDist;
        public readonly float baseMoveDelay;
        public readonly Growths growths;
        public readonly Battler.Resistances_Raw resistances;

        /// <summary>
        /// Constructor. Should only ever be called by BattlerDatabase.ImportUnitDefWithID()
        /// </summary>
        public BattlerData (BattlerType _battlerType, bool _isFixedStats, BattlerAIType _aiType, BattlerAIFlags _aiFlags, byte _level, float _size, float _stepTime, float _yOffset, BattlerModelType _modelType, 
            BattleStance[] _stances, BattleStance _metaStance, int _baseHP, ushort _baseATK, ushort _baseDEF, ushort _baseMATK, ushort _baseMDEF, ushort _baseSPE, ushort _baseHIT, ushort _baseEVA, 
            float _baseMoveDist, float _baseMoveDelay, Growths _growths, Battler.Resistances_Raw _resistances)
        {
            battlerType = _battlerType;
            isFixedStats = _isFixedStats;
            aiType = _aiType;
            aiFlags = _aiFlags;
            level = _level;
            size = _size;
            stepTime = _stepTime;
            yOffset = _yOffset;
            modelType = _modelType;
            stances = _stances;
            metaStance = _metaStance;
            baseHP = _baseHP;
            baseATK = _baseATK;
            baseDEF = _baseDEF;
            baseMATK = _baseMATK;
            baseMDEF = _baseMDEF;
            baseSPE = _baseSPE;
            baseHIT = _baseHIT;
            baseEVA = _baseEVA;
            baseMoveDist = _baseMoveDist;
            baseMoveDelay = _baseMoveDelay;
            growths = _growths;
            resistances = _resistances;
        }
    }
}