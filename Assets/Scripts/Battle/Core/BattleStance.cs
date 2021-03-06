﻿using UnityEngine;

namespace CnfBattleSys
{
    /// <summary>
    /// Data structure representing a stance.
    /// </summary>
    public class BattleStance
    {
        public readonly StanceType stanceID;
        public readonly int animHash_Break;
        public readonly int animHash_Die;
        public readonly int animHash_Dodge;
        public readonly int animHash_Idle;
        public readonly int animHash_Heal;
        public readonly int animHash_Hit;
        public readonly int animHash_Move;
        public readonly Battler.Resistances_Raw resistances;
        public readonly BattleAction[] actionSet;
        public readonly BattleAction counterattackAction;
        public readonly float moveDelayBonus;
        public readonly float moveDelayMultiplier;
        public readonly float moveDistBonus;
        public readonly float moveDistMultiplier;
        public readonly float stanceChangeDelayBonus;
        public readonly float stanceChangeDelayMultiplier;
        public readonly float statMultiplier_MaxHP;
        public readonly float statMultiplier_ATK;
        public readonly float statMultiplier_DEF;
        public readonly float statMultiplier_MATK;
        public readonly float statMultiplier_MDEF;
        public readonly float statMultiplier_SPE;
        public readonly float statMultiplier_HIT;
        public readonly float statMultiplier_EVA;
        public readonly int statBonus_MaxHP;
        public readonly short statBonus_ATK;
        public readonly short statBonus_DEF;
        public readonly short statBonus_MATK;
        public readonly short statBonus_MDEF;
        public readonly short statBonus_SPE;
        public readonly short statBonus_HIT;
        public readonly short statBonus_EVA;
        public readonly byte maxStamina; // this is almost always 100 but I might wanna fudge it for enemies or something? idk

        /// <summary>
        /// Constructor. Shouldn't be called by anything outside of Datasets.LoadStances().
        /// </summary>
        public BattleStance (StanceType _stanceID, string _animName_Break, string _animName_Die, string _animName_Dodge, string _animName_Heal, string _animName_Hit, string _animName_Idle, string _animName_Move,
            BattleAction[] _actionSet, BattleAction _counterattackAction, float _moveDelayBonus, float _moveDelayMultiplier, float _moveDistBonus, float _moveDistMultiplier, float _stanceChangeDelayBonus, 
            float _stanceChangeDelayMultiplier, float _statMultiplier_MaxHP, float _statMultiplier_ATK, float _statMultiplier_DEF, float _statMultiplier_MATK, float _statMultiplier_MDEF, float _statMultiplier_SPE,
            float _statMultiplier_HIT, float _statMultiplier_EVA, int _statBonus_MaxHP, short _statBonus_ATK, short _statBonus_DEF, short _statBonus_MATK, short _statBonus_MDEF, short _statBonus_SPE, short _statBonus_HIT, 
            short _statBonus_EVA, byte _maxSP, Battler.Resistances_Raw _resistances)
        {
            stanceID = _stanceID;
            animHash_Break = Animator.StringToHash(_animName_Break);
            animHash_Die = Animator.StringToHash(_animName_Die);
            animHash_Dodge = Animator.StringToHash(_animName_Dodge);
            animHash_Heal = Animator.StringToHash(_animName_Heal);
            animHash_Hit = Animator.StringToHash(_animName_Hit);
            animHash_Idle = Animator.StringToHash(_animName_Idle);
            animHash_Move = Animator.StringToHash(_animName_Move);
            actionSet = _actionSet;
            counterattackAction = _counterattackAction;
            moveDelayBonus = _moveDelayBonus;
            moveDelayMultiplier = _moveDelayMultiplier;
            moveDistBonus = _moveDistBonus;
            moveDistMultiplier = _moveDistMultiplier;
            stanceChangeDelayBonus = _stanceChangeDelayBonus;
            stanceChangeDelayMultiplier = _stanceChangeDelayMultiplier;
            statMultiplier_MaxHP = _statMultiplier_MaxHP;
            statMultiplier_ATK = _statMultiplier_ATK;
            statMultiplier_DEF = _statMultiplier_DEF;
            statMultiplier_MATK = _statMultiplier_MATK;
            statMultiplier_MDEF = _statMultiplier_MDEF;
            statMultiplier_SPE = _statMultiplier_SPE;
            statMultiplier_HIT = _statMultiplier_HIT;
            statMultiplier_EVA = _statMultiplier_EVA;
            statBonus_MaxHP = _statBonus_MaxHP;
            statBonus_ATK = _statBonus_ATK;
            statBonus_DEF = _statBonus_DEF;
            statBonus_MATK = _statBonus_MATK;
            statBonus_MDEF = _statBonus_MDEF;
            statBonus_SPE = _statBonus_SPE;
            statBonus_HIT = _statBonus_HIT;
            statBonus_EVA = _statBonus_EVA;
            maxStamina = _maxSP;
            resistances = _resistances;
        }
    }
}