using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AttackPack
{
    [System.Serializable]
    public class ResistanceStats<T>
    {
        [System.Serializable]
        public struct ResistancePair
        {
            public T resistType;
            public float resistancePercent;
        }
        [SerializeField]
        ResistancePair[] resistancePairs;//used only to initalize and for setting values in the inspector

        Dictionary<T, float> resistanceValues;


        public void Initialize()
        {
            if (resistancePairs.Length == 0)
                return;

            if (resistanceValues == null)
                resistanceValues = new Dictionary<T, float>();


            foreach (ResistancePair resistancePair in resistancePairs)
            {
                resistanceValues.Add(resistancePair.resistType, resistancePair.resistancePercent);
            }
        }

        public void AddResistance(T damageType, float resistancePercent)
        {
            if (resistanceValues == null)
                resistanceValues = new Dictionary<T, float>();

            resistanceValues.Add(damageType, resistancePercent);
        }
        public float GetResistance(T damageType)
        {
            if (resistanceValues.ContainsKey(damageType))
                return resistanceValues[damageType];
            else
                return 0f;
        }
        public bool IsEmpty()
        {
            if (resistanceValues == null || resistanceValues.Count == 0)
                return true;
            else
                return false;
        }

        public float CalcDamage(float damage, T damageType)
        {
            if (!resistanceValues.ContainsKey(damageType))
                return damage;
            else if (resistanceValues[damageType] == 100f)
                return 0f;
            else
                return damage - (damage * (resistanceValues[damageType] * 0.01f));
        }

        public override string ToString()
        {
            if (IsEmpty())
                return "empty";


            string output = "";
            foreach (T damageType in resistanceValues.Keys)
            {
                output += damageType.ToString() + ": " + resistanceValues[damageType].ToString() + "% | ";
            }

            return output;
        }
    }
}