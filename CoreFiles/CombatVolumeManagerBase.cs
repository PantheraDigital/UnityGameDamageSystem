using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace AttackPack
{
    /*
     * Provides functions to activate and deactivate volumes controlled by this manager
     *  volumes managed may be one or many as well as a single type or different types for filtering of attack data
     *  
     *  
     */
    [System.Serializable]
    public abstract class CombatVolumeManagerBase : MonoBehaviour
    {
        /*
         * be careful not to over use the tag system as it is for internal use of this class allone
         * some data is better used outside this class rather than effecting this class
         * 
         * EX: it is better to send if an attack was blocked or parried through the event rather than adding tags such as 'blocked'
         *     this will allow for better use of the data outside of the class for things like block reactions or reducing taken damage when blocked
         */

        //This event is called after sending AttackDataComposite to a hit object.
        //The AttackDataComposite passed through this event is adjusted to represent the applied data after hit object applies possible adjustments such as resistances
        // can be used by other scripts on this object to get data about attack such as if blocked or amount of damage delt
        public delegate void OnHitObject(GameObject hitObject, ICombatCollider.State colliderState, AttackDataComposite appliedComposite);
        public event OnHitObject onHitObject;

#if UNITY_EDITOR
        [SerializeField]
        bool onDetectHitLog;
        [SerializeField]
        bool activationLog;
        [SerializeField]
        bool returnedCompositeLog;

        [Space]
#endif

        [SerializeField]
        protected LayerMask layerMask;
        
        [Tooltip("Filtering data will filter the attack composite data through the different active combat volumes based on their type.\nWithout filtering the whole composite will be used for every active combat volume." +
            "\n\nEnable to have the Hurtbox type volumes send damage data, and for Pushbox volumes to send the force data of an attack composite.")]
        [SerializeField]
        protected bool filterData;



        HitObjectContainer hitContainer;
        protected bool canClearHitList;

        protected bool damageSendingEnabled;
        protected AttackDataComposite currentAttack;
        protected bool keepAttack;

        bool hitNonAttackable;

        [SerializeField]
        List<GameObject> testItems;

        protected void Awake()
        {
            hitContainer = new HitList();
            hitContainer.FilterData = filterData;
            hitContainer.LogDebug = onDetectHitLog;

            /*float time = Time.realtimeSinceStartup;
            
            currentAttack = new AttackDataComposite( new DamageData( gameObject, AttackType.Light, new Dictionary<DamageType, float>(){ 
                { DamageType.Slash, 5f }, {DamageType.Fire, 10f } 
            } ) );
            currentAttack.Add(new ForceData(gameObject, Vector2.up, 10f, ForceMode.Impulse, false));
            EnableDamageSending(true);

Debug.Log("start time " + time);
            foreach (GameObject item in testItems)
            {
                AddGameObjectToHitList(item, CombatVolumeBase.VolumeType.Hurtbox);
                AddGameObjectToHitList(item, CombatVolumeBase.VolumeType.All);
            }

Debug.Log("end time " + Time.realtimeSinceStartup + "\nExecution time " + (Time.realtimeSinceStartup - time).ToString());

            EnableDamageSending(false);
            currentAttack = null;*/
        }

        //clear hit list in late update to allow all functions related to hit list to fire before clearing
        private void Update()
        {
            if (canClearHitList)
            {
                hitContainer.Clear();
                canClearHitList = false;
                hitNonAttackable = false;
            }
        }


        //set when volumes look for targets
        #region VolumeActivation
        public abstract void CVM_SetVolumesActive(CombatVolumeBase.VolumeType volumeType = CombatVolumeBase.VolumeType.All);

        public abstract void CVM_SetVolumesInactive();
        

        //use to activate for one frame
        public abstract void CVM_VolumeHitCheck(CombatVolumeBase.VolumeType volumeType = CombatVolumeBase.VolumeType.All);
        #endregion


        #region CombatVolumeFunctions
        //volumes call this function to report found objects
        public void AddGameObjectToHitList(GameObject hitObject, CombatVolumeBase.VolumeType volumeTypeOfSender)
        {
            if (hitObject != null && currentAttack != null && damageSendingEnabled)
            {
                ITakeDamage damageInterface = hitObject.GetComponent<ITakeDamage>();
                ICombatCollider combatColliderInterface = hitObject.GetComponent<ICombatCollider>();

                if (damageInterface != null || combatColliderInterface != null)
                {
                    AttackDataComposite attackDataComposite = null;
                    ICombatCollider.State colliderState = ICombatCollider.State.None;
#if UNITY_EDITOR
                    string debug = "";
                    if (onDetectHitLog)
                        debug += "<b>OnDetectHit</b> object <b><i>" + hitObject.name + "</i>.</b>";
#endif
                    //get collider state
                    //add to hitObjects if no ITakeDamage interface
                    if (combatColliderInterface != null)
                    {
                        colliderState = combatColliderInterface.ColliderState;

                        if (damageInterface == null)
                            hitContainer.AddObject(combatColliderInterface.GameObject);

#if UNITY_EDITOR
                        if (onDetectHitLog)
                        { 
                            debug += "\tICombatCollider: " + colliderState.ToString();
                            if (damageInterface == null)
                                debug += "\n\t" + hitContainer.ToString();
                        }
#endif
                    }

                    if (damageInterface != null)
                    {
#if UNITY_EDITOR
                        bool damageInterfaceAdded = false;
                        if (onDetectHitLog)
                        {
                            debug += "\tITakeDamage: ";
                            debug += hitContainer.AddObject(damageInterface, volumeTypeOfSender, out damageInterfaceAdded);
                            debug += "\n\t" + hitContainer.ToString();
                        }
                        else
                            hitContainer.AddObject(damageInterface, volumeTypeOfSender, out damageInterfaceAdded);
#else
                        hitContainer.AddObject(damageInterface, volumeTypeOfSender, out bool damageInterfaceAdded);
#endif
                        if (damageInterfaceAdded)
                        {
                            /*
                             * apply damage if object is not in hitlist or has special tag
                             * fire event
                             */
#if UNITY_EDITOR
                            AttackDataComposite composite;
                            if (filterData)
                                composite = currentAttack.SubComposite(volumeTypeOfSender);
                            else
                                composite = currentAttack;

                            attackDataComposite = damageInterface.ApplyDamage(composite);
                            if (onDetectHitLog)
                            {
                                debug += "\nFrom " + volumeTypeOfSender + " " + composite.ToString();
                            }
#else
                            if (filterData)
                                attackDataComposite = damageInterface.ApplyDamage(currentAttack.SubComposite(volumeTypeOfSender));
                            else
                                attackDataComposite = damageInterface.ApplyDamage(currentAttack);
#endif
                        }

                    }
#if UNITY_EDITOR
                    if (onDetectHitLog)
                        Debug.Log(debug);

                    if (returnedCompositeLog)
                        Debug.Log(hitObject.name + " Returned Composite: " + attackDataComposite.ToString());
#endif

                    onHitObject?.Invoke(hitObject, colliderState, attackDataComposite);
                }
                else
                {
                    hitNonAttackable = hitContainer.AddObject(hitObject);
#if UNITY_EDITOR
                    if (onDetectHitLog)
                        Debug.Log("<b>OnDetectHit</b> object <b><i>" + hitObject.name + "</i>.</b> Does not implement a useable interface.\n\t" + hitContainer.ToString());
#endif
                }
            }
        }
#endregion


#region ExternalControllFunctions
        public abstract void ClearVolumes();

        public abstract void AddVolume(CombatVolumeBase combatVolume);

        protected void EnableDamageSending(bool enabled)
        {
            damageSendingEnabled = enabled;

            //clear hit list at end of attack
            if (enabled == false)
            {
                canClearHitList = true;
                if (!keepAttack)
                    currentAttack = null;
            }
#if UNITY_EDITOR
            if(activationLog)
            {
                if (currentAttack != null)
                    Debug.Log(gameObject.name + " enabled: " + enabled + "  with attack: " + currentAttack.ToString());
                else
                    Debug.Log(gameObject.name + " enabled: " + enabled + "  with attack: null");
            }
#endif
        }

        public void SetCurrentAttack(AttackDataComposite attackDataComposite, bool keepAttack = false)
        {
            currentAttack = attackDataComposite;
            this.keepAttack = keepAttack;
        }

        //check if hitList holds any objects that do not implement ITakeDamage or ICombatCollider
        public bool HitNonAttackable()
        {
            return hitNonAttackable;
        }
#endregion
    }
}