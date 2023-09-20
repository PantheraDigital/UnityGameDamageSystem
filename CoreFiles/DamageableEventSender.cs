using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AttackPack
{

    public class DamageableEventSender : MonoBehaviour, ITakeDamage
    {
        public delegate void OnHit(AttackDataComposite appliedComposite);
        public event OnHit onHit;

        public event OnHit onHitLateCall;//used for events that may need adjusted values after OnHit


        [SerializeField]
        string groupID = "null";
        [SerializeField]
        GameObject parent;

        [Space]
        [SerializeField]
        ResistanceStats<AttackType> attackTypeResistance;
        [SerializeField]
        ResistanceStats<DamageType> damageTypeResistance;


        public string GroupID { get => groupID; set => groupID = value; }
        public GameObject Parent { get => parent; set => parent = value; }

        public GameObject GameObject { get => gameObject; }


        public AttackDataComposite ApplyDamage(AttackDataComposite attack)
        {
            if (attack != null)
            {
                DamageData dmgData = attack.GetAttackData<DamageData>();
                DamageAdjustment(dmgData);

                onHit?.Invoke(attack);
                onHitLateCall?.Invoke(attack);
            }

            return attack;
        }

        void DamageAdjustment(DamageData damageData)
        {
            if (damageData == null)
                return;

            foreach (DamageType damageType in new List<DamageType>(damageData.DamageValues.Keys))
            {
                float dmg = damageData.DamageValues[damageType];

                if (!damageTypeResistance.IsEmpty())
                    dmg = damageTypeResistance.CalcDamage(dmg, damageType);

                if (!attackTypeResistance.IsEmpty())
                    dmg = attackTypeResistance.CalcDamage(dmg, damageData.AttackType);

                damageData.DamageValues[damageType] = dmg;
            }
        }

    }

}
