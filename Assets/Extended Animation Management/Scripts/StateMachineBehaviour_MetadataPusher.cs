using UnityEngine;

namespace ExtendedAnimationManagement
{
    /// <summary>
    /// The StateMachineBehaviour that creates and attaches the animatorMetadataContainer.
    /// This is automatically destroyed once that happens.
    /// </summary>
    public class StateMachineBehaviour_MetadataPusher : StateMachineBehaviour
    {
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
            animatorMetadataContainer.FillWith(AnimatorMetadataLookupTable.lookupTable[tableIndex_SetAutomatically]);
            Destroy(this);
        }
    }

}