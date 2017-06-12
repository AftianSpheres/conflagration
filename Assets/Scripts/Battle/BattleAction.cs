namespace CnfBattleSys
{
    /// <summary>
    /// Defines a single battle action.
    /// </summary>
    public class BattleAction
    {
        /// <summary>
        /// Defines the Subactions that comprise this action.
        /// Each Subaction is a single "event."
        /// We can chain multiple Subactions together for (ex:) multi-hit attacks,
        /// or complex effects that combine multiple buffs/debuffs/whatever.
        /// Every Action needs to have at least one Subaction associated with it!
        /// Many will only have one; that's okay.
        /// </summary>
        public class Subaction
        {
            /// <summary>
            /// Defines the type and parameters of non-damaging effects that a Subaction inflicts on its targets.
            /// </summary>
            public class EffectPackage
            {
                public readonly EventBlock eventBlock;
                public readonly SubactionEffectType effectType;
                public readonly LogicalStatType hitStat;
                public readonly LogicalStatType evadeStat;
                public readonly bool applyEvenIfSubactionMisses;
                public readonly float baseSuccessRate; // if this >= 1.0 we should just skip the succeed/fail checks entirely!!!
                public readonly float length_Float; // depending on the effect you're applying, length/strength may need to be represented as either float or int, and float-to-int casting is nasty
                public readonly float strength_Float;
                public readonly byte length_Byte;
                public readonly int strength_Int;
                /// <summary>
                /// Must be lower than the EffectPackage's index in the Subaction.effectPackages array. 
                /// If greater than -1, we skip the normal FX success/fail checks here
                /// and just base things on whether or not the FX at the specified index
                /// was able to fire off successfully.
                /// This lets you build complex effects like multi-stat buffs/debuffs or what have you.
                /// NOTE FOR SELF: don't forget - you need to keep an index-matched array of passes/fails around
                /// for each target in the current Subaction!
                /// </summary>
                public readonly sbyte tieSuccessToEffectIndex;
                /// <summary>
                /// This is the base value the AI uses to score successful executions of this fx package.
                /// This isn't used for anything else.
                /// The most useful way to think of this is as the amount of damage or healing, as a percentage of target HP, that this effect is "worth" if it executes successfully.
                /// So, ex: you have a score of 20, you're saying "treat this as equal in value to dealing 20% of the target's HP in damage."
                /// You have a score of -45.15, you're saying that it's (presumably) a buff, and it's a buff that's equal in value to a _heal_ of 45.15% of target HP.
                /// </summary>
                public readonly float baseAIScoreValue;

                /// <summary>
                /// FXPackage constructor.
                /// This should never be called outside of ActionDataset.LoadData()!
                /// </summary>
                public EffectPackage(EventBlock _eventBlock, SubactionEffectType _fxType, LogicalStatType _fxHitStat, LogicalStatType _fxEvadeStat,
                                 bool _applyEvenIfSubactionMisses, float _baseSuccessRate, float _fxLength_Float, float _fxStrength_Float,
                                 byte _fxLength_Byte, int _fxStrength_Int, sbyte _thisFXSuccessTiedToFXAtIndex, float _baseAIScoreValue)
                {
                    eventBlock = _eventBlock;
                    effectType = _fxType;
                    hitStat = _fxHitStat;
                    evadeStat = _fxEvadeStat;
                    applyEvenIfSubactionMisses = _applyEvenIfSubactionMisses;
                    baseSuccessRate = _baseSuccessRate;
                    length_Float = _fxLength_Float;
                    strength_Float = _fxStrength_Float;
                    length_Byte = _fxLength_Byte;
                    strength_Int = _fxStrength_Int;
                    tieSuccessToEffectIndex = _thisFXSuccessTiedToFXAtIndex;
                    baseAIScoreValue = _baseAIScoreValue;
                }
            }

            public readonly EventBlock eventBlock;
            /// <summary>
            /// If baseDamage is less than zero, this heals the target.
            /// If baseDamage is nonzero and damage is tied to another Subaction, we multiply the damage from that Subaction by baseDamage.
            /// If baseDamage is zero, this is a non-damaging Subaction; we skip damage/accuracy checks and go straight to fx.
            /// </summary>
            public readonly int baseDamage;
            public readonly float baseAccuracy;
            public readonly bool useAlternateTargetSet;
            public readonly LogicalStatType atkStat;
            public readonly LogicalStatType defStat;
            public readonly LogicalStatType hitStat;
            public readonly LogicalStatType evadeStat;
            public readonly DamageTypeFlags damageTypes;
            /// <summary>
            /// The categoryFlags of a subaction determine if that subaction is considered by the AI system
            /// when calculating that category score.
            /// These should always be a subset of the action's own categoryFlags; we can't do anything with categoryFlags that _aren't_ shared.
            /// If the action doesn't identify itself as a buff, we never bother scoring subactions as buffs!
            /// </summary>
            public readonly BattleActionCategoryFlags categoryFlags;

            /// <summary>
            /// Must be lower than the Subaction's index in the BattleAction.Subactions array.
            /// If greater than -1, we skip damage calculation and just use the per-target
            /// final damage figures of the Subaction at the specified index.
            /// </summary>
            public readonly sbyte thisSubactionDamageTiedToSubactionAtIndex;
            /// <summary>
            /// Must be lower than the Subaction's index in the BattleAction.Subactions array.
            /// If greater than -1, we skip damage calculation and just use the per-target
            /// success values of the Subaction at the specified index.
            /// </summary>
            public readonly sbyte thisSubactionSuccessTiedToSubactionAtIndex;
            public readonly EffectPackage[] effectPackages;

            /// <summary>
            /// Subaction constructor.
            /// This should never be called outside of ActionDataset.LoadData()!
            /// </summary>
            public Subaction(EventBlock _eventBlock, int _baseDamage, float _baseAccuracy, bool _useAlternateTargetSet,
                             LogicalStatType _atkStat, LogicalStatType _defStat, LogicalStatType _hitStat, LogicalStatType _evadeStat,
                             sbyte _thisSubactionDamageTiedToSubactionAtIndex, sbyte _thisSubactionSuccessTiedToSubactionAtIndex,
                             BattleActionCategoryFlags _categoryFlags, EffectPackage[] _fx, DamageTypeFlags _damageTypes)
            {
                eventBlock = _eventBlock;
                baseDamage = _baseDamage;
                baseAccuracy = _baseAccuracy;
                useAlternateTargetSet = _useAlternateTargetSet;
                atkStat = _atkStat;
                defStat = _defStat;
                hitStat = _hitStat;
                evadeStat = _evadeStat;
                thisSubactionDamageTiedToSubactionAtIndex = _thisSubactionDamageTiedToSubactionAtIndex;
                thisSubactionSuccessTiedToSubactionAtIndex = _thisSubactionSuccessTiedToSubactionAtIndex;
                categoryFlags = _categoryFlags;
                effectPackages = _fx;
                damageTypes = _damageTypes;
            }
        }

        public readonly EventBlock animSkip;
        public readonly EventBlock onConclusion;
        public readonly EventBlock onStart;
        public readonly ActionType actionID;
        public readonly float baseAOERadius;
        public readonly float baseDelay;
        public readonly float baseFollowthroughStanceChangeDelay;
        public readonly float baseMinimumTargetingDistance;
        public readonly float baseTargetingRange;
        public readonly byte baseSPCost;
        /// <summary>
        /// A second TargetSideFlags. Subactions that act on the alternate target set will act on targets selected from these units.
        /// </summary>
        public readonly TargetSideFlags alternateTargetSideFlags;
        public readonly TargetSideFlags targetingSideFlags;
        /// <summary>
        /// A second ActionTargetType. Subactions that act on the alternate target set will act on units acquired this way.
        /// </summary>
        public readonly ActionTargetType alternateTargetType;
        public readonly ActionTargetType targetingType;
        public readonly BattleActionCategoryFlags categoryFlags;
        public readonly Subaction[] Subactions;

        /// <summary>
        /// Constructs a BattleAction struct, given, uh, the entire contents of the BattleAction struct.
        /// This should never be called outside of ActionDatabase.Load()!
        /// </summary>
        public BattleAction(EventBlock _animSkip, EventBlock _onConclusion, EventBlock _onStart, ActionType _actionID, float _baseAOERadius, float _baseDelay, float _baseFollowthroughStanceChangeDelay, float _baseMinimumTargetingDistance, 
            float _basetargetingRange, byte _baseSPCost, TargetSideFlags _alternateTargetSideFlags, TargetSideFlags _targetingSideFlags, ActionTargetType _alternateTargetType, ActionTargetType _targetingType,
            BattleActionCategoryFlags _categoryFlags, Subaction[] _Subactions)
        {
            if (_Subactions.Length == 0) Util.Crash(new System.Exception("Tried to create a battle Action with no Subactions, which should never happen."));
            animSkip = _animSkip;
            onConclusion = _onConclusion;
            onStart = _onStart;
            actionID = _actionID;
            baseAOERadius = _baseAOERadius;
            baseDelay = _baseDelay;
            baseFollowthroughStanceChangeDelay = _baseFollowthroughStanceChangeDelay;
            baseMinimumTargetingDistance = _baseMinimumTargetingDistance;
            baseTargetingRange = _basetargetingRange;
            baseSPCost = _baseSPCost;
            targetingSideFlags = _targetingSideFlags;
            targetingType = _targetingType;
            categoryFlags = _categoryFlags;
            Subactions = _Subactions;
        }
    }
}