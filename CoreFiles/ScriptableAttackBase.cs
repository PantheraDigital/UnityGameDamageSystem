using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AttackPack
{

    //[CreateAssetMenu(fileName = "ScriptableAttackBase", menuName = "ScriptableObjects/ScriptableAttackBase")]
    public abstract class ScriptableAttackBase : ScriptableObject
    {
        public abstract AttackDataComposite GetAttackInfo(GameObject owner);
    }

}