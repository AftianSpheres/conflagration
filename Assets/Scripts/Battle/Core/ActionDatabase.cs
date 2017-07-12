using UnityEngine;
using System;
using System.Collections.Generic;
using GeneratedDatasets;

namespace CnfBattleSys
{
    /// <summary>
    /// Static class that stores and handles the action datatable, plus utilities for getting icons and etc. based on action ID.
    /// </summary>
    public static class ActionDatabase
    {
        public static int count = Enum.GetValues(typeof(ActionType)).Length - SpecialActions.count;
        private readonly static EventBlock emptyEventBlock = new EventBlock(new AnimEvent[0], new AudioEvent[0], new FXEvent[0], BattleCameraScriptType.None);
        const string actionIconsResourcePath = "Battle/2D/UI/AWIcon/Action/";

        /// <summary>
        /// Contains special-case action defs that exust outside of the main table we populate from the XML files.
        /// </summary>
        public static class SpecialActions
        {
            /// <summary>
            /// The number of special action defs. Since these have their own (less than zero) entries in the ActionType enum, we need to subtract the number of special actions from the total when determining how many spaces there need to be 
            /// </summary>
            public const int count = 2;

            /// <summary>
            /// The default battle action entry, used to populate invalid entries on the table or when we need a placeholder action entry somewhere else in the battle system.
            /// </summary>
            public static readonly BattleAction defaultBattleAction = new BattleAction(emptyEventBlock, emptyEventBlock, emptyEventBlock, ActionType.InvalidAction, 0, 0, 0, 0, 0, 0, TargetSideFlags.None, TargetSideFlags.None, 
                                                                                       ActionTargetType.None, ActionTargetType.None, BattleActionCategoryFlags.None, new BattleAction.Subaction[0]);

            /// <summary>
            /// Another empty placeholder battle action - all we care about with any of these placeholder actions is _identity_. They don't do anything.
            /// This actually gets plugged into the table, so don't count it as part of the special actions count above. None is index 0, not a negative index.
            /// </summary>
            public static readonly BattleAction noneBattleAction = new BattleAction(emptyEventBlock, emptyEventBlock, emptyEventBlock, ActionType.None, 0, 0, 0, 0, 0, 0, TargetSideFlags.None, TargetSideFlags.None, 
                                                                                    ActionTargetType.None, ActionTargetType.None, BattleActionCategoryFlags.None, new BattleAction.Subaction[0]);

            /// <summary>
            /// The entry for the "break own stance" entry, which is a placeholder just like the other two. We don't "execute" this action in the normal sense - 
            /// if you go into action execution with this action, you go through some hardcoded special-case behavior instead of executing an action def.
            /// </summary>
            public static readonly BattleAction selfStanceBreakAction = new BattleAction(emptyEventBlock, emptyEventBlock, emptyEventBlock, ActionType.INTERNAL_BreakOwnStance, 0, 0, 0, 0, 0, 0, TargetSideFlags.None, TargetSideFlags.None, 
                                                                                         ActionTargetType.None, ActionTargetType.None, BattleActionCategoryFlags.None, new BattleAction.Subaction[0]);
        }

        /// <summary>
        /// Gets a BattleAction corresponding to actionID from the dataset.
        /// </summary>
        public static BattleAction Get(ActionType actionID)
        {
            if (actionID > ActionType.None) return Datasets.battleActions[(int)actionID];
            else
            {
                switch (actionID)
                {
                    case ActionType.None:
                        return SpecialActions.noneBattleAction;
                    case ActionType.INTERNAL_BreakOwnStance:
                        return SpecialActions.selfStanceBreakAction;
                    default:
                        return SpecialActions.defaultBattleAction;
                }
            }
        }

        /// <summary>
        /// Returns the Sprite from Resources/Battle/2D/UI/AWIcon/Action corresponding to this ID, if one exists,
        /// or the placeholder graphic otherwise.
        /// </summary>
        public static Sprite GetIconForActionID (ActionType actionID)
        {
            Sprite iconSprite = Resources.Load<Sprite>(actionIconsResourcePath + actionID.ToString());
            if (iconSprite == null) iconSprite = Resources.Load<Sprite>(actionIconsResourcePath + ActionType.InvalidAction.ToString());
            if (iconSprite == null) Util.Crash(new Exception("Couldn't get invalid action icon placeholder"));
            return iconSprite;
        }
    }
}