using UnityEngine;
using System.Collections.Generic;
using MovementEffects;

namespace CnfBattleSys
{
    /// <summary>
    /// MonoBehaviour side of a battler.
    /// Doesn't do any battle-logic things - just gets messages from the Battler
    /// and handles gameObject movement/model/animations/etc.
    /// </summary>
    public class BattlerPuppet : MonoBehaviour
    {
        public Battler battler;
        public Animator animator;
        public MeshRenderer meshRenderer;
        public MeshFilter meshFilter;
        private BattlerModelType modelType;
        private Vector3 offset;
        private float stepTime;

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        /// <summary>
        /// Coroutine: lerps unit between original and final positions.
        /// speed is a float that - like stepTime - represents the amount of time it should take to move one unit.
        /// Normally speed = stepTime, but we might wanna eg. throw units around at speeds independent of their
        /// stepTime values.
        /// </summary>
        private IEnumerator<float> _Move (Vector3 moveVector, float speed, AnimEventType exitEvent)
        {
            float vd = 0;
            float distance = moveVector.magnitude;
            Vector3 op = transform.position;
            Vector3 np = transform.position + moveVector;
            while (vd < distance)
            {
                transform.position = Vector3.Lerp(op, np, vd / distance);
                vd += (1/speed) * Timing.DeltaTime * distance ;
                yield return 0;
            }
            SyncPosition();
            ProcessAnimEvent(exitEvent);
        }

        /// <summary>
        /// Processes move command. If we have an animation for moveEvent we move gradually across the playfield playing that animation;
        /// otherwise, we immediately SyncPosition().
        /// Plays exitEvent after the move event ends. Normally this is AnimEventType.Idle.
        /// Returns true if we were able to process the moveEvent.
        /// </summary>
        public bool ProcessMove(Vector3 moveVector, float speed, AnimEventType moveEvent, AnimEventType exitEvent)
        {
            bool r = ProcessAnimEvent(moveEvent);
            if (r == true)
            {
                Timing.RunCoroutine(_Move(moveVector, speed, exitEvent));
            }
            else
            {
                SyncPosition();
            }
            return r;
        }

        /// <summary>
        /// Processes 
        /// </summary>
        /// <param name="animEventType"></param>
        /// <returns></returns>
        public bool ProcessAnimEvent (AnimEventType animEventType)
        {
            bool r = false;
            for (int i = 0; i < animator.runtimeAnimatorController.animationClips.Length; i++)
            {
                string s = animEventType.ToString();
                if (animator.runtimeAnimatorController.animationClips[i].name == s)
                {
                    animator.Play(s);
                    r = true;
                    break;
                }
            }
            return r;
        }

        /// <summary>
        /// Updates position of GameObject w/ battler's logical position
        /// </summary>
        private void SyncPosition ()
        {
            transform.position = battler.logicalPosition + offset;
        }

        /// <summary>
        /// Loads our model from the Resources folder, based on the modelType
        /// </summary>
        private void LoadModel ()
        {
            const string bmPath = "Battle/Models/";
            const string meshPath = "/mesh";
            string myPath = modelType.ToString();
            Mesh mesh = Resources.Load<Mesh>(bmPath + myPath + meshPath);
            if (mesh == null) throw new System.Exception("Couldn't load battle model mesh: " + myPath + meshPath);
            meshFilter.mesh = mesh;
        }
    }

}

