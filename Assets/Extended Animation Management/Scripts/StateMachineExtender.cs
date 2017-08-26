using System;
using UnityEngine;

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
        private bool tripped = false;

        /// <summary>
        /// Don't touch this! This is set automatically by the animator metadata table builder.
        /// </summary>
        public int tableIndex_SetAutomatically;

        /// <summary>
        /// StateMachineBehaviour.OnStateEnter ()
        /// </summary>
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (!tripped) Trip(animator);
        }

        /// <summary>
        /// StateMachineBehaviour.OnStateMachineEnter ()
        /// </summary>
        public override void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
        {
            if (!tripped) Trip(animator);
        }

        /// <summary>
        /// Links to metadata container.
        /// Should never run more than once per instance.
        /// </summary>
        private void Trip (Animator animator)
        {
            AnimatorMetadataContainer animatorMetadataContainer = animator.gameObject.GetComponent<AnimatorMetadataContainer>();
            if (animatorMetadataContainer == null) animatorMetadataContainer = animator.gameObject.AddComponent<AnimatorMetadataContainer>();
            if (animatorMetadataContainer.contents == null) animatorMetadataContainer.FillWith(AnimatorMetadataLookupTable.lookupTable[tableIndex_SetAutomatically], this);
            onStateChanged?.Invoke();
            tripped = true;
        }
    }

}