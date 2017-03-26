using UnityEngine;
using System.Collections;

namespace CnfBattleSys
{
    /// <summary>
    /// MonoBehaviour side of a battler.
    /// Doesn't do any battle-logic things - just gets messages from the Battler
    /// and handles gameObject movement/model/animations/etc.
    /// </summary>
    public class BattlerPuppet : MonoBehaviour
    {
        private Battler battler;
        public MeshRenderer meshRenderer;

        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}

