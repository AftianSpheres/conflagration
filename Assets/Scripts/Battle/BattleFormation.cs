using UnityEngine;

namespace CnfBattleSys
{
    /// <summary>
    /// Another one of these PODS classes that we use to populate a lookup table.
    /// This one contains all the data you need to load into a single battle.
    /// Enemy party, venue, bgm, etc.
    /// </summary>
    public class BattleFormation
    {
        /// <summary>
        /// Struct that stores battlerData reference, fieldPosition, and side for a single unit.
        /// </summary>
        public struct FormationMember
        {
            public readonly BattlerData battlerData;
            public readonly Vector2 fieldPosition;
            public readonly BattleStance startStance;
            public readonly BattlerSideFlags side;

            /// <summary>
            /// Constructor. Should only be called by FormationDatabase.Load()
            /// </summary>
            public FormationMember(BattlerData _battler, Vector2 _fieldPosition, BattleStance _startStance, BattlerSideFlags _side)
            {
                battlerData = _battler;
                fieldPosition = _fieldPosition;
                startStance = _startStance;
                side = _side;
                if (!BattleUtility.IsSingleSide(side)) throw new System.Exception("Created a FormationMember with multiple sides. Fix that!");
            }
        }

        public readonly FormationType formation;
        public readonly VenueType venue;
        public readonly BGMTrackType bgmTrack;
        public readonly BattleFormationFlags flags;
        public readonly Vector2 fieldBounds; // field is ovoid
        public readonly FormationMember[] battlers;

        /// <summary>
        /// Constructor. Should only be called by FormationDatabase.Load()
        /// </summary>
        public BattleFormation (FormationType _formation, VenueType _venue, BGMTrackType _bgmTrack, BattleFormationFlags _flags, Vector2 _fieldBounds, FormationMember[] _battlers)
        {
            formation = _formation;
            venue = _venue;
            bgmTrack = _bgmTrack;
            flags = _flags;
            fieldBounds = _fieldBounds;
            battlers = _battlers;
        }
    }
}