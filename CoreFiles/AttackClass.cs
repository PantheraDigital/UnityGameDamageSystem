using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AttackPack
{
    //greatest container of attack data for attacker
    [System.Serializable]
    public class AttackBody
    {
        [SerializeField]
        AnimationClip animationClip;
        [SerializeField]
        AttackDataComposite attackData;

        public AttackDataComposite AttackDataComposite
        {
            get => attackData;
        }

        public AttackBody(AnimationClip animationClip, AttackDataComposite attackDataComposite)
        {
            this.attackData = attackDataComposite;
            this.animationClip = animationClip;
        }


        #region GettersAndSetters

        public string GetAnim()
        {
            if (animationClip)
                return animationClip.name;
            else
                return "Null";
        }

        #endregion
    }

}