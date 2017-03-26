using UnityEngine;
using System;

namespace CnfBattleSys
{
    /// <summary>
    /// Object representing a single in-battle unit.
    /// Battle logic is totally decoupled from presentation - 
    /// the MonoBehaviours create and manipulate Battler objects, which do
    /// all the crunchy stuff.
    /// </summary>
    public class Battler
    {
        public const int maxLevel = 120; // you could actually have things over maxLevel but we use this for scaling stats w/ level...

        /// <summary>
        /// Data structure for Battler stats.
        /// Stores base stats, does transformations on those to yield final values.
        /// </summary>
        public struct Stats
        {
            private Battler owner;
            public readonly int baseHP;
            public readonly int baseAtk;
            public readonly int baseDef;
            public readonly int baseMAtk;
            public readonly int baseMDef;
            public readonly int baseSpe;
            public readonly int baseHit;
            public readonly int baseEva;
            public readonly float baseMoveDist;
            public readonly float baseMoveDelay;
            public int maxHP { get { return flooredIntStat(baseHP, totalBonus_maxHP, totalMulti_maxHP); } }
            public int Atk { get { return flooredIntStat(baseAtk, totalBonus_Atk, totalMulti_Atk); } }
            public int Def { get { return flooredIntStat(baseDef, totalBonus_Def, totalMulti_Def); } }
            public int MAtk { get { return flooredIntStat(baseMAtk, totalBonus_MAtk, totalMulti_MAtk); } }
            public int MDef { get { return flooredIntStat(baseMDef, totalBonus_MDef, totalMulti_MDef); } }
            public int Spe { get { return flooredIntStat(baseSpe, totalBonus_Spe, totalMulti_Spe); } }
            public int Hit { get { return flooredIntStat(baseHit, totalBonus_Hit, totalMulti_Hit); } }
            public int Eva { get { return flooredIntStat(baseEva, totalBonus_Eva, totalMulti_Eva); } }
            public float moveDist { get { return flooredFloatStat(baseMoveDist, totalBonus_moveDist, totalMulti_moveDist); } }
            public float moveDelay { get { return flooredFloatStat(baseMoveDelay, totalBonus_moveDelay, totalMulti_moveDelay); } }
            public int totalBonus_maxHP { get { return owner.currentStance.statBonus_MaxHP + owner.metaStance.statBonus_MaxHP + GetBonus_MaxHP(); } }
            public int totalBonus_Atk { get { return owner.currentStance.statBonus_ATK + owner.metaStance.statBonus_ATK + GetBonus_Atk(); } }
            public int totalBonus_Def { get { return owner.currentStance.statBonus_DEF + owner.metaStance.statBonus_DEF + GetBonus_Def(); } }
            public int totalBonus_MAtk { get { return owner.currentStance.statBonus_MATK + owner.metaStance.statBonus_MATK + GetBonus_MAtk(); } }
            public int totalBonus_MDef { get { return owner.currentStance.statBonus_MDEF + owner.metaStance.statBonus_MDEF + GetBonus_MDef(); } }
            public int totalBonus_Spe { get { return owner.currentStance.statBonus_SPE + owner.metaStance.statBonus_SPE + GetBonus_Spe(); } }
            public int totalBonus_Hit { get { return owner.currentStance.statBonus_HIT + owner.metaStance.statBonus_HIT + GetBonus_Hit(); } }
            public int totalBonus_Eva { get { return owner.currentStance.statBonus_EVA + owner.metaStance.statBonus_EVA + GetBonus_Eva(); } }
            public float totalBonus_moveDist { get { return owner.currentStance.moveDistBonus + owner.metaStance.moveDistBonus + GetBonus_MoveDist(); } }
            public float totalBonus_moveDelay { get { return owner.currentStance.moveDelayBonus + owner.metaStance.moveDelayBonus + GetBonus_MoveDelay(); } }
            public float totalMulti_maxHP { get { return owner.currentStance.statMultiplier_MaxHP * owner.metaStance.statMultiplier_MaxHP * GetMultiplier_MaxHP(); } }
            public float totalMulti_Atk { get { return owner.currentStance.statMultiplier_ATK * owner.metaStance.statMultiplier_ATK * GetMultiplier_Atk(); } }
            public float totalMulti_Def { get { return owner.currentStance.statMultiplier_DEF * owner.metaStance.statMultiplier_DEF * GetMultiplier_Def(); } }
            public float totalMulti_MAtk { get { return owner.currentStance.statMultiplier_MATK * owner.metaStance.statMultiplier_MATK * GetMultiplier_MAtk(); } }
            public float totalMulti_MDef { get { return owner.currentStance.statMultiplier_MDEF * owner.metaStance.statMultiplier_MDEF * GetMultiplier_MDef(); } }
            public float totalMulti_Spe { get { return owner.currentStance.statMultiplier_SPE * owner.metaStance.statMultiplier_SPE * GetMultiplier_Spe(); } }
            public float totalMulti_Hit { get { return owner.currentStance.statMultiplier_HIT * owner.metaStance.statMultiplier_HIT * GetMultiplier_Hit(); } }
            public float totalMulti_Eva { get { return owner.currentStance.statMultiplier_EVA * owner.metaStance.statMultiplier_EVA * GetMultiplier_Eva(); } }
            public float totalMulti_moveDist { get { return owner.currentStance.moveDistMultiplier * owner.metaStance.moveDistMultiplier * GetMultiplier_MoveDist(); } }
            public float totalMulti_moveDelay { get { return owner.currentStance.moveDelayMultiplier * owner.metaStance.moveDelayMultiplier * GetMultiplier_MoveDelay(); } }

            /// <summary>
            /// Constructor for Stats. Should never be called by anything but Battler.
            /// </summary>
            public Stats (Battler _owner, int _baseHP, int _baseAtk, int _baseDef, int _baseMAtk, int _baseMDef, int _baseSpe, int _baseHit, int _baseEva, float _baseMoveDist, float _baseMoveDelay)
            {
                owner = _owner;
                baseHP = _baseHP;
                baseAtk = _baseAtk;
                baseDef = _baseDef;
                baseMAtk = _baseMAtk;
                baseMDef = _baseMDef;
                baseSpe = _baseSpe;
                baseHit = _baseHit;
                baseEva = _baseEva;
                baseMoveDist = _baseMoveDist;
                baseMoveDelay = _baseMoveDelay;
            }

            /// <summary>
            /// Totals and sanity-checks a stat used as an int with minimum value of 1
            /// </summary>
            private int flooredIntStat (int baseStat, int bonus, float multi)
            {
                int pv = Mathf.RoundToInt((baseStat + bonus) * multi);
                if (pv < 1) pv = 1;
                return pv;
            }

            /// <summary>
            /// Totals and sanity-checks a stat used as a float with a minimum value of 0
            /// </summary>
            private float flooredFloatStat (float baseStat, float bonus, float multi)
            {
                float pv = (baseStat + bonus) * multi;
                if (pv < 0) pv = 0;
                return pv;
            }

            /// <summary>
            /// Sums the positive/negative bonus points in max HP for all StatusPackets attached to this Battler.
            /// Except StatusPackets don't exist yet, so lol.
            /// </summary>
            public int GetBonus_MaxHP()
            {
                return 0;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket bonus points for Attack.
            /// </summary>
            public int GetBonus_Atk()
            {
                return 0;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket bonus points for Defense.
            /// </summary>
            public int GetBonus_Def()
            {
                return 0;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket bonus points for MAttack.
            /// </summary>
            public int GetBonus_MAtk()
            {
                return 0;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket bonus points for MDefense.
            /// </summary>
            public int GetBonus_MDef()
            {
                return 0;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket bonus points for Speed.
            /// </summary>
            public int GetBonus_Spe()
            {
                return 0;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket bonus points for Hit.
            /// </summary>
            public int GetBonus_Hit()
            {
                return 0;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket bonus points for Evade.
            /// </summary>
            public int GetBonus_Eva()
            {
                return 0;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket bonus move distance
            /// </summary>
            public float GetBonus_MoveDist()
            {
                return 0;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket bonus move delay
            /// </summary>
            /// <returns></returns>
            public float GetBonus_MoveDelay()
            {
                return 0;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket multipliers for max HP.
            /// </summary>
            public float GetMultiplier_MaxHP()
            {
                return 1;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket multipliers for Attack.
            /// </summary>
            public float GetMultiplier_Atk()
            {
                return 1;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket multipliers for Defense.
            /// </summary>
            public float GetMultiplier_Def()
            {
                return 1;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket multipliers for MAttack
            /// </summary>
            public float GetMultiplier_MAtk()
            {
                return 1;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket multipliers for MDefense
            /// </summary>
            public float GetMultiplier_MDef()
            {
                return 1;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket multipliers for Speed
            /// </summary>
            public float GetMultiplier_Spe()
            {
                return 1;
            }


            /// <summary>
            /// Sums positive/negative StatusPacket multipliers for Hit
            /// </summary>
            public float GetMultiplier_Hit()
            {
                return 1;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket multipliers for Evade
            /// </summary>
            public float GetMultiplier_Eva()
            {
                return 1;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket multipliers for move distance
            /// </summary>
            public float GetMultiplier_MoveDist()
            {
                return 1;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket multipliers for move delay
            /// </summary>
            public float GetMultiplier_MoveDelay()
            {
                return 1;
            }
        }

        /// <summary>
        /// Data structure for elemental resistance/weakness modifiers.
        /// </summary>
        public struct Resistances_Raw
        {
            public readonly float global;
            public readonly float magic;
            public readonly float strike;
            public readonly float slash;
            public readonly float thrust;
            public readonly float fire;
            public readonly float earth;
            public readonly float air;
            public readonly float water;
            public readonly float light;
            public readonly float dark;
            public readonly float bio;
            public readonly float sound;
            public readonly float psyche;
            public readonly float reality;
            public readonly float time;
            public readonly float space;
            public readonly float electric;
            public readonly float ice;
            public readonly float spirit;

            /// <summary>
            /// This can be called by Battler and StanceDatasets. 
            /// Should never be called anywhere else.
            /// </summary>
            public Resistances_Raw (float _global, float _magic, float _strike, float _slash, float _thrust, float _fire, float _earth, float _air, float _water, float _light,
                float _dark, float _bio, float _sound, float _psyche, float _reality, float _time, float _space, float _electric, float _ice, float _spirit)
            {
                global = _global;
                magic = _magic;
                strike = _strike;
                slash = _slash;
                thrust = _thrust;
                fire = _fire;
                earth = _earth;
                air = _air;
                water = _water;
                light = _light;
                dark = _dark;
                bio = _bio;
                sound = _sound;
                psyche = _psyche;
                reality = _reality;
                time = _time;
                space = _space;
                electric = _electric;
                ice = _ice;
                spirit = _spirit;
            }
        }

        /// <summary>
        /// Data structure for Battler resistance/weakness mods. Stores raw values and does transformations on those.
        /// </summary>
        public struct Resistances
        {
            public readonly Resistances_Raw raw;
            public readonly Battler owner;

            public float global { get { return raw.global * owner.currentStance.resistances.global * owner.metaStance.resistances.global * GetMulti_Global(); } }
            public float magic { get { return global * raw.magic * owner.currentStance.resistances.magic * owner.metaStance.resistances.magic * GetMulti_Magic(); } }
            public float strike { get { return global * raw.strike * owner.currentStance.resistances.strike * owner.metaStance.resistances.strike * GetMulti_Strike(); } }
            public float slash { get { return global * raw.slash * owner.currentStance.resistances.slash * owner.metaStance.resistances.slash * GetMulti_Slash(); } }
            public float thrust { get { return global * raw.thrust * owner.currentStance.resistances.thrust * owner.metaStance.resistances.thrust * GetMulti_Thrust(); } }
            public float fire { get { return global * raw.fire * owner.currentStance.resistances.fire * owner.metaStance.resistances.fire * GetMulti_Fire(); } }
            public float earth { get { return global * raw.earth * owner.currentStance.resistances.earth * owner.metaStance.resistances.earth * GetMulti_Earth(); } }
            public float air { get { return global * raw.air * owner.currentStance.resistances.air * owner.metaStance.resistances.air * GetMulti_Air(); } }
            public float water { get { return global * raw.water * owner.currentStance.resistances.water * owner.metaStance.resistances.water * GetMulti_Water(); } }
            public float light { get { return global * raw.light * owner.currentStance.resistances.light * owner.metaStance.resistances.light * GetMulti_Light(); } }
            public float dark { get { return global * raw.dark * owner.currentStance.resistances.dark * owner.metaStance.resistances.dark * GetMulti_Dark(); } }
            public float bio { get { return global * raw.bio * owner.currentStance.resistances.bio * owner.metaStance.resistances.bio * GetMulti_Bio(); } }
            public float sound { get { return global * raw.sound * owner.currentStance.resistances.sound * owner.metaStance.resistances.sound * GetMulti_Sound(); } }
            public float psyche { get { return global * raw.psyche * owner.currentStance.resistances.psyche * owner.metaStance.resistances.psyche * GetMulti_Psyche(); } }
            public float reality { get { return global * raw.reality * owner.currentStance.resistances.reality * owner.metaStance.resistances.reality * GetMulti_Reality(); } }
            public float time { get { return global * raw.time * owner.currentStance.resistances.time * owner.metaStance.resistances.time * GetMulti_Time(); } }
            public float space { get { return global * raw.space * owner.currentStance.resistances.space * owner.metaStance.resistances.space * GetMulti_Space(); } }
            public float electric { get { return global * raw.electric * owner.currentStance.resistances.electric * owner.metaStance.resistances.electric * GetMulti_Electric(); } }
            public float ice { get { return global * raw.ice * owner.currentStance.resistances.ice * owner.metaStance.resistances.ice * GetMulti_Ice(); } }
            public float spirit { get { return global * raw.spirit * owner.currentStance.resistances.spirit * owner.metaStance.resistances.spirit * GetMulti_Spirit(); } }

            /// <summary>
            /// Constructor. Only Battler should call this. (and only within its own constructor, too)
            /// </summary>
            public Resistances (Resistances_Raw _raw, Battler _owner)
            {
                raw = _raw;
                owner = _owner;
            }

            /// <summary>
            /// Sums StatusPacket multis for global resistance.
            /// </summary>
            private float GetMulti_Global ()
            {
                return 1;
            }

            /// <summary>
            /// Sums StatusPacket multis for magic resistance
            /// </summary>
            private float GetMulti_Magic ()
            {
                return 1;
            }

            /// <summary>
            /// Sums StatusPacket multis for strike resistance
            /// </summary>
            private float GetMulti_Strike ()
            {
                return 1;
            }

            /// <summary>
            /// Sums StatusPacket multis for slash resistance
            /// </summary>
            private float GetMulti_Slash ()
            {
                return 1;
            }

            /// <summary>
            /// Sums StatusPacket multis for thrust resistance
            /// </summary>
            private float GetMulti_Thrust ()
            {
                return 1;
            }

            /// <summary>
            /// Sums StatusPacket multis for fire resistance
            /// </summary>
            private float GetMulti_Fire ()
            {
                return 1;
            }

            /// <summary>
            /// Sums StatusPacket multis for earth resistance
            /// </summary>
            private float GetMulti_Earth()
            {
                return 1;
            }

            /// <summary>
            /// Sums StatusPacket multis for Air resistance
            /// </summary>
            private float GetMulti_Air()
            {
                return 1;
            }

            /// <summary>
            /// Sums StatusPacket multis for water resistance
            /// </summary>
            private float GetMulti_Water()
            {
                return 1;
            }

            /// <summary>
            /// Sums StatusPacket multis for light resistance
            /// </summary>
            private float GetMulti_Light()
            {
                return 1;
            }

            /// <summary>
            /// Sums StatusPacket multis for dark resistance
            /// </summary>
            private float GetMulti_Dark()
            {
                return 1;
            }

            /// <summary>
            /// Sums StatusPacket multis for bio resistance
            /// </summary>
            private float GetMulti_Bio()
            {
                return 1;
            }

            /// <summary>
            /// Sums StatusPacket multis for sound resistance
            /// </summary>
            private float GetMulti_Sound()
            {
                return 1;
            }

            /// <summary>
            /// Sums StatusPacket multis for psyche resistance
            /// </summary>
            private float GetMulti_Psyche()
            {
                return 1;
            }

            /// <summary>
            /// Sums StatusPacket multis for reality resistance
            /// </summary>
            private float GetMulti_Reality()
            {
                return 1;
            }

            /// <summary>
            /// Sums StatusPacket multis for Time resistance
            /// </summary>
            private float GetMulti_Time()
            {
                return 1;
            }

            /// <summary>
            /// Sums StatusPacket multis for space resistance
            /// </summary>
            private float GetMulti_Space()
            {
                return 1;
            }

            /// <summary>
            /// Sums StatusPacket multis for electric resistance
            /// </summary>
            private float GetMulti_Electric()
            {
                return 1;
            }

            /// <summary>
            /// Sums StatusPacket multis for ice resistance
            /// </summary>
            private float GetMulti_Ice()
            {
                return 1;
            }

            /// <summary>
            /// Sums StatusPacket multis for spirit resistance
            /// </summary>
            private float GetMulti_Spirit()
            {
                return 1;
            }

        }

        public readonly BattlerType battlerType;
        public readonly Stats stats;

        public readonly BattleStance[] stances;
        public BattleStance currentStance { get; private set; }
        public BattleStance metaStance { get; private set; } // a second stance that goes "on top" of the main one adding actions/bonus/multis
        public BattlerSideFlags side { get; private set; } // This is a bitflag, but it should never do bitflaggy things here because that'd be weird.
        public Vector3 logicalPosition { get; private set; } // This is what we use for determining targeting ranges, etc. When we get a move action, we set logicalPosition immediately, and the puppet moves there. z-axis doesn't matter - battlefield is 2D.

        public float currentDelay { get; private set; }
        public int currentHP { get; private set; }
        public int currentSP { get; private set; }
        public int level { get; private set; }


        /// <summary>
        /// Given base stat, level, and growth bias, calculates stat at level.
        /// </summary>
        /// <param name="baseStat">Base stat value. Approximately = stat at level 120.</param>
        /// <param name="level">I ain't gonna spell this out for you.</param>
        /// <param name="growth">Growth factor. Normally less than 1. Applies a multiplier equal to growth at level 1 and 1 at level 120. 
        /// Lower values cause steeper slopes, as more of the stat gains are loaded into the later levels.</param>
        public static int CalculateStat (int baseStat, int level, float growth)
        {
            const float mlv = maxLevel; // implicit divide-as-float
            const int adjustment = 30;
            return Mathf.RoundToInt(baseStat * ((level + adjustment) / mlv) * (growth + ((level / mlv) * (1 - growth))));
        }

    }

}