using UnityEngine;
using System.Collections.Generic;

namespace CnfBattleSys
{
    /// <summary>
    /// The options for what condition has to be fulfilled for a StatusPacket to be considered "finished" and removed from the Battler.
    /// None: lasts forever
    /// CancelWhenDurationZero: set duration when initializing StatusPacket, tick down each time the unit moves, remove when that hits zero
    /// CancelWhenChargesZero: set charges when initializing StatusPacket, tick down each time the "condition" associated with one of its MiscStatusEffectFlags happens. Ex: 
    /// damage reflect - > lose one charge each time you reflect damage. This is effectively infinite if you aren't using MiscStatusEffectFlags that can lose charges somehow, so be careful.
    /// CancelWhenEitherDurationOrChargesZero: all of the above.
    /// </summary>
    public enum StatusPacket_CancelationCondition
    {
        None,
        CancelWhenDurationZero,
        CancelWhenChargesZero,
        CancelWhenEitherDurationOrChargesZero
    }

    /// <summary>
    /// Stores data pertaining to one specific status ailment/buff/debuff/whatever.
    /// </summary>
    public class StatusPacket
    {
        private readonly Dictionary<StatusType, StatusPacket> dict;
        public readonly StatusType statusType;
        public readonly StatusPacket_CancelationCondition cancelationCondition;
        public readonly int originalCharges;
        public readonly int originalDuration;
        public readonly Battler.Resistances_Raw resistances;
        public readonly int recurringDamage;
        public readonly DamageTypeFlags damageTypeFlags;
        public readonly MiscStatusEffectFlags miscStatusEffectFlags;
        public readonly int statBonus_MaxHP;
        public readonly int statBonus_ATK;
        public readonly int statBonus_DEF;
        public readonly int statBonus_MATK;
        public readonly int statBonus_MDEF;
        public readonly int statBonus_SPE;
        public readonly int statBonus_EVA;
        public readonly int statBonus_HIT;
        public readonly float statBonus_MoveDelay;
        public readonly float statBonus_MoveDist;
        public readonly float statMulti_MaxHP;
        public readonly float statMulti_ATK;
        public readonly float statMulti_DEF;
        public readonly float statMulti_MATK;
        public readonly float statMulti_MDEF;
        public readonly float statMulti_SPE;
        public readonly float statMulti_EVA;
        public readonly float statMulti_HIT;
        public readonly float statMulti_MoveDelay;
        public readonly float statMulti_MoveDist;
        public int charges { get; private set; }
        public int duration { get; private set; }

        /// <summary>
        /// Constructor for StatusPacket.
        /// Reminder: most of this thing is read-only, so
        /// modifying the details of existing status packets
        /// requires you to create a new one.
        /// </summary>
        public StatusPacket (Dictionary<StatusType, StatusPacket> _dict, StatusType _statusType, StatusPacket_CancelationCondition _cancelationCondition, int _charges, int _duration, Battler.Resistances_Raw _resistances,
            int _recurringDamage, DamageTypeFlags _damageTypeFlags, MiscStatusEffectFlags _miscStatusEffectFlags, int _statBonus_MaxHP, int _statBonus_ATK, int _statBonus_DEF, int _statBonus_MATK, int _statBonus_MDEF,
            int _statBonus_SPE, int _statBonus_EVA, int _statBonus_HIT, float _statBonus_MoveDelay, float _statBonus_MoveDist, float _statMulti_MaxHP, float _statMulti_ATK, float _statMulti_DEF, float _statMulti_MATK,
            float _statMulti_MDEF, float _statMulti_SPE, float _statMulti_EVA, float _statMulti_HIT, float _statMulti_MoveDelay, float _statMulti_MoveDist)
        {
            dict = _dict;
            statusType = _statusType;
            cancelationCondition = _cancelationCondition;
            charges = originalCharges = _charges;
            duration = originalDuration = _duration;
            resistances = _resistances;
            recurringDamage = _recurringDamage;
            damageTypeFlags = _damageTypeFlags;
            miscStatusEffectFlags = _miscStatusEffectFlags;
            statBonus_MaxHP = _statBonus_MaxHP;
            statBonus_ATK = _statBonus_ATK;
            statBonus_DEF = _statBonus_DEF;
            statBonus_MATK = _statBonus_MATK;
            statBonus_MDEF = _statBonus_MDEF;
            statBonus_SPE = _statBonus_SPE;
            statBonus_EVA = _statBonus_EVA;
            statBonus_HIT = _statBonus_HIT;
            statBonus_MoveDelay = _statBonus_MoveDelay;
            statBonus_MoveDist = _statBonus_MoveDist;
            statMulti_MaxHP = _statMulti_MaxHP;
            statMulti_ATK = _statMulti_ATK;
            statMulti_DEF = _statMulti_DEF;
            statMulti_MATK = _statMulti_MATK;
            statMulti_MDEF = _statMulti_MDEF;
            statMulti_EVA = _statMulti_EVA;
            statMulti_HIT = _statMulti_HIT;
            statMulti_MoveDelay = _statMulti_MoveDelay;
            statMulti_MoveDist = _statMulti_MoveDist;
        }

        /// <summary>
        /// Merges two StatusPackets of the same StatusType.
        /// </summary>
        public StatusPacket CollideWith (StatusPacket _sp)
        {
            if (_sp.statusType != statusType) throw new System.Exception("Can't merge two StatusPackets of different types");
            StatusPacket_CancelationCondition cc = cancelationCondition;
            if (_sp.cancelationCondition != cc)
            {
                switch (_sp.cancelationCondition)
                {
                    case StatusPacket_CancelationCondition.None:
                        cc = StatusPacket_CancelationCondition.None;
                        break;
                    case StatusPacket_CancelationCondition.CancelWhenChargesZero:
                        if (cc == StatusPacket_CancelationCondition.CancelWhenDurationZero) cc = StatusPacket_CancelationCondition.CancelWhenEitherDurationOrChargesZero;
                        break;
                    case StatusPacket_CancelationCondition.CancelWhenDurationZero:
                        if (cc == StatusPacket_CancelationCondition.CancelWhenChargesZero) cc = StatusPacket_CancelationCondition.CancelWhenEitherDurationOrChargesZero;
                        break;
                    case StatusPacket_CancelationCondition.CancelWhenEitherDurationOrChargesZero:
                        cc = StatusPacket_CancelationCondition.CancelWhenEitherDurationOrChargesZero;
                        break;
                }
            }
            Battler.Resistances_Raw nr = new Battler.Resistances_Raw(resistances.global * _sp.resistances.global, resistances.magic * _sp.resistances.magic, resistances.strike * _sp.resistances.strike,
                resistances.slash * _sp.resistances.slash, resistances.thrust * _sp.resistances.thrust, resistances.fire * _sp.resistances.fire, resistances.earth * _sp.resistances.earth, resistances.air * _sp.resistances.air,
                resistances.water * _sp.resistances.water, resistances.light * _sp.resistances.light, resistances.dark * _sp.resistances.dark, resistances.bio * _sp.resistances.bio, resistances.sound * _sp.resistances.sound,
                resistances.psyche * _sp.resistances.psyche, resistances.reality * _sp.resistances.reality, resistances.time * _sp.resistances.time, resistances.space * _sp.resistances.space, resistances.electric * _sp.resistances.electric,
                resistances.ice * _sp.resistances.ice, resistances.spirit * _sp.resistances.spirit);
            StatusPacket newStatusPacket = new StatusPacket(dict, statusType, cc, charges + _sp.charges, duration + _sp.duration, nr, recurringDamage + _sp.recurringDamage, damageTypeFlags & _sp.damageTypeFlags,
                miscStatusEffectFlags & _sp.miscStatusEffectFlags, statBonus_MaxHP + _sp.statBonus_MaxHP, statBonus_ATK + _sp.statBonus_ATK, statBonus_DEF + _sp.statBonus_DEF, statBonus_MATK + _sp.statBonus_MATK,
                statBonus_MDEF + _sp.statBonus_MDEF, statBonus_SPE + _sp.statBonus_SPE, statBonus_EVA + _sp.statBonus_EVA, statBonus_HIT + _sp.statBonus_HIT, statBonus_MoveDelay + _sp.statBonus_MoveDelay,
                statBonus_MoveDist + _sp.statBonus_MoveDist, statMulti_MaxHP * _sp.statMulti_MaxHP, statMulti_ATK * _sp.statMulti_ATK, statMulti_DEF * _sp.statMulti_DEF, statMulti_MATK * _sp.statMulti_MATK,
                statMulti_MDEF * _sp.statMulti_MDEF, statMulti_SPE * _sp.statMulti_SPE, statMulti_EVA * _sp.statMulti_EVA, statMulti_HIT * _sp.statMulti_HIT, statMulti_MoveDelay * _sp.statMulti_MoveDelay,
                statMulti_MoveDist * _sp.statMulti_MoveDist);
            dict[statusType] = newStatusPacket;
            return newStatusPacket;
        }

        /// <summary>
        /// If StatusPacket has limited charges, remove _charges charges 
        /// in the event that _flag is set in our miscStatusEffectFlags field.
        /// Returns true if this results in the status in question being eliminated.
        /// (You can use that result for determining when to do e.g status effect fade animations, if needed.)
        /// </summary>
        public bool Discharge (MiscStatusEffectFlags _flag, int _charges = 1)
        {
            bool r = false;
            if (cancelationCondition == StatusPacket_CancelationCondition.CancelWhenChargesZero || cancelationCondition == StatusPacket_CancelationCondition.CancelWhenEitherDurationOrChargesZero)
            {
                charges -= _charges;
                if (charges <= 0)
                {
                    r = true;
                    Wipe();
                }
            }
            return r;
        }

        /// <summary>
        /// If StatusPacket is of limited duration, cut remaining duration by turns.
        /// Returns true if this results in the status in question being eliminated.
        /// </summary>
        public bool PassTurns (int turns = 1)
        {
            bool r = false;
            if (cancelationCondition == StatusPacket_CancelationCondition.CancelWhenDurationZero || cancelationCondition == StatusPacket_CancelationCondition.CancelWhenEitherDurationOrChargesZero)
            {
                duration -= turns;
                if (duration <= 0)
                {
                    r = true;
                    Wipe();
                }
            }
            return r;
        }

        /// <summary>
        /// Eliminates the designated StatusPacket.
        /// </summary>
        public void Wipe ()
        {
            dict.Remove(statusType);
        }
    }

}