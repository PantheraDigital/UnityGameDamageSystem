using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AttackPack
{
    public enum LandingHitStatus
    {
        None
        , Miss
        , Landed
        , Block
        , Perried
    }

    public enum ObjectHitStatus
    {
        None
        , Normal
        , Hit 
        , Stunned
        , Finishable
    }


    //allows objects to be attacked
    // returns a modified AttackDataComposite representing landing damage effect
    // EX: if resistant to a damage type passed then it will return the amount of damage taken by that type
    //
    //  fire dmg: 10 -> ITakeDamage
    //  ITakeDamage fire resistance 50%
    //  fire dmg: 5 <- ITakeDamage
    public interface ITakeDamage
    {
        [SerializeField]
        string GroupID { get; set; }
        [SerializeField]
        GameObject Parent { get; set; }
        GameObject GameObject { get; }


        //used by other classes to send attack info to this obj
        AttackDataComposite ApplyDamage(AttackDataComposite attack);
    }


    public interface ICombatCollider
    {
        public enum State
        {
            None,
            Block,
            Parry,
            Attack
        }

        [SerializeField]
        State ColliderState { get; set; }

        GameObject GameObject { get; }
    }
}
