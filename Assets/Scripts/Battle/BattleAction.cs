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
            public class FXPackage
            {
                public readonly SubactionFXType fxType;
                public readonly LogicalStatType fxHitStat;
                public readonly LogicalStatType fxEvadeStat;
                public readonly bool applyEvenIfSubactionMisses;
                public readonly float baseSuccessRate; // if this >= 1.0 we should just skip the succeed/fail checks entirely!!!
                public readonly float fxLength_Float; // depending on the effect you're applying, length/strength may need to be represented as either float or int, and float-to-int casting is nasty
                public readonly float fxStrength_Float;
                public readonly byte fxLength_Byte;
                public readonly int fxStrength_Int;
                /// <summary>
                /// Must be lower than the FXPackage's index in the Subaction.fx array. 
                /// If greater than -1, we skip the normal FX success/fail checks here
                /// and just base things on whether or not the FX at the specified index
                /// was able to fire off successfully.
                /// This lets you build complex effects like multi-stat buffs/debuffs or what have you.
                /// NOTE FOR SELF: don't forget - you need to keep an index-matched array of passes/fails around
                /// for each target in the current Subaction!
                /// </summary>
                public readonly sbyte thisFXSuccessTiedToFXAtIndex;

                /// <summary>
                /// FXPackage constructor.
                /// This should never be called outside of ActionDataset.LoadData()!
                /// </summary>
                public FXPackage(SubactionFXType _fxType, LogicalStatType _fxHitStat, LogicalStatType _fxEvadeStat,
                                 bool _applyEvenIfSubactionMisses, float _baseSuccessRate, float _fxLength_Float, float _fxStrength_Float,
                                 byte _fxLength_Byte, int _fxStrength_Int, sbyte _thisFXSuccessTiedToFXAtIndex)
                {
                    fxType = _fxType;
                    fxHitStat = _fxHitStat;
                    fxEvadeStat = _fxEvadeStat;
                    applyEvenIfSubactionMisses = _applyEvenIfSubactionMisses;
                    baseSuccessRate = _baseSuccessRate;
                    fxLength_Float = _fxLength_Float;
                    fxStrength_Float = _fxStrength_Float;
                    fxLength_Byte = _fxLength_Byte;
                    fxStrength_Int = _fxStrength_Int;
                    thisFXSuccessTiedToFXAtIndex = _thisFXSuccessTiedToFXAtIndex;
                }
            }

            /// <summary>
            /// If baseDamage is less than zero, this heals the target.
            /// If baseDamage is nonzero and damage is tied to another Subaction, we multiply the damage from that Subaction by baseDamage.
            /// If baseDamage is zero, this is a non-damaging Subaction; we skip damage/accuracy checks and go straight to fx.
            /// </summary>
            public readonly int baseDamage;
            public readonly float baseAccuracy;
            public readonly bool useAlternateTargetSet;
            public readonly AnimEventType onSubactionHitTargetAnim;
            public readonly AnimEventType onSubactionExecuteUserAnim;
            public readonly LogicalStatType atkStat;
            public readonly LogicalStatType defStat;
            public readonly LogicalStatType hitStat;
            public readonly LogicalStatType evadeStat;
            public readonly DamageTypeFlags damageTypes;

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
            public readonly FXPackage[] fx;

            /// <summary>
            /// Subaction constructor.
            /// This should never be called outside of ActionDataset.LoadData()!
            /// </summary>
            public Subaction(int _baseDamage, float _baseAccuracy, bool _useAlternateTargetSet,
                             AnimEventType _onSubactionHitTargetAnim, AnimEventType _onSubactionExecuteUserAnim,
                             LogicalStatType _atkStat, LogicalStatType _defStat, LogicalStatType _hitStat, LogicalStatType _evadeStat,
                             sbyte _thisSubactionDamageTiedToSubactionAtIndex, sbyte _thisSubactionSuccessTiedToSubactionAtIndex,
                             FXPackage[] _fx, DamageTypeFlags _damageTypes)
            {
                baseDamage = _baseDamage;
                baseAccuracy = _baseAccuracy;
                useAlternateTargetSet = _useAlternateTargetSet;
                onSubactionHitTargetAnim = _onSubactionHitTargetAnim;
                onSubactionExecuteUserAnim = _onSubactionExecuteUserAnim;
                atkStat = _atkStat;
                defStat = _defStat;
                hitStat = _hitStat;
                evadeStat = _evadeStat;
                thisSubactionDamageTiedToSubactionAtIndex = _thisSubactionDamageTiedToSubactionAtIndex;
                thisSubactionSuccessTiedToSubactionAtIndex = _thisSubactionSuccessTiedToSubactionAtIndex;
                fx = _fx;
                damageTypes = _damageTypes;
            }
        }

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
        public readonly AnimEventType animSkipTargetHitAnim;
        public readonly AnimEventType onActionEndTargetAnim;
        public readonly AnimEventType onActionEndUserAnim;
        public readonly AnimEventType onActionUseTargetAnim;
        public readonly AnimEventType onActionUseUserAnim;
        public readonly Subaction[] Subactions;

        /// <summary>
        /// Constructs a BattleAction struct, given, uh, the entire contents of the BattleAction struct.
        /// This should never be called outside of ActionDatabase.Load()!
        /// </summary>
        public BattleAction(ActionType _actionID, float _baseAOERadius, float _baseDelay, float _baseFollowthroughStanceChangeDelay, float _baseMinimumTargetingDistance, float _basetargetingRange,
            byte _baseSPCost, TargetSideFlags _alternateTargetSideFlags, TargetSideFlags _targetingSideFlags, ActionTargetType _alternateTargetType, ActionTargetType _targetingType, AnimEventType _animSkipTargetHitAnim,
            AnimEventType _onActionEndTargetAnim, AnimEventType _onActionEndUserAnim, AnimEventType _OnActionUseTargetAnim, AnimEventType _OnActionUseUserAnim,
            Subaction[] _Subactions)
        {
            if (_Subactions.Length == 0) throw new System.Exception("Tried to create a battle Action with no Subactions, which should never happen.");
            actionID = _actionID;
            baseAOERadius = _baseAOERadius;
            baseDelay = _baseDelay;
            baseFollowthroughStanceChangeDelay = _baseFollowthroughStanceChangeDelay;
            baseMinimumTargetingDistance = _baseMinimumTargetingDistance;
            baseTargetingRange = _basetargetingRange;
            baseSPCost = _baseSPCost;
            targetingSideFlags = _targetingSideFlags;
            targetingType = _targetingType;
            animSkipTargetHitAnim = _animSkipTargetHitAnim;
            onActionEndTargetAnim = _onActionEndTargetAnim;
            onActionEndUserAnim = _onActionEndUserAnim;
            onActionUseTargetAnim = _OnActionUseTargetAnim;
            onActionUseUserAnim = _OnActionUseUserAnim;
            Subactions = _Subactions;
        }
    }
}