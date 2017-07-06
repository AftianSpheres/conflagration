using System;
using System.Collections.Generic;
using UnityEngine;
using CnfBattleSys;

namespace ExtendedAnimationManagement
{
    /// <summary>
    /// Special StateMachineBehaviour that powers extended animator features. Automatically attached to all
    /// AnimatorController assets. Creates and attaches the animatorMetadataContainer,
    /// and handles the onStateChanged event.
    /// </summary>
    public class StateMachineExtender : StateMachineBehaviour
    {
        public event Action onStateChanged;

        /// <summary>
        /// Don't touch this! This is set automatically by the animator metadata table builder.
        /// </summary>
        public int tableIndex_SetAutomatically;

        /// <summary>
        /// StateMachineBehaviour.OnStateEnter ()
        /// </summary>
        override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            AnimatorMetadataContainer animatorMetadataContainer = animator.gameObject.GetComponent<AnimatorMetadataContainer>();
            if (animatorMetadataContainer == null) animatorMetadataContainer = animator.gameObject.AddComponent<AnimatorMetadataContainer>();
            if (animatorMetadataContainer.contents == null) animatorMetadataContainer.FillWith(AnimatorMetadataLookupTable.lookupTable[tableIndex_SetAutomatically], this);
            onStateChanged();
        }
    }

}