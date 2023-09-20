using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace AttackPack
{
    //basic class usable for HP, shields, and any other measurment 
    // attach to object and use ID to differentiate between meters and their uses
    [System.Serializable]
    public class Meter
    {
        [SerializeField]
        string id;
        public string ID { get => id; }


        [SerializeField]
        float maxValue;
        public float MaxValue { get => maxValue; set => maxValue = value; }

        [SerializeField]
        bool startAtMax = true;

        public float CurrentValue { get; set; }


        public Meter()
        {
            Initialize();
        }
        public void Initialize()
        {
            if (startAtMax)
                CurrentValue = maxValue;
        }

        public void ResetValue()
        {
            CurrentValue = maxValue;
        }
    }
}
