using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace AttackPack
{
    [System.Serializable]
    public enum DamageType
    {
        None
        , Physical
        , Slash
        , Fire
        , Blunt
    }

    //this describes the type of attack but is also used by SimpleAttackContainer and AttackScript to get attacks based on input
    // input is tied to attack types
    // this can be changed by creating an attack container that associates attacks with something other than attack type such as attack direction
    //  or by changing AttackType enum to hold desired data like UpAttack and RightAttack in the case of directional based attacks if light and heavy is not wanted
    public enum AttackType 
    {
        None
        , Light
        , Heavy
        , Special
    }

    /*
     * A container of AttackData in which AttackData acts as modules
     *  0-n AttackDatas may be stored to create a composite that will be passed to target object
     *  
     * AttackData is stored based on ID value
     *  Only one AttackData per ID. If a mathcing ID is found it will be overriden.
     */
    public class AttackDataComposite
    {
        AttackData singleComponent;
        List<AttackData> dataList;

        public AttackDataComposite()
        {
            singleComponent = null;
            dataList = null;
        }
        public AttackDataComposite(AttackData attackData, bool deepCopyData = false)
        {
            if(attackData != null)
            {
                if (deepCopyData)
                    singleComponent = attackData.DeepCopy();
                else
                    singleComponent = attackData;
            }
        }
        public AttackDataComposite(AttackData[] attackData, bool deepCopyData = false)
        {
            if(attackData == null)
            {
                singleComponent = null;
                dataList = null;
                return;
            }    

            if(attackData.Length == 1)
            {
                if (deepCopyData)
                    singleComponent = attackData[0].DeepCopy();
                else
                    singleComponent = attackData[0];
            }
            else
            {
                dataList = new List<AttackData>(attackData.Length);
                for(int i = 0; i < attackData.Length; ++i)
                {
                    if (attackData[i] != null)
                    {
                        if (deepCopyData)
                            Add(attackData[i].DeepCopy());
                        else
                            Add(attackData[i]);
                    }
                }
            }
        }

        public override string ToString()
        {
            if (singleComponent != null)
                return ("Component:\n" + "    " + singleComponent.ToString());
            else if (dataList != null)
            {
                string output = "Components:\n";
                foreach (AttackData data in dataList)
                    output += "    " + data.ToString() + "\n";

                return output;
            }
            else
                return "No Components.";
        }
        
        public void Add(AttackData attackData)
        {
            if (attackData == null)
                return;


            if (singleComponent == null && dataList == null)
                singleComponent = attackData;
            else
            {
                if(dataList == null)
                {
                    dataList = new List<AttackData>(3);
                    dataList.Add(singleComponent);
                    singleComponent = null;
                }

                int index = AttackIDIndex(dataList.ToArray(), attackData.ID);
                if (index < 0)
                {
                    dataList.Add(attackData);
                    dataList.Sort((x, y) => x.ID.CompareTo(y.ID));
                }
                else
                {
                    dataList[index] = attackData;
                }
            }
        }
        
        public bool ContainsID(string attackDataID)
        {
            int num = AttackIDIndex(dataList.ToArray(), attackDataID);
            return num >= 0;
        }

        /*
         * This region is for direct adjustment of AttackData in composite and should only be used if changing values
         * Data retrieved will NOT be copies
         */
        #region DirectDataAccess
        public AttackData GetAttackData(string id)
        {
            if (singleComponent != null)
            {
                if (singleComponent.ID == id)
                    return singleComponent;
                else
                    return null;
            }
            else if (dataList != null)
            {
                int index = AttackIDIndex(dataList.ToArray(), id);
                if (index >= 0)
                    return dataList[index];
            }
            
            return null;
        }

        /*
         * Used for getting AttackData of a specific type with a specific ID and cast it to it's type before returning.
         * EX: 
         *  AttackDataComposite attack;
         *  ForceData forceData = attack.GetAttackData<ForceData>("ForceData_Self");
         *  
         * This is useful if multiple AttackData subclasses of the same type exist in composite with different IDs.
         */
        public T GetAttackData<T>(string id) where T:AttackData
        {
            AttackData temp = GetAttackData(id);
            if (temp == null)
                return null;

            return temp as T;
        }
        /*
         * Get AttackData subclass which has an ID that matches its type.
         * EX:
         *  AttackDataComposite attack;
         *  DamageData dmgData = attack.GetAttackData<DamageData>();
         *  
         *  Here the ID of DamageData is "DamageData"
         */
        public T GetAttackData<T>() where T : AttackData
        {
            string[] names = typeof(T).ToString().Split('.');
            return GetAttackData<T>(names[^1]);
        }
        
        public AttackData[] GetAllAttackData()
        {
            if (singleComponent != null)
            {
                return new AttackData[1] { singleComponent };
            }
            else if (dataList != null && dataList.Count > 0)
            {
                AttackData[] temp = new AttackData[dataList.Count];
                int index = 0;

                foreach (AttackData data in dataList)
                {
                    temp[index] = data;
                    ++index;
                }

                return temp;
            }
            else
                return null;
        }
        public AttackData[] GetAllAttackData(CombatVolumeBase.VolumeType combatVolumePipeline)
        {
            if (singleComponent != null && (singleComponent.CombatVolumePipeline == combatVolumePipeline || combatVolumePipeline == CombatVolumeBase.VolumeType.All))
                return new AttackData[1] { singleComponent };
            else if (dataList != null && dataList.Count > 0)
            {
                AttackData[] temp = new AttackData[dataList.Count];
                int index = 0;

                foreach (AttackData data in dataList)
                {
                    if(data.CombatVolumePipeline == combatVolumePipeline || combatVolumePipeline == CombatVolumeBase.VolumeType.All)
                    {
                        temp[index] = data;
                        ++index;
                    }
                }

                return TrimNull(temp);
            }
            else
                return null;
        }
        #endregion

        /*
         * This region contains similar functions to DirectDataAccess region but only returns Deep Copies
         *  to prevent changes to the original AttackData in composite
         */
        #region DataCopy

        //create a new object that is exactly the same
        public AttackDataComposite DeepCopy()
        {
            AttackDataComposite dataComposite = new AttackDataComposite();
            if (singleComponent != null)
            {
                dataComposite.Add(singleComponent.DeepCopy());
            }
            else
            {
                foreach (AttackData data in dataList)
                {
                    dataComposite.Add(data.DeepCopy());
                }
            }

            return dataComposite;
        }

        //create a deep copy of this composite that only contains data with matching id
        public AttackDataComposite SubComposite(string id)
        {
            return new AttackDataComposite(CopyAttackData(id));
        }

        //create a deep copy composite that only contains data with matching CVM type
        public AttackDataComposite SubComposite(CombatVolumeBase.VolumeType combatVolumePipeline)
        {
            return new AttackDataComposite(CopyAllAttackData(combatVolumePipeline));
        }
        /*
         * Create and return a new Composite that only holds AttackData with IDs that contain string "tag" 
         *  Uses Sting.Split with char[] splitChar to split ID string
         *  
         * Should only be used when needed as these create new arrays. 
         * Use get functions if possible instead on existing composites. 
         * 
         * EX:
         *  attackComposite.SubCompositeExclude('_', "Self");
         *  
         *  this will return a Composite that include IDs such as "DamageData_Self" or "Self_ForceData"
         */
        public AttackDataComposite SubComposite(char[] splitChar, string tag)
        {
            AttackDataComposite dataComposite = new AttackDataComposite();
            if (singleComponent != null)
            {
                FilterAttackDataWithTag(dataComposite, singleComponent, splitChar, tag, true);
            }
            else if (dataList != null && dataList.Count > 0)
            {
                foreach (AttackData data in dataList)
                {
                    FilterAttackDataWithTag(dataComposite, data, splitChar, tag, true);
                }
            }
            return dataComposite;
        }
        public AttackDataComposite SubComposite(char splitChar, string tag)
        {
            return SubComposite(new[] { splitChar }, tag);
        }

        /*
         * SubCompositeExclude functions are the same as SubComposite but return the inverse of SubComposite.
         * 
         * Composites returned will contain all AttackData EXCEPT the specified data
         * EX:
         *  AttackDataComposite with AttackData IDs: "DamageData", "ForceData", and "ForceData_Self"
         *  composite = AttackDataComposite.SubCompositeExclude("DamageData");
         *  
         *  composite will only have AttackData with IDs "ForceData", and "ForceData_Self"
         *  
         * Similarlly 
         *  composite = AttackDataComposite.SubCompositeExclude('_', "Self");
         *  composite will only have AttackData with IDs "ForceData" and "DamageData"
         */
        public AttackDataComposite SubCompositeExclude(string id)
        {
            AttackData[] attackDataArray = GetAllAttackData();

            if (attackDataArray == null)
                return new AttackDataComposite();

            for (int i = 0; i < attackDataArray.Length; ++i)
            {
                if (attackDataArray[i].ID == id)
                    attackDataArray[i] = null;
            }

            return new AttackDataComposite(TrimNull(attackDataArray), true);
        }
        public AttackDataComposite SubCompositeExclude(char[] splitChar, string tag)
        {
            AttackDataComposite dataComposite = new AttackDataComposite();
            if (singleComponent != null)
            {
                FilterAttackDataWithTag(dataComposite, singleComponent, splitChar, tag, false);
            }
            else if (dataList != null && dataList.Count > 0)
            {
                foreach (AttackData data in dataList)
                {
                    FilterAttackDataWithTag(dataComposite, data, splitChar, tag, false);
                }
            }
            return dataComposite;
        }
        public AttackDataComposite SubCompositeExclude(char splitChar, string tag)
        {
            return SubCompositeExclude(new[] { splitChar }, tag);
        }
        public AttackDataComposite SubCompositeExclude(CombatVolumeBase.VolumeType combatVolumePipeline)
        {
            AttackData[] attackDataArray = GetAllAttackData();
            if (attackDataArray == null)
                return new AttackDataComposite();

            for (int i = 0; i < attackDataArray.Length; ++i)
            {
                if (attackDataArray[i].CombatVolumePipeline == combatVolumePipeline)
                    attackDataArray[i] = null;
            }

            return new AttackDataComposite(TrimNull(attackDataArray), true);
        }

        /*
         * Copy counterparts to functions in DirectDataAccess region
         * These return deep copies of data to prevent changes to original data in composite
         */
        public AttackData CopyAttackData(string id)
        {
            AttackData attackData = GetAttackData(id);
            if (attackData == null)
                return null;
            else
                return attackData.DeepCopy();
        }
        public T CopyAttackData<T>(string id) where T : AttackData
        {
            AttackData temp = CopyAttackData(id);
            if (temp == null)
                return null;

            return temp as T;
        }
        public T CopyAttackData<T>() where T : AttackData
        {
            string[] names = typeof(T).ToString().Split('.');
            return CopyAttackData<T>(names[^1]);
        }
        public AttackData[] CopyAllAttackData()
        {
            AttackData[] attackDataArray = GetAllAttackData();
            if (attackDataArray == null)
                return null;

            for (int i = 0; i < attackDataArray.Length; ++i)
            {
                attackDataArray[i] = attackDataArray[i].DeepCopy();
            }
            return attackDataArray;
        }
        public AttackData[] CopyAllAttackData(CombatVolumeBase.VolumeType combatVolumePipeline)
        {
            AttackData[] attackDataArray = GetAllAttackData(combatVolumePipeline);
            if (attackDataArray == null)
                return null;

            for (int i = 0; i < attackDataArray.Length; ++i)
            {
                attackDataArray[i] = attackDataArray[i].DeepCopy();
            }
            return attackDataArray;
        }
        #endregion

        int AttackIDIndex(AttackData[] dataArray, string id)
        {
            int left = 0;
            int right = dataArray.Length - 1;
            while (left <= right)
            {
                int middle = (left + right) / 2;
                int comparison = dataArray[middle].ID.CompareTo(id);
                if (comparison == 0)
                {
                    return middle;
                }
                else if (comparison < 0)
                {
                    left = middle + 1;
                }
                else
                {
                    right = middle - 1;
                }
            }
            return -1;
        }
        AttackData[] TrimNull(AttackData[] attackDataArray)
        {
            if (attackDataArray == null)
                return null;

            if(attackDataArray.Length == 1)
            {
                if (attackDataArray[0] == null)
                    return null;
                else
                    return attackDataArray;
            }

            int nullCount = 0;
            for(int i = 0; i < attackDataArray.Length; ++i)
            {
                if (attackDataArray[i] == null)
                    ++nullCount;
            }

            if (nullCount == 0)
                return attackDataArray;
            else if(nullCount == attackDataArray.Length)
                return null;
            else
            {
                int index = 0;
                AttackData[] newArray = new AttackData[attackDataArray.Length - nullCount];
                for(int i = 0; i < attackDataArray.Length; ++i)
                {
                    if (attackDataArray[i] != null)
                    {
                        newArray[index] = attackDataArray[i];
                        ++index;
                    }
                }
                return newArray;
            }
        }
        void FilterAttackDataWithTag(AttackDataComposite dataComposite, AttackData attackData, char[] splitChar, string tag, bool allowTag)
        {
            if (dataComposite == null || attackData == null)
                return;

            string[] names = attackData.ID.Split(splitChar);
            if (allowTag)
            {
                if (Array.IndexOf(names, tag) >= 0)
                    dataComposite.Add(attackData.DeepCopy());
            }
            else
            {
                if (Array.IndexOf(names, tag) < 0)
                    dataComposite.Add(attackData.DeepCopy());
            }
        }
    }

    //A single container for a portion of data that will be contained in an attack
    // data is portioned out for reusability and modular attack creation
    [System.Serializable]
    public abstract class AttackData
    {
        //ID should match the subclass name as a way to define the type of data that is held and so casting to the right class is easier
        // prefixes and suffixes, or tags, may be added to the ID in order to further describe the use of the data
        // EX: ID-ForceData may be tagged with "Self" creating the ID ForceData_Self
        //  the char '_' is added to easier parse the ID
        //  this tag allows us to later see that force data is to be applied to self rather than a hit object
        //  this also allows us to store more than one ForceData in AttackDataComposite. One for self and one for the hit object
        public string ID { get; protected set; }

        //AKA Owner, or the game object that is attacking
        [System.NonSerialized]
        GameObject instigator;
        public GameObject Instigator
        {
            get => instigator;
        }

        //this determins which combat volume's results to use when sending over data to target
        // EX: DamageData (a subclass of AttackData) uses the Hurtboxs, so Hurtbox overlap results will recive DamageData
        //
        // This is set in subclasses constructor forcing:
        //   DamageData - Hurtbox
        //   ForceData - Pushbox or None
        CombatVolumeBase.VolumeType combatVolumePipeline;
        public CombatVolumeBase.VolumeType CombatVolumePipeline
        {
            get => combatVolumePipeline;
            protected set => combatVolumePipeline = value;
        }


        public AttackData(string id, GameObject instigator, CombatVolumeBase.VolumeType managerPipeline)
        {
            this.ID = id;
            this.instigator = instigator;
            this.combatVolumePipeline = managerPipeline;
        }
        public AttackData()
        {
            ID = "ID_NOT_SET";
            instigator = null;
        }

        public override string ToString()
        {
            if (instigator)
                return (ID + " | Instigator: " + Instigator.name + " | Manager Pipeline: " + CombatVolumePipeline.ToString());
            else
                return (ID + " | Instigator: Null | Manager Pipeline: " + CombatVolumePipeline.ToString());
        }

        public abstract string InfoString();
        public abstract AttackData DeepCopy();
    }

    [System.Serializable]
    public class DamageData : AttackData
    {
        //more overhead using properties but allows SerializeFields and easy access through property
        [SerializeField]
        AttackType attackType;
        public AttackType AttackType
        {
            get => attackType;
        }


        protected Dictionary<DamageType, float> damageValues;
        public Dictionary<DamageType, float> DamageValues
        {
            get => damageValues;
        }


        public DamageData(GameObject instigator, AttackType attackType, Dictionary<DamageType, float> damageValues)
            : base("DamageData", instigator, CombatVolumeBase.VolumeType.Hurtbox)
        {
            this.attackType = attackType;
            this.damageValues = damageValues;
        }
        protected DamageData(string id, GameObject instigator, AttackType attackType, Dictionary<DamageType, float> damageValues)
            : base(id, instigator, CombatVolumeBase.VolumeType.Hurtbox)
        {
            this.attackType = attackType;
            this.damageValues = damageValues;
        }


        public override AttackData DeepCopy()
        {
            Dictionary<DamageType, float> damageValuesCopy = new Dictionary<DamageType, float>(damageValues);
            return new DamageData(Instigator, AttackType, damageValuesCopy);
        }

        public float GetTotalDamage()
        {
            if (damageValues == null)
                return 0f;
            else
            {
                float total = 0f;
                foreach (DamageType damageType in damageValues.Keys)
                {
                    total += damageValues[damageType];
                }
                return total;
            }
        }

        public override string ToString()
        {
            return (base.ToString() + " |  " + InfoString());
        }

        public override string InfoString()
        {
            string dmgVals = "Empty";
            if (damageValues != null && damageValues.Count > 0)
            {
                dmgVals = "";
                foreach (DamageType damageType in damageValues.Keys)
                {
                    dmgVals += (damageType.ToString() + " " + damageValues[damageType].ToString() + "; ");
                }
            }

            return ("AttackType: " + attackType.ToString() + " | DamageValues: " + dmgVals);
        }

    }

    //A subclass of Damage Data that allows damage values to be set to a random value within defined ranges
    [System.Serializable]
    public class RandomDamageData : DamageData
    {
        public struct RandomRange
        {
            public float min;
            public float max;
        }

        Dictionary<DamageType, RandomRange> damageValueRanges;
        public Dictionary<DamageType, RandomRange> DamageValueRanges
        {
            get => damageValueRanges;
        }
        
        public bool DamageDataCalculated { get; private set; }

        public RandomDamageData(GameObject instigator, AttackType attackType, Dictionary<DamageType, RandomRange> damageValues)
            : base("RandomDamageData", instigator, attackType, new Dictionary<DamageType, float>())
        {
            this.damageValueRanges = damageValues;
        }
        private RandomDamageData(GameObject instigator, Dictionary<DamageType, RandomRange> damageValues, DamageData damageData)
            : base("RandomDamageData", instigator, damageData.AttackType, new Dictionary<DamageType, float>(damageData.DamageValues))
        {
            this.damageValueRanges = new Dictionary<DamageType, RandomRange>(damageValues);
        }


        public override AttackData DeepCopy()
        {
            return new RandomDamageData(Instigator, damageValueRanges, this);
        }

        public void CalculateDamage()
        {
            DamageDataCalculated = true;
            foreach (DamageType damageType in damageValueRanges.Keys)
            {
                damageValues[damageType] = UnityEngine.Random.Range(damageValueRanges[damageType].min, damageValueRanges[damageType].min);
            }
        }

        public override string ToString()
        {
            return (base.ToString() + " |  " + InfoString());
        }
        public override string InfoString()
        {
            string dmgVals = "Empty";
            if (damageValueRanges != null && damageValueRanges.Count > 0)
            {
                dmgVals = "";
                foreach (DamageType damageType in damageValueRanges.Keys)
                {
                    dmgVals += (damageType.ToString() + "  min: " + damageValueRanges[damageType].min + "  max: " + damageValueRanges[damageType].max + "; ");
                }
            }

            return ("AttackType: " + AttackType.ToString() + " | DamageValues: " + dmgVals);
        }

    }

    [System.Serializable]
    public class ForceData : AttackData
    {
        //if force is applied to attacker(self) or target
        bool applyToSelf;

        [SerializeField]
        Vector3 forceDirection;
        public Vector3 ForceDirection
        {
            get => forceDirection;
            set => forceDirection = value;
        }

        [SerializeField]
        float force;
        public float ForceScalar
        {
            get => force;
        }

        [SerializeField]
        ForceMode forceMode;
        public ForceMode ForceMode
        {
            get => forceMode;
        }

        [System.NonSerialized]
        Vector3 adjustedForceDirection;
        public Vector3 AdjustedForceDirection 
        {
            get => adjustedForceDirection; 
            set => adjustedForceDirection = value; 
        }

        public Vector3 Force
        {
            get => (adjustedForceDirection * force);
        }


        public ForceData(GameObject instigator, Vector3 forceDirection, float force, ForceMode forceMode, bool applyToSelf)
            : base("ForceData", instigator, CombatVolumeBase.VolumeType.Pushbox)
        {
            if (applyToSelf)
            {
                ID += "_Self"; //ID becomes "ForceData_Self"
                CombatVolumePipeline = CombatVolumeBase.VolumeType.None; //data is ment to affect self and will not be passed down any pipeline
            }

            this.applyToSelf = applyToSelf;
            this.forceDirection = forceDirection;
            adjustedForceDirection = forceDirection;
            this.force = force;
            this.forceMode = forceMode;
        }
        public ForceData(GameObject instigator, Vector3 forceDirection, Vector3 adjustedForceDirection, float force, ForceMode forceMode, bool applyToSelf)
            :this(instigator, forceDirection, force, forceMode, applyToSelf)
        {
            this.adjustedForceDirection = adjustedForceDirection;
        }

        public override AttackData DeepCopy()
        {
            return new ForceData(Instigator, forceDirection, adjustedForceDirection, force, forceMode, applyToSelf);
        }


        public override string ToString()
        {
            return (base.ToString() + "  " + InfoString());
        }

        public override string InfoString()
        {
            return ("AttackForce: Force Direction-" + forceDirection.ToString() + " | Adjusted Force Direction-" + adjustedForceDirection.ToString() + " | Force-" + force.ToString() + " |  Mode-" + forceMode.ToString());
        }

    }


}