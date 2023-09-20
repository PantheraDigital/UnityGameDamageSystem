using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AttackPack
{
    public abstract class CombatVolumeBase : MonoBehaviour
    {
        //Volume type refers to the type of data that will be passed to objects detected by this volume
        public enum VolumeType
        {
            Hurtbox,
            Pushbox,
            All, //for when all attack data subclasses data can go through all types of volumes
            None //do not set any volumes to the type on None. This is ment for AttackData that will not use volumes. Case: apply force to self, not target.
        }

        protected LayerMask layerMask;
        protected CombatVolumeManagerBase manager;

        protected bool active;

        [SerializeField]
        protected VolumeType volumeType;
        public VolumeType VType
        {
            get => volumeType;
            set => volumeType = value;
        }


        public virtual void Initialize(CombatVolumeManagerBase manager, LayerMask layerMask)
        {
            this.manager = manager;
            this.layerMask = layerMask;

            if (manager == null)
                Debug.LogError(gameObject.name + " not properly initialized.");
        }

        public void Start()
        {
            if (manager == null)
            {
                this.enabled = false;
                Debug.LogError(gameObject.name + " not properly initialized. Add to a manager.");
            }
        }


        //detect hits till set false;
        public virtual void CombatVolumeSetActive(bool active)
        {
            this.active = active;
        }


        protected void SendObjectToManager(GameObject otherObject)
        {
            if (otherObject != null && otherObject != gameObject)
                manager.AddGameObjectToHitList(otherObject, volumeType);
        }

    }

}

