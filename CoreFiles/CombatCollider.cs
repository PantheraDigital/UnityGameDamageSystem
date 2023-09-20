using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace AttackPack
{
    //attach to a collider that will be used for combat
    // this behaviour allows collider to be detected by an attack 
    // this collider can be used to block or parry attacks
    // setting to Attack state will also allow for attack collisions
    // 
    // works on triggers or solid colliders
    //  a trigger is recommended if collider moves with attack volumes and physical collision is not wanted
    //  
    public class CombatCollider : MonoBehaviour, ICombatCollider, ITakeDamage
    {
        [SerializeField]
        ICombatCollider.State colliderState;
        [SerializeField]
        string groupID;
        [SerializeField]
        GameObject parent;


        public ICombatCollider.State ColliderState { get => colliderState; set => colliderState = value; }

        public GameObject GameObject => gameObject;

        public string GroupID { get => groupID; set => groupID = value; }
        public GameObject Parent { get => parent; set => parent = value; }

        public AttackDataComposite ApplyDamage(AttackDataComposite attack)
        {
            return null;
        }
    }

}