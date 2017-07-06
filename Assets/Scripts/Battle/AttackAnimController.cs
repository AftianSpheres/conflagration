using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CnfBattleSys
{
    public class AttackAnimController : MonoBehaviour
    {
        public class Layer
        {
            public readonly LinkedListNode<Layer> myNode;
            public readonly Action callback;
            // what can we need here?
            // wait on eventblock events
            // or events dispatched by this controller
            // that's it.
            public readonly Func<bool> checkIfReadyToProgress;
            public readonly string subactionName;

            /// <summary>
            /// Fire off this layer's associated subaction.
            /// </summary>
            public bool FireSubaction ()
            {
                return BattleOverseer.currentBattle.actionExecutionSubsystem.FireSubaction(subactionName);
            }
        }

        private Layer currentLayer;
        private LinkedList<Layer> layers;

        void Update()
        {
            
        }
    }

}