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
        public static TurnActions defaultTurnActions = new TurnActions(false, -1, new Battler[0], new Battler[0], ActionDatabase.SpecialActions.defaultBattleAction, StanceDatabase.SpecialStances.defaultStance);

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
            ///  Establishes a Resistances_Raw all resists equal to v
            /// </summary>
            public Resistances_Raw (float v)
            {
                global = magic = strike = slash = thrust = fire = earth = air = water = light = dark = bio = sound = psyche = reality = time = space = electric = ice = spirit = v;
            }

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
        public class Resistances
        {
            public readonly Resistances_Raw raw;
            public readonly Battler owner;

            public float global { get { return raw.global * owner.currentStance.resistances.global * owner.metaStance.resistances.global * GetMulti_Global(); } }
            public float magic { get { return raw.magic * owner.currentStance.resistances.magic * owner.metaStance.resistances.magic * GetMulti_Magic(); } }
            public float strike { get { return raw.strike * owner.currentStance.resistances.strike * owner.metaStance.resistances.strike * GetMulti_Strike(); } }
            public float slash { get { return raw.slash * owner.currentStance.resistances.slash * owner.metaStance.resistances.slash * GetMulti_Slash(); } }
            public float thrust { get { return raw.thrust * owner.currentStance.resistances.thrust * owner.metaStance.resistances.thrust * GetMulti_Thrust(); } }
            public float fire { get { return raw.fire * owner.currentStance.resistances.fire * owner.metaStance.resistances.fire * GetMulti_Fire(); } }
            public float earth { get { return raw.earth * owner.currentStance.resistances.earth * owner.metaStance.resistances.earth * GetMulti_Earth(); } }
            public float air { get { return raw.air * owner.currentStance.resistances.air * owner.metaStance.resistances.air * GetMulti_Air(); } }
            public float water { get { return raw.water * owner.currentStance.resistances.water * owner.metaStance.resistances.water * GetMulti_Water(); } }
            public float light { get { return raw.light * owner.currentStance.resistances.light * owner.metaStance.resistances.light * GetMulti_Light(); } }
            public float dark { get { return raw.dark * owner.currentStance.resistances.dark * owner.metaStance.resistances.dark * GetMulti_Dark(); } }
            public float bio { get { return raw.bio * owner.currentStance.resistances.bio * owner.metaStance.resistances.bio * GetMulti_Bio(); } }
            public float sound { get { return raw.sound * owner.currentStance.resistances.sound * owner.metaStance.resistances.sound * GetMulti_Sound(); } }
            public float psyche { get { return raw.psyche * owner.currentStance.resistances.psyche * owner.metaStance.resistances.psyche * GetMulti_Psyche(); } }
            public float reality { get { return raw.reality * owner.currentStance.resistances.reality * owner.metaStance.resistances.reality * GetMulti_Reality(); } }
            public float time { get { return raw.time * owner.currentStance.resistances.time * owner.metaStance.resistances.time * GetMulti_Time(); } }
            public float space { get { return raw.space * owner.currentStance.resistances.space * owner.metaStance.resistances.space * GetMulti_Space(); } }
            public float electric { get { return raw.electric * owner.currentStance.resistances.electric * owner.metaStance.resistances.electric * GetMulti_Electric(); } }
            public float ice { get { return raw.ice * owner.currentStance.resistances.ice * owner.metaStance.resistances.ice * GetMulti_Ice(); } }
            public float spirit { get { return raw.spirit * owner.currentStance.resistances.spirit * owner.metaStance.resistances.spirit * GetMulti_Spirit(); } }

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

        /// <summary>
        /// Data structure containing everything a Battler wants to do for its turn - move (moves aren't a thing yet, this is reserved),
        /// targets/alt targets, action, etc.
        /// </summary>
        public struct TurnActions
        {
            /// <summary>
            /// True if we changed stances before selecting our action. False otherwise.
            /// </summary>
            public readonly bool stanceChanged;
            /// <summary>
            /// (DUMMY) Distance what we gonna move.
            /// </summary>
            public readonly float moveDist;
            /// <summary>
            /// Primary target set array
            /// </summary>
            public readonly Battler[] targets;
            /// <summary>
            /// Alternate target set array
            /// </summary>
            public readonly Battler[] alternateTargets;
            /// <summary>
            /// Action what we're doing.
            /// </summary>
            public readonly BattleAction action;
            /// <summary>
            /// Stance what we do it in
            /// </summary>
            public readonly BattleStance stance;

            /// <summary>
            /// Constructor. TurnActions is a structure for passing data on what we're doing this turn to the battle overseer, so
            /// you should only really be generating these at its behest.
            /// </summary>
            internal TurnActions (bool _stanceChanged, float _moveDist, Battler[] _targets, Battler[] _alternateTargets, BattleAction _action, BattleStance _stance)
            {
                stanceChanged = _stanceChanged;
                moveDist = _moveDist;
                targets = _targets;
                alternateTargets = _alternateTargets;
                action = _action;
                stance = _stance;
            }
        }

        // Things derived from the battler data table
        public readonly BattlerType battlerType;
        public readonly BattlerAIType aiType;
        public readonly BattlerAIFlags aiFlags;
        public int level { get; private set; }
        public readonly Stats stats;
        public readonly BattleStance[] stances;
        public BattleStance metaStance { get; private set; } // a second stance that goes "on top" of the main one adding actions/bonus/multis
        public float footprintRadius { get; private set; }
        public Resistances resistances { get; private set; }

        // Things derived from a specific formation instance
        public BattlerSideFlags side { get; private set; } // This is a bitflag, but it should never do bitflaggy things here because that'd be weird.
        public readonly int asSideIndex; // This is just the index of battler within side, and is mainly used to quickly identify which player party infobox a player-side battler should use.

        // Transients
        public bool isDead { get; private set; }
        public BattleStance currentStance { get; private set; }
        public float currentDelay { get; private set; }
        public int currentHP { get; private set; }
        public int currentStamina { get; private set; }
        public Vector3 logicalPosition { get; private set; } // This is what we use for determining targeting ranges, etc. When we get a move action, we set logicalPosition immediately, and the puppet moves there. z-axis doesn't matter - battlefield is 2D.
        public readonly Dictionary<StatusType, StatusPacket> statusPackets;
        public TurnActions turnActions { get; private set; }
        public BattleAction lastActionExecuted { get; private set; }
        public BattleStance lockedStance { get; private set; }

        // Collider, which is ugly, but using Unity colliders is the simplest way to do AOE checks and shit
        public CapsuleCollider capsuleCollider { get; private set; }

        public BattlerPuppet puppet { get; private set; }

        // Magic
        public float speedFactor { get { return stats.Spe / BattleOverseer.normalizedSpeed; } }
        public int index { get { return BattleOverseer.allBattlers.IndexOf(this); } }

        public Battler (BattleFormation.FormationMember fm)
        {
            statusPackets = new Dictionary<StatusType, StatusPacket>();

            // Load in everything from the battler data table first
            battlerType = fm.battlerData.battlerType;
            level = fm.battlerData.level;
            aiType = fm.battlerData.aiType;
            aiFlags = fm.battlerData.aiFlags;
            int baseHP = fm.battlerData.baseHP;
            int baseATK = fm.battlerData.baseATK;
            int baseDEF = fm.battlerData.baseDEF;
            int baseMATK = fm.battlerData.baseMATK;
            int baseMDEF = fm.battlerData.baseMDEF;
            int baseSPE = fm.battlerData.baseSPE;
            int baseEVA = fm.battlerData.baseEVA;
            int baseHIT = fm.battlerData.baseHIT;
            if (!fm.battlerData.isFixedStats)
            {
                baseHP = CalculateStat(baseHP, fm.battlerData.level, fm.battlerData.growths.HP);
                baseATK = CalculateStat(baseATK, fm.battlerData.level, fm.battlerData.growths.ATK);
                baseDEF = CalculateStat(baseDEF, fm.battlerData.level, fm.battlerData.growths.DEF);
                baseMATK = CalculateStat(baseMATK, fm.battlerData.level, fm.battlerData.growths.MATK);
                baseMDEF = CalculateStat(baseMDEF, fm.battlerData.level, fm.battlerData.growths.MDEF);
                baseSPE = CalculateStat(baseSPE, fm.battlerData.level, fm.battlerData.growths.SPE);
                baseEVA = CalculateStat(baseEVA, fm.battlerData.level, fm.battlerData.growths.EVA);
                baseHIT = CalculateStat(baseHIT, fm.battlerData.level, fm.battlerData.growths.HIT);
            }
            stats = new Stats(this, baseHP, baseATK, baseDEF, baseMATK, baseMDEF, baseSPE, baseHIT, baseEVA, fm.battlerData.baseMoveDist, fm.battlerData.baseMoveDelay);
            stances = fm.battlerData.stances;
            metaStance = fm.battlerData.metaStance;
            footprintRadius = fm.battlerData.size;
            resistances = new Resistances(fm.battlerData.resistances, this);

            // Now: conform the Battler to the details in the FormationMember
            currentStance = fm.startStance;
            logicalPosition = fm.fieldPosition;
            side = fm.side;
            asSideIndex = fm.asSideIndex;

            // Initialize transients
            isDead = false;
            currentDelay = 0;
            currentHP = stats.maxHP;
            currentStamina = currentStance.maxStamina;
            turnActions = defaultTurnActions;
        }

        /// <summary>
        /// Given base stat, level, and growth bias, calculates stat at level.
        /// </summary>
        /// <param name="baseStat">Base stat value. Approximately = stat at level 120.</param>
        /// <param name="level">I ain't gonna spell this out for you.</param>
        /// <param name="growth">Growth factor. Normally less than 1. Applies a multiplier equal to growth at level 1 and 1 at level 120. 
        /// Lower values cause steeper slopes, as more of the stat gains are loaded into the later levels.</param>
        public static int CalculateStat(int baseStat, int level, float growth)
        {
            const float mlv = maxLevel; // implicit divide-as-float
            const int adjustment = 30;
            return Mathf.RoundToInt(baseStat * ((level + adjustment) / mlv) * (growth + ((level / mlv) * (1 - growth))));
        }

        /// <summary>
        /// Applies speed factor and adds given float to current delay.
        /// Doesn't allow delay to go below zero.
        /// </summary>
        public void ApplyDelay (float delay)
        {
            currentDelay += (delay / speedFactor);
            if (currentDelay < 0) currentDelay = 0;
        }

        /// <summary>
        /// Applies transformations to battler state based on specified FXPackage.
        /// </summary>
        public void ApplyFXPackage (BattleAction.Subaction.FXPackage fxPackage)
        {
            switch (fxPackage.fxType)
            {
                case SubactionFXType.Test_PushTargetBackward:
                    Debug.Log("If unit movement existed, we would be pushed backward now");
                    break;
                case SubactionFXType.Test_Buff_STR:
                    ApplyStatus(StatusType.TestBuff, StatusPacket_CancelationCondition.CancelWhenDurationZero, 0, fxPackage.fxLength_Byte, new Resistances_Raw(1), 0, DamageTypeFlags.None,
                        MiscStatusEffectFlags.None, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, fxPackage.fxStrength_Float, 1, 1, 1, 1, 1, 1, 1, 1);
                    break;
                default:
                    throw new System.Exception("Unregognized FXtype: " + fxPackage.fxType);
            }
        }

        /// <summary>
        /// Creates a status packet based on the given parameters. If there's already a status packet in the dictionary of this type, collides the new one with it;
        /// otherwise, adds it to the dict. Returns true if we collided with an existing packet, false otherwise.
        /// </summary>
        public bool ApplyStatus (StatusType _statusType, StatusPacket_CancelationCondition _cancelationCondition, int _charges, int _duration, Resistances_Raw _resistances, int _recurringDamage, DamageTypeFlags _damageTypeFlags,
            MiscStatusEffectFlags _miscStatusEffectFlags, int _statBonus_MaxHP, int _statBonus_ATK, int _statBonus_DEF, int _statBonus_MATK, int _statBonus_MDEF, int _statBonus_SPE, int _statBonus_EVA, int _statBonus_HIT,
            float _statBonus_MoveDelay, float _statBonus_MoveDist, float _statMulti_MaxHP, float _statMulti_ATK, float _statMulti_DEF, float _statMulti_MATK, float _statMulti_MDEF, float _statMulti_SPE, float _statMulti_EVA,
            float _statMulti_HIT, float _statMulti_MoveDelay, float _statMulti_MoveDist)
        {
            bool keyAlreadyExisted = false;
            StatusPacket packet = new StatusPacket(statusPackets, _statusType, _cancelationCondition, _charges, _duration, _resistances, _recurringDamage, _damageTypeFlags, _miscStatusEffectFlags,
                _statBonus_MaxHP, _statBonus_ATK, _statBonus_DEF, _statBonus_MATK, _statBonus_MDEF, _statBonus_SPE, _statBonus_EVA, _statBonus_HIT, _statBonus_MoveDelay, _statBonus_MoveDist,
                _statMulti_MaxHP, _statMulti_ATK, _statMulti_DEF, _statMulti_MATK, _statMulti_MDEF, _statMulti_SPE, _statMulti_EVA, _statMulti_HIT, _statMulti_MoveDelay, _statMulti_MoveDist);
            if (statusPackets.ContainsKey(_statusType))
            {
                keyAlreadyExisted = true;
                statusPackets[_statusType].CollideWith(packet);
            }
            else statusPackets[_statusType] = packet;
            return keyAlreadyExisted;
        }

        /// <summary>
        /// Breaks this unit's stance.
        /// If spPenalty is greater than or equal to zero, we're forced to break our stance, so we're going to apply
        /// the forced version of the stance break debuffs with a strength dependent on how far into the red we are.
        /// Otherwise, we're breaking our stance voluntarily, so we get the much more friendly voluntary-stance-change
        /// penalties.
        /// </summary>
        public void BreakStance (int spPenalty = -1)
        {
            const float statPenaltiesMin = 0.25f; // at worst, forced stat changes leave you with one quarter of your previous def/mdef/spe
            if (spPenalty < 0) ApplyStatus(StatusType.StanceBroken_Voluntary, StatusPacket_CancelationCondition.None, 0, 0, new Resistances_Raw(1), 0, DamageTypeFlags.None, MiscStatusEffectFlags.None,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0.85f, 1, 0.85f, 1, 0.85f, 1, 1, 1);
            else
            {
                float penalty = 0.75f - ((spPenalty / 2.0f) * 0.01f);
                if (penalty < statPenaltiesMin) penalty = statPenaltiesMin;
                ApplyStatus(StatusType.StanceBroken_Forced, StatusPacket_CancelationCondition.None, 0, 0, new Resistances_Raw(1), 0, DamageTypeFlags.None, MiscStatusEffectFlags.None,
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, penalty, 1, penalty, 1, penalty, 1, 1, 1);
            }
            if (puppet == null) return;
            else puppet.DispatchAnimEvent(AnimEventType.StanceBreak);
        }

        /// <summary>
        /// Given the attacking Battler and the damage-dealing subaction, fetches the pertinent stats, applies any modifiers or whatever we need to, and
        /// runs the final figures through the damage calculator.
        /// </summary>
        public int CalcDamageAgainstMe (Battler attacker, BattleAction.Subaction subaction, bool withDeviation, bool weaknessAware, bool resistanceAware)
        {
            const float normalDeviation = 0.15f; // this is fiddly and will almost certainly get the shit tuned out of it
            float deviation;
            if (withDeviation) deviation = normalDeviation;
            else deviation = 0;
            int dmg = 0;
            if (subaction.baseDamage != 0)
            {
                int atkStat = -1;
                int defStat = -1;
                if (subaction.atkStat != LogicalStatType.None) atkStat = attacker.GetLogicalStatValue(subaction.atkStat);
                if (subaction.defStat != LogicalStatType.None) defStat = GetLogicalStatValue(subaction.defStat);
                int modifiedBaseDamage = subaction.baseDamage;
                if (subaction.baseDamage < 0) modifiedBaseDamage *= -1; // this should be positive for damage calculations as a rule; we re-flip the damage figure later if we're healing, but this gives more flexibility if I wanna change the damage formula down the line
                if (atkStat != -1 && defStat != -1) dmg = Util.DamageCalc(attacker.level, atkStat, defStat, subaction.baseDamage, deviation);
                else dmg = modifiedBaseDamage; // like with hit/misses: it should be possible for atk/def to do something unopposed but I dunno what that looks like.
                if (subaction.baseDamage < 0) dmg *= -1; // re-flip
                float resMod = GetResistance(subaction.damageTypes, resistanceAware, weaknessAware);
                dmg = Mathf.FloorToInt(dmg * resMod);
            }
            return dmg;
        }

        /// <summary>
        /// Applies any necessary modifiers to an action's base stamina cost and returns the final value.
        /// </summary>
        public int CalcActionStaminaCost (int baseStaminaCost)
        {
            return baseStaminaCost; // No status effects currently exist that would modify stamina cost.
        }

        /// <summary>
        /// Returns true if the battler is allowed to execute this action,
        /// false if one or more active statuses prevents it.
        /// </summary>
        public bool CanExecuteAction (BattleAction action)
        {
            return true; // No status effects that would preclude action execution presently exist, so this will always return true until we can give it something to do.
        }

        /// <summary>
        /// Sets battler stance immediately. Doesn't do any of the normal
        /// stance break behavior. This should only be used to let the AI
        /// system test a Battler's prospective actions in multiple stances,
        /// and you should be very careful to make sure you set the stance back
        /// to what it was originally when you're done.
        /// </summary>
        public void ChangeStance_ImmediateProvisional(BattleStance stance)
        {
            currentStance = stance;
        }

        /// <summary>
        /// Sets battler stance and removes the stance break debuffs.
        /// </summary>
        public void ChangeStanceTo (BattleStance stance)
        {
            if (stance == lockedStance) throw new System.Exception("Can't change stances to locked stance!");
            lockedStance = currentStance;
            currentStance = stance;
            currentStamina = stance.maxStamina;
            statusPackets.Remove(StatusType.StanceBroken_Voluntary);
            statusPackets.Remove(StatusType.StanceBroken_Forced);
            puppet.DispatchBattlerUIEvent(BattlerUIEventType.StanceChange);
        }

        /// <summary>
        /// Applies transformations to battler state based on chosen action,
        /// then unsets chosen action.
        /// </summary>
        public void CommitCurrentChosenActions ()
        {
            if (turnActions.stanceChanged) ChangeStanceTo(turnActions.stance);
            ApplyDelay(turnActions.moveDist * stats.moveDelay);
            // stance break is a special case - it doesn't have its own base delay value; the delay incurred by breaking your own stance is determined by the followthrough stance change delay of the last action you used
            if (turnActions.action.actionID == ActionType.INTERNAL_BreakOwnStance) ApplyDelay(turnActions.action.baseFollowthroughStanceChangeDelay);
            else ApplyDelay(turnActions.action.baseDelay);
            DealOrHealStaminaDamage(CalcActionStaminaCost(turnActions.action.baseSPCost));
            lastActionExecuted = turnActions.action;
            turnActions = defaultTurnActions;
        }

        /// <summary>
        /// Deals/heals damage.
        /// </summary>
        public void DealOrHealDamage (int dmg)
        {
            if (dmg != 0)
            {
                currentHP -= dmg;
                if (currentHP > stats.maxHP) currentHP = stats.maxHP;
                if (currentHP <= 0) Die();
                puppet.DispatchBattlerUIEvent(BattlerUIEventType.HPValueChange);
                if (dmg > 0) puppet.DispatchAnimEvent(AnimEventType.Hit);
                else if (dmg < 0) puppet.DispatchAnimEvent(AnimEventType.Heal);
            }
            else puppet.DispatchAnimEvent(AnimEventType.NoSell);

        }

        /// <summary>
        /// Deals/heals stamina damage.
        /// Immediately breaks stance if we're in the red.
        /// </summary>
        public void DealOrHealStaminaDamage (int dmg)
        {
            if (dmg != 0)
            {
                currentStamina -= dmg;
                if (currentStamina > currentStance.maxStamina) currentStamina = currentStance.maxStamina;
                else if (currentStamina < 0)
                {
                    BreakStance(-currentStamina);
                }
                puppet.DispatchBattlerUIEvent(BattlerUIEventType.StaminaValueChange);
            }
        }

        /// <summary>
        /// Causes the battler to die.
        /// </summary>
        public void Die ()
        {
            currentHP = 0;
            currentStamina = 0;
            currentStance = StanceDatabase.Get(StanceType.None);
            currentDelay = float.PositiveInfinity;
            statusPackets.Clear();
            isDead = true;
            if (puppet == null) return;
            else puppet.DispatchAnimEvent(AnimEventType.Die);
            BattleOverseer.BattlerIsDead(this);
        }

        /// <summary>
        /// Called on each Battler in the battle between turns.
        /// turndelayReduction is the "lowest delay" value we get during the
        /// between-turns phase, and we subtract that from the delay value of all battlers.
        /// </summary>
        public void BetweenTurns (float turnDelayReduction)
        {
            if (!isDead)
            {
                currentDelay -= turnDelayReduction;
                if (currentDelay < 0) currentDelay = 0;
                if (currentDelay == 0) BattleOverseer.RequestTurn(this);
            }
        }
        
        /// <summary>
        /// Call this to make the Battler start figuring out what action to take.
        /// This doesn't return anything. What it _does_ do is cause the Battler
        /// to update its TurnActions with the parameters the battle system
        /// needs to execute an action. The BattleOverseer tells the Battler
        /// to put its order in that box, and it needs to _watch_ that box
        /// once it puts in the request.
        /// </summary>
        public void GetAction ()
        {
            bool changeStances = false;
            if (StanceBroken()) changeStances = true;
            BattlerAISystem.StartThinking(this, changeStances);
        }

        /// <summary>
        /// Gets the value corresponding to a given LogicalStatType, doing
        /// any necessary math to return the appropriate value.
        /// </summary>
        public int GetLogicalStatValue (LogicalStatType logicalStatType)
        {
            switch (logicalStatType)
            {
                case LogicalStatType.Stat_MaxHP:
                    return stats.maxHP;
                case LogicalStatType.Stat_CurrentSP:
                    return currentStamina;
                case LogicalStatType.Stat_ATK:
                    return stats.ATK;
                case LogicalStatType.Stat_DEF:
                    return stats.DEF;
                case LogicalStatType.Stat_MATK:
                    return stats.MATK;
                case LogicalStatType.Stat_MDEF:
                    return stats.MDEF;
                case LogicalStatType.Stat_SPE:
                    return stats.Spe;
                case LogicalStatType.Stat_EVA:
                    return stats.EVA;
                case LogicalStatType.Stat_HIT:
                    return stats.Hit;
                case LogicalStatType.Stats_ATKDEF:
                    return Util.Mean(new int[] { stats.ATK, stats.DEF });
                case LogicalStatType.Stats_ATKHIT:
                    return Util.Mean(new int[] { stats.ATK, stats.Hit });
                case LogicalStatType.Stats_ATKMATK:
                    return Util.Mean(new int[] { stats.ATK, stats.MATK });
                case LogicalStatType.Stats_ATKSPE:
                    return Util.Mean(new int[] { stats.ATK, stats.Spe });
                case LogicalStatType.Stats_DEFEVA:
                    return Util.Mean(new int[] { stats.DEF, stats.EVA });
                case LogicalStatType.Stats_DEFMDEF:
                    return Util.Mean(new int[] { stats.DEF, stats.MDEF });
                case LogicalStatType.Stats_MATKHIT:
                    return Util.Mean(new int[] { stats.MATK, stats.Hit });
                case LogicalStatType.Stats_MATKMDEF:
                    return Util.Mean(new int[] { stats.MATK, stats.MDEF });
                case LogicalStatType.Stats_MATKSPE:
                    return Util.Mean(new int[] { stats.MATK, stats.Spe });
                case LogicalStatType.Stats_MDEFEVA:
                    return Util.Mean(new int[] { stats.MDEF, stats.EVA });
                case LogicalStatType.Stats_All:
                    return Util.Mean(new int[] { stats.ATK, stats.DEF, stats.MATK, stats.MDEF, stats.Spe, stats.EVA, stats.Hit });
                default:
                    throw new System.Exception("Can't return value logical stat value for LogicalStatType value of " + logicalStatType.ToString());
            }
        }

        /// <summary>
        /// Gets total resistances for an attack of the given damage type.
        /// This can accept a DamageTypeFlags with multiple flags set, and will spit out
        /// the product of all relevant resistances.
        /// </summary>
        public float GetResistance (DamageTypeFlags damageType, bool resistanceAware, bool weaknessAware)
        {
            float r;
            if ((resistanceAware | resistances.global > 1) | (weaknessAware | resistances.global < 1)) r = resistances.global;
            else r = 1;
            if ((damageType & DamageTypeFlags.Magic) == DamageTypeFlags.Magic && (resistanceAware | resistances.magic > 1) && (weaknessAware | resistances.magic < 1)) r *= resistances.magic;
            if ((damageType & DamageTypeFlags.Strike) == DamageTypeFlags.Strike && (resistanceAware | resistances.strike > 1) && (weaknessAware | resistances.strike < 1)) r *= resistances.strike;
            if ((damageType & DamageTypeFlags.Slash) == DamageTypeFlags.Slash && (resistanceAware | resistances.slash > 1) && (weaknessAware | resistances.slash < 1)) r *= resistances.slash;
            if ((damageType & DamageTypeFlags.Thrust) == DamageTypeFlags.Thrust && (resistanceAware | resistances.thrust > 1) && (weaknessAware | resistances.thrust < 1)) r *= resistances.thrust;
            if ((damageType & DamageTypeFlags.Fire) == DamageTypeFlags.Fire && (resistanceAware | resistances.fire > 1) && (weaknessAware | resistances.fire < 1)) r *= resistances.fire;
            if ((damageType & DamageTypeFlags.Earth) == DamageTypeFlags.Earth && (resistanceAware | resistances.earth > 1) && (weaknessAware | resistances.earth < 1)) r *= resistances.earth;
            if ((damageType & DamageTypeFlags.Air) == DamageTypeFlags.Air && (resistanceAware | resistances.air > 1) && (weaknessAware | resistances.air < 1)) r *= resistances.air;
            if ((damageType & DamageTypeFlags.Water) == DamageTypeFlags.Water && (resistanceAware | resistances.water > 1) && (weaknessAware | resistances.water < 1)) r *= resistances.water;
            if ((damageType & DamageTypeFlags.Light) == DamageTypeFlags.Light && (resistanceAware | resistances.light > 1) && (weaknessAware | resistances.light < 1)) r *= resistances.light;
            if ((damageType & DamageTypeFlags.Dark) == DamageTypeFlags.Dark && (resistanceAware | resistances.dark > 1) && (weaknessAware | resistances.dark < 1)) r *= resistances.dark;
            if ((damageType & DamageTypeFlags.Bio) == DamageTypeFlags.Bio && (resistanceAware | resistances.bio > 1) && (weaknessAware | resistances.bio < 1)) r *= resistances.bio;
            if ((damageType & DamageTypeFlags.Sound) == DamageTypeFlags.Sound && (resistanceAware | resistances.sound > 1) && (weaknessAware | resistances.sound < 1)) r *= resistances.sound;
            if ((damageType & DamageTypeFlags.Psyche) == DamageTypeFlags.Psyche && (resistanceAware | resistances.psyche > 1) && (weaknessAware | resistances.psyche < 1)) r *= resistances.psyche;
            if ((damageType & DamageTypeFlags.Reality) == DamageTypeFlags.Reality && (resistanceAware | resistances.reality > 1) && (weaknessAware | resistances.reality < 1)) r *= resistances.reality;
            if ((damageType & DamageTypeFlags.Time) == DamageTypeFlags.Time && (resistanceAware | resistances.time > 1) && (weaknessAware | resistances.time < 1)) r *= resistances.time;
            if ((damageType & DamageTypeFlags.Space) == DamageTypeFlags.Space && (resistanceAware | resistances.space > 1) && (weaknessAware | resistances.space < 1)) r *= resistances.space;
            if ((damageType & DamageTypeFlags.Electric) == DamageTypeFlags.Electric && (resistanceAware | resistances.electric > 1) && (weaknessAware | resistances.electric < 1)) r *= resistances.electric;
            if ((damageType & DamageTypeFlags.Ice) == DamageTypeFlags.Ice && (resistanceAware | resistances.ice > 1) && (weaknessAware | resistances.ice < 1)) r *= resistances.ice;
            if ((damageType & DamageTypeFlags.Spirit) == DamageTypeFlags.Spirit && (resistanceAware | resistances.spirit > 1) && (weaknessAware | resistances.spirit < 1)) r *= resistances.spirit;
            return r;
        }

        /// <summary>
        /// Gives this Battler control over the specified puppet.
        /// </summary>
        public void GivePuppet (BattlerPuppet _puppet)
        {
            puppet = _puppet;
            capsuleCollider = puppet.capsuleCollider;
            capsuleCollider.radius = footprintRadius;
        }

        /// <summary>
        /// Takes a TurnActions struct and stores it as our turnActions.
        /// Also takes a set of BattlerAIMessageFlags, which can
        /// be used to communicate things like eg. "extend your turn"
        /// or "you can't move next turn."
        /// (Both of those flags are of course used to allow player
        /// units to move before selecting an action.)
        /// </summary>
        /// <param name="turnActions"></param>
        public void ReceiveAThought (TurnActions _turnActions, BattlerAIMessageFlags messageFlags)
        {
            if (_turnActions.action.actionID == ActionType.INTERNAL_BreakOwnStance && StanceBroken()) throw new System.Exception("u wot m8");
            turnActions = _turnActions;
            if ((messageFlags & BattlerAIMessageFlags.ExtendTurn) == BattlerAIMessageFlags.ExtendTurn)
                BattleOverseer.ExtendCurrentTurn();
            if ((messageFlags & BattlerAIMessageFlags.ForbidMovementOnNextTurn) == BattlerAIMessageFlags.ForbidMovementOnNextTurn)
                Debug.Log("What even is movement, mannnn");
        }

        /// <summary>
        /// Returns true if either stance break debuff is currently on the Battler, false otherwise.
        /// </summary>
        public bool StanceBroken ()
        {
            return statusPackets.ContainsKey(StatusType.StanceBroken_Forced) || statusPackets.ContainsKey(StatusType.StanceBroken_Voluntary);
        }

        /// <summary>
        /// DUMMY: Always returns true.
        /// Designed behavior: returns true if this battler is a valid target given the combination of targeting battler and action.
        /// </summary>
        public bool IsValidTargetFor (Battler targeter, BattleAction action)
        {
            //Debug.Log("Please implement Battler.IsValidTargetFor()");
            return true;
        }

        /// <summary>
        /// Runs hit/evade checks for (the damage-dealing component of) a given subaction against this battler.
        /// </summary>
        public bool TryToLandAttackAgainstMe (Battler attacker, BattleAction.Subaction subaction)
        {
            float modifiedAccuracy = BattleUtility.GetModifiedAccuracyFor(subaction, attacker, this);
            // like with fx packages: there should also be non-contested hit/evade bonuses if the subaction says "yo I got a hit stat but no evade stat" or vice versa, but I don't know what the math looks like yet
            return (modifiedAccuracy > Random.Range(0f, 1f));
        }

        /// <summary>
        /// Runs hit/evade checks for given fx package against this Battler.
        /// </summary>
        public bool TryToLandFXAgainstMe(Battler attacker, BattleAction.Subaction.FXPackage fxPackage)
        {
            float adjustedSuccessRate = BattleUtility.GetModifiedAccuracyFor(fxPackage, attacker, this);
            // It should also be possible for uncontested hit/evade stats to provide hit/evade bonuses on FX packages, but that requires me to have some idea of what the numbers look like
            return (adjustedSuccessRate > Random.Range(0f, 1f));
        }

    }

}