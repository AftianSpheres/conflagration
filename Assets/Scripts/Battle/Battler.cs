using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
            public readonly int baseATK;
            public readonly int baseDEF;
            public readonly int baseMATK;
            public readonly int baseMDEF;
            public readonly int baseSpe;
            public readonly int baseHit;
            public readonly int baseEVA;
            public readonly float baseMoveDist;
            public readonly float baseMoveDelay;
            public int maxHP { get { return flooredIntStat(baseHP, totalBonus_maxHP, totalMulti_maxHP); } }
            public int ATK { get { return flooredIntStat(baseATK, totalBonus_ATK, totalMulti_ATK); } }
            public int DEF { get { return flooredIntStat(baseDEF, totalBonus_DEF, totalMulti_DEF); } }
            public int MATK { get { return flooredIntStat(baseMATK, totalBonus_MATK, totalMulti_MATK); } }
            public int MDEF { get { return flooredIntStat(baseMDEF, totalBonus_MDEF, totalMulti_MDEF); } }
            public int Spe { get { return flooredIntStat(baseSpe, totalBonus_Spe, totalMulti_Spe); } }
            public int Hit { get { return flooredIntStat(baseHit, totalBonus_Hit, totalMulti_Hit); } }
            public int EVA { get { return flooredIntStat(baseEVA, totalBonus_EVA, totalMulti_EVA); } }
            public float moveDist { get { return flooredFloatStat(baseMoveDist, totalBonus_moveDist, totalMulti_moveDist); } }
            public float moveDelay { get { return flooredFloatStat(baseMoveDelay, totalBonus_moveDelay, totalMulti_moveDelay); } }
            public int totalBonus_maxHP { get { return owner.currentStance.statBonus_MaxHP + owner.metaStance.statBonus_MaxHP + GetBonus_MaxHP(); } }
            public int totalBonus_ATK { get { return owner.currentStance.statBonus_ATK + owner.metaStance.statBonus_ATK + GetBonus_ATK(); } }
            public int totalBonus_DEF { get { return owner.currentStance.statBonus_DEF + owner.metaStance.statBonus_DEF + GetBonus_DEF(); } }
            public int totalBonus_MATK { get { return owner.currentStance.statBonus_MATK + owner.metaStance.statBonus_MATK + GetBonus_MATK(); } }
            public int totalBonus_MDEF { get { return owner.currentStance.statBonus_MDEF + owner.metaStance.statBonus_MDEF + GetBonus_MDEF(); } }
            public int totalBonus_Spe { get { return owner.currentStance.statBonus_SPE + owner.metaStance.statBonus_SPE + GetBonus_SPE(); } }
            public int totalBonus_Hit { get { return owner.currentStance.statBonus_HIT + owner.metaStance.statBonus_HIT + GetBonus_HIT(); } }
            public int totalBonus_EVA { get { return owner.currentStance.statBonus_EVA + owner.metaStance.statBonus_EVA + GetBonus_EVA(); } }
            public float totalBonus_moveDist { get { return owner.currentStance.moveDistBonus + owner.metaStance.moveDistBonus + GetBonus_MoveDist(); } }
            public float totalBonus_moveDelay { get { return owner.currentStance.moveDelayBonus + owner.metaStance.moveDelayBonus + GetBonus_MoveDelay(); } }
            public float totalMulti_maxHP { get { return owner.currentStance.statMultiplier_MaxHP * owner.metaStance.statMultiplier_MaxHP * GetMultiplier_MaxHP(); } }
            public float totalMulti_ATK { get { return owner.currentStance.statMultiplier_ATK * owner.metaStance.statMultiplier_ATK * GetMultiplier_ATK(); } }
            public float totalMulti_DEF { get { return owner.currentStance.statMultiplier_DEF * owner.metaStance.statMultiplier_DEF * GetMultiplier_DEF(); } }
            public float totalMulti_MATK { get { return owner.currentStance.statMultiplier_MATK * owner.metaStance.statMultiplier_MATK * GetMultiplier_MATK(); } }
            public float totalMulti_MDEF { get { return owner.currentStance.statMultiplier_MDEF * owner.metaStance.statMultiplier_MDEF * GetMultiplier_MDEF(); } }
            public float totalMulti_Spe { get { return owner.currentStance.statMultiplier_SPE * owner.metaStance.statMultiplier_SPE * GetMultiplier_SPE(); } }
            public float totalMulti_Hit { get { return owner.currentStance.statMultiplier_HIT * owner.metaStance.statMultiplier_HIT * GetMultiplier_HIT(); } }
            public float totalMulti_EVA { get { return owner.currentStance.statMultiplier_EVA * owner.metaStance.statMultiplier_EVA * GetMultiplier_EVA(); } }
            public float totalMulti_moveDist { get { return owner.currentStance.moveDistMultiplier * owner.metaStance.moveDistMultiplier * GetMultiplier_MoveDist(); } }
            public float totalMulti_moveDelay { get { return owner.currentStance.moveDelayMultiplier * owner.metaStance.moveDelayMultiplier * GetMultiplier_MoveDelay(); } }

            /// <summary>
            /// Constructor for Stats. Should never be called by anything but Battler.
            /// </summary>
            public Stats (Battler _owner, int _baseHP, int _baseATK, int _baseDEF, int _baseMATK, int _baseMDEF, int _baseSpe, int _baseHit, int _baseEVA, float _baseMoveDist, float _baseMoveDelay)
            {
                owner = _owner;
                baseHP = _baseHP;
                baseATK = _baseATK;
                baseDEF = _baseDEF;
                baseMATK = _baseMATK;
                baseMDEF = _baseMDEF;
                baseSpe = _baseSpe;
                baseHit = _baseHit;
                baseEVA = _baseEVA;
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
            /// </summary>
            public int GetBonus_MaxHP()
            {
                int r = 0;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r += owner.statusPackets[keys[i]].statBonus_MaxHP;
                }
                return r;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket bonus points for Attack.
            /// </summary>
            public int GetBonus_ATK()
            {
                int r = 0;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r += owner.statusPackets[keys[i]].statBonus_ATK;
                }
                return r;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket bonus points for Defense.
            /// </summary>
            public int GetBonus_DEF()
            {
                int r = 0;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r += owner.statusPackets[keys[i]].statBonus_DEF;
                }
                return r;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket bonus points for MAttack.
            /// </summary>
            public int GetBonus_MATK()
            {
                int r = 0;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r += owner.statusPackets[keys[i]].statBonus_MATK;
                }
                return r;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket bonus points for MDefense.
            /// </summary>
            public int GetBonus_MDEF()
            {
                int r = 0;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r += owner.statusPackets[keys[i]].statBonus_MDEF;
                }
                return r;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket bonus points for Speed.
            /// </summary>
            public int GetBonus_SPE()
            {
                int r = 0;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r += owner.statusPackets[keys[i]].statBonus_SPE;
                }
                return r;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket bonus points for Hit.
            /// </summary>
            public int GetBonus_HIT()
            {
                int r = 0;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r += owner.statusPackets[keys[i]].statBonus_HIT;
                }
                return r;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket bonus points for EVAde.
            /// </summary>
            public int GetBonus_EVA()
            {
                int r = 0;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r += owner.statusPackets[keys[i]].statBonus_EVA;
                }
                return r;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket bonus move distance
            /// </summary>
            public float GetBonus_MoveDist()
            {
                float r = 0;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r += owner.statusPackets[keys[i]].statBonus_MoveDist;
                }
                return r;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket bonus move delay
            /// </summary>
            /// <returns></returns>
            public float GetBonus_MoveDelay()
            {
                float r = 0;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r += owner.statusPackets[keys[i]].statBonus_MoveDelay;
                }
                return r;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket multipliers for max HP.
            /// </summary>
            public float GetMultiplier_MaxHP()
            {
                float r = 1;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r *= owner.statusPackets[keys[i]].statMulti_MaxHP;
                }
                return r;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket multipliers for Attack.
            /// </summary>
            public float GetMultiplier_ATK()
            {
                float r = 1;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r *= owner.statusPackets[keys[i]].statMulti_ATK;
                }
                return r;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket multipliers for Defense.
            /// </summary>
            public float GetMultiplier_DEF()
            {
                float r = 1;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r *= owner.statusPackets[keys[i]].statMulti_DEF;
                }
                return r;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket multipliers for MAttack
            /// </summary>
            public float GetMultiplier_MATK()
            {
                float r = 1;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r *= owner.statusPackets[keys[i]].statMulti_MATK;
                }
                return r;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket multipliers for MDefense
            /// </summary>
            public float GetMultiplier_MDEF()
            {
                float r = 1;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r *= owner.statusPackets[keys[i]].statMulti_MDEF;
                }
                return r;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket multipliers for Speed
            /// </summary>
            public float GetMultiplier_SPE()
            {
                float r = 1;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r *= owner.statusPackets[keys[i]].statMulti_SPE;
                }
                return r;
            }


            /// <summary>
            /// Sums positive/negative StatusPacket multipliers for Hit
            /// </summary>
            public float GetMultiplier_HIT()
            {
                float r = 1;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r *= owner.statusPackets[keys[i]].statMulti_HIT;
                }
                return r;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket multipliers for EVAde
            /// </summary>
            public float GetMultiplier_EVA()
            {
                float r = 1;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r *= owner.statusPackets[keys[i]].statMulti_EVA;
                }
                return r;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket multipliers for move distance
            /// </summary>
            public float GetMultiplier_MoveDist()
            {
                float r = 1;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r *= owner.statusPackets[keys[i]].statMulti_MoveDist;
                }
                return r;
            }

            /// <summary>
            /// Sums positive/negative StatusPacket multipliers for move delay
            /// </summary>
            public float GetMultiplier_MoveDelay()
            {
                float r = 1;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r *= owner.statusPackets[keys[i]].statMulti_MoveDelay;
                }
                return r;
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
                float r = 1;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r *= owner.statusPackets[keys[i]].resistances.global;
                }
                return r;
            }

            /// <summary>
            /// Sums StatusPacket multis for magic resistance
            /// </summary>
            private float GetMulti_Magic ()
            {
                float r = 1;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r *= owner.statusPackets[keys[i]].resistances.magic;
                }
                return r;
            }

            /// <summary>
            /// Sums StatusPacket multis for strike resistance
            /// </summary>
            private float GetMulti_Strike ()
            {
                float r = 1;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r *= owner.statusPackets[keys[i]].resistances.strike;
                }
                return r;
            }

            /// <summary>
            /// Sums StatusPacket multis for slash resistance
            /// </summary>
            private float GetMulti_Slash ()
            {
                float r = 1;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r *= owner.statusPackets[keys[i]].resistances.slash;
                }
                return r;
            }

            /// <summary>
            /// Sums StatusPacket multis for thrust resistance
            /// </summary>
            private float GetMulti_Thrust ()
            {
                float r = 1;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r *= owner.statusPackets[keys[i]].resistances.thrust;
                }
                return r;
            }

            /// <summary>
            /// Sums StatusPacket multis for fire resistance
            /// </summary>
            private float GetMulti_Fire ()
            {
                float r = 1;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r *= owner.statusPackets[keys[i]].resistances.fire;
                }
                return r;
            }

            /// <summary>
            /// Sums StatusPacket multis for earth resistance
            /// </summary>
            private float GetMulti_Earth()
            {
                float r = 1;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r *= owner.statusPackets[keys[i]].resistances.earth;
                }
                return r;
            }

            /// <summary>
            /// Sums StatusPacket multis for Air resistance
            /// </summary>
            private float GetMulti_Air()
            {
                float r = 1;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r *= owner.statusPackets[keys[i]].resistances.air;
                }
                return r;
            }

            /// <summary>
            /// Sums StatusPacket multis for water resistance
            /// </summary>
            private float GetMulti_Water()
            {
                float r = 1;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r *= owner.statusPackets[keys[i]].resistances.water;
                }
                return r;
            }

            /// <summary>
            /// Sums StatusPacket multis for light resistance
            /// </summary>
            private float GetMulti_Light()
            {
                float r = 1;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r *= owner.statusPackets[keys[i]].resistances.light;
                }
                return r;
            }

            /// <summary>
            /// Sums StatusPacket multis for dark resistance
            /// </summary>
            private float GetMulti_Dark()
            {
                float r = 1;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r *= owner.statusPackets[keys[i]].resistances.dark;
                }
                return r;
            }

            /// <summary>
            /// Sums StatusPacket multis for bio resistance
            /// </summary>
            private float GetMulti_Bio()
            {
                float r = 1;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r *= owner.statusPackets[keys[i]].resistances.bio;
                }
                return r;
            }

            /// <summary>
            /// Sums StatusPacket multis for sound resistance
            /// </summary>
            private float GetMulti_Sound()
            {
                float r = 1;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r *= owner.statusPackets[keys[i]].resistances.sound;
                }
                return r;
            }

            /// <summary>
            /// Sums StatusPacket multis for psyche resistance
            /// </summary>
            private float GetMulti_Psyche()
            {
                float r = 1;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r *= owner.statusPackets[keys[i]].resistances.psyche;
                }
                return r;
            }

            /// <summary>
            /// Sums StatusPacket multis for reality resistance
            /// </summary>
            private float GetMulti_Reality()
            {
                float r = 1;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r *= owner.statusPackets[keys[i]].resistances.reality;
                }
                return r;
            }

            /// <summary>
            /// Sums StatusPacket multis for Time resistance
            /// </summary>
            private float GetMulti_Time()
            {
                float r = 1;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r *= owner.statusPackets[keys[i]].resistances.time;
                }
                return r;
            }

            /// <summary>
            /// Sums StatusPacket multis for space resistance
            /// </summary>
            private float GetMulti_Space()
            {
                float r = 1;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r *= owner.statusPackets[keys[i]].resistances.space;
                }
                return r;
            }

            /// <summary>
            /// Sums StatusPacket multis for electric resistance
            /// </summary>
            private float GetMulti_Electric()
            {
                float r = 1;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r *= owner.statusPackets[keys[i]].resistances.electric;
                }
                return r;
            }

            /// <summary>
            /// Sums StatusPacket multis for ice resistance
            /// </summary>
            private float GetMulti_Ice()
            {
                float r = 1;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r *= owner.statusPackets[keys[i]].resistances.ice;
                }
                return r;
            }

            /// <summary>
            /// Sums StatusPacket multis for spirit resistance
            /// </summary>
            private float GetMulti_Spirit()
            {
                float r = 1;
                StatusType[] keys = owner.statusPackets.Keys.ToArray();
                for (int i = 0; i < owner.statusPackets.Count; i++)
                {
                    r *= owner.statusPackets[keys[i]].resistances.spirit;
                }
                return r;
            }

        }

        public readonly BattlerType battlerType;
        public readonly Stats stats;
        public readonly Dictionary<StatusType, StatusPacket> statusPackets;


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