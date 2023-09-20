using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace AttackPack
{
    //attack container is only responsible for providing an attack to attack script
    // container can be simple or as complex as needed
    // can be a simple 3 attack string or a tree of attacks dependent on inputs and move states

    [System.Serializable]
    public class SimpleAttackContainer 
    {

        [System.Serializable]
        public struct SerializedAttackData
        {
            public AttackKey attackKey;
            public AnimationClip animationClip;
            public ScriptableAttackBase attackScriptable;
        }

        [System.Serializable]
        public struct AttackKey
        {
            public AttackType attackType;
            public int index;
        }


        [SerializeField]
        SerializedAttackData[] attackKeys;

        Dictionary<AttackKey, AttackBody> attackContainer;
        int currentIndex;
        int maxIndex;


        public void Initialize(GameObject attackOwner)
        {
            if (attackKeys.Length > 0)
            {
                attackContainer = new Dictionary<AttackKey, AttackBody>();

                foreach (SerializedAttackData attackData in attackKeys)
                {
                    if (maxIndex < attackData.attackKey.index)
                        maxIndex = attackData.attackKey.index;

                    attackContainer.Add(attackData.attackKey, new AttackBody(attackData.animationClip, attackData.attackScriptable.GetAttackInfo(attackOwner)));
                }
            }
        }

        public AttackBody GetAttack(AttackType attackType)
        {
            if (currentIndex > maxIndex)
                currentIndex = 0;

            AttackKey tempKey;
            tempKey.attackType = attackType;
            tempKey.index = currentIndex;

            if (attackContainer.ContainsKey(tempKey))
            {
                currentIndex++;
                return attackContainer[tempKey];
            }
            else
                return null;

        }

        public void ResetIndex()
        {
            currentIndex = 0;
        }
    }

}