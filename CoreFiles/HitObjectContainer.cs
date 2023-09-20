using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


namespace AttackPack
{
    //Allows for different organization of hit objects depending on game or object's needs
    public abstract class HitObjectContainer
    {
        public bool LogDebug { get; set; }
        public bool FilterData { get; set; }
        public abstract string AddObject(ITakeDamage damageInterface, CombatVolumeBase.VolumeType volumeTypeOfSender, out bool wasAdded);
        public abstract bool AddObject(GameObject gameObject);
        public abstract void Clear();
    }


    /*
     * A linked list like container that holds information about objects hit by an attack.
     * This can be used to prevent sending data such as damage to an object more than once per attack as well as filter data if multiple combat volume types are used in a single attack.
     * 
     * Hitlist: Node, Node, Node
     *          |           |
     *       SubNode     SubNode
     *          |
     *       SubNode
     *           
     * Hit objects are GameObjects stored in Nodes within a List with a link to a possible SubNode.
     * SubNode are only added if a hit object uses the ITakeDamage interface. The GroupID string is stored in SubNode as well as a link to a possible other SubNode.
     * 
     * EX:
     * HitList: Player, StaticWall
     *            |
     *          L_Arm
     *            |
     *           Body
     *           
     * In this case an attack hit a wall that does not use ITakeDamage as well as a Player that uses ITakeDamage and has groups "L_Arm" and "Body".
     *   Player may have more groups but only these two were hit in this example. 
     *   
     *   
     * In addition to this HitList can "tag" groups with extra information.
     *   If FilterData is true this means that multiple combat volume types are used and each is ment to send a different type of data from AttackComposite.
     *   to track what data was already sent this attack, HitList adds the VolumeType of the volume that is hitting the object as an int.
     *   
     * EX:
     * enum VolumeType{ HurtBox, PushBox };
     *   
     * HitList: Player, StaticWall
     *            |
     *          L_Arm_0
     *            |
     *           Body_0_1
     *           
     * Here the HitList adds "_0" and "_0_1" to the groupIDs. (The '_' char is used to seperate tags but can be changed with the "tagSeperator" variable.)
     *   This is showing that Player L_Arm was hit by HurtBox, while Body was hit by both HurtBox and PushBox.
     */
    public class HitList : HitObjectContainer
    {
        const char tagSeperator = '_';


        public struct Node
        {
            public GameObject gameObject;
            public SubNode subNode;

            public Node(GameObject gameObject)
            {
                this.gameObject = gameObject;
                subNode = null;
            }
        }
        public class SubNode
        {
            public string groupID;
            public SubNode subNode;

            public SubNode(string groupID)
            {
                this.groupID = groupID;
                subNode = null;
            }
        }
        public class CompareNodes : IComparer<Node>
        {
            public int Compare(Node x, Node y)
            {
                return x.gameObject.transform.GetHashCode().CompareTo(y.gameObject.transform.GetHashCode());
            }
        }

        CompareNodes compareNodes;

        List<Node> hitList;

        public HitList()
        {
            hitList = new List<Node>();
            compareNodes = new CompareNodes();
        }


        //use for adding objects that use ITakeDamage interface
        // return value is a string that holds debug info for printing to log
        // The last argument "wasAdded" is used to tell if the object was added to HitList. If it waas that means the object was not hit yet by this attack.
        public override string AddObject(ITakeDamage damageInterface, CombatVolumeBase.VolumeType volumeTypeOfSender, out bool wasAdded)
        {
#if UNITY_EDITOR
            string s = "";
#endif
            Vector2 sortingVector;

            if (hitList.Count != 0)
                sortingVector = FindNode(damageInterface.GameObject);
            else
            {
                if (damageInterface.Parent == null)
                    sortingVector = Vector2.negativeInfinity;
                else
                    sortingVector = Vector2.positiveInfinity;
            }



            if (sortingVector.x != float.NegativeInfinity && sortingVector.y == float.NegativeInfinity)
            {//parent exists but not group ID subNode

                SubNode subNode = GetLastSubNode(hitList[(int)sortingVector.x]);
                if (subNode == null)
                {
                    Node parentNode = hitList[(int)sortingVector.x];
                    if (FilterData)
                        parentNode.subNode = new SubNode(damageInterface.GroupID + tagSeperator + ((int)volumeTypeOfSender).ToString());
                    else
                        parentNode.subNode = new SubNode(damageInterface.GroupID);

                    hitList[(int)sortingVector.x] = parentNode;//update the stored struct
                }
                else
                {
                    if (FilterData)
                        subNode.subNode = new SubNode(damageInterface.GroupID + tagSeperator + ((int)volumeTypeOfSender).ToString());
                    else
                        subNode.subNode = new SubNode(damageInterface.GroupID);
                }

#if UNITY_EDITOR
                if (LogDebug)
                    s += "<i><b>Parent found.</b> Added ID subNode.</i>";
#endif
            }
            else if (sortingVector.x == float.PositiveInfinity && sortingVector.y == float.PositiveInfinity)
            {//does not exist but parent needs to be added first

                Node node;
                node = new Node(damageInterface.Parent);

                ITakeDamage parentInterface = damageInterface.Parent.GetComponent<ITakeDamage>();
                if (parentInterface != null)
                {
                    node.subNode = new SubNode(parentInterface.GroupID + tagSeperator + "notHit");
                    if (FilterData)
                        node.subNode.subNode = new SubNode(damageInterface.GroupID + tagSeperator + ((int)volumeTypeOfSender).ToString());
                    else
                        node.subNode.subNode = new SubNode(damageInterface.GroupID);
                }
                else
                {
                    if (FilterData)
                        node.subNode = new SubNode(damageInterface.GroupID + tagSeperator + ((int)volumeTypeOfSender).ToString());
                    else
                        node.subNode = new SubNode(damageInterface.GroupID);
                }


                hitList.Add(node);
                hitList.Sort(compareNodes);

#if UNITY_EDITOR
                if (LogDebug)
                    s += "<i><b>Parent not found.</b> Added ID subNode and parent with tag '" + tagSeperator + "notHit'.</i>";
#endif
            }
            else if (sortingVector.x == float.NegativeInfinity && sortingVector.y == float.NegativeInfinity)
            {//does not exists

                Node node = new Node(damageInterface.GameObject);

                if (FilterData)
                    node.subNode = new SubNode(damageInterface.GroupID + tagSeperator + ((int)volumeTypeOfSender).ToString());
                else
                    node.subNode = new SubNode(damageInterface.GroupID);

                hitList.Add(node);
                hitList.Sort(compareNodes);

#if UNITY_EDITOR
                if (LogDebug)
                    s += "<i><b>Parent not found.</b> Added parent node.</i>";
#endif
            }
            else //object exists in hit list
            {
                //check tags
                SubNode subNode = GetSubNode((int)sortingVector.x, (int)sortingVector.y);
                string[] tags = subNode.groupID.Split(tagSeperator);
                int volumeInt = (int)volumeTypeOfSender;

                if (Array.Exists(tags, element => element == "notHit"))
                {
                    //store all tags that are NOT "notHit"
                    tags = Array.FindAll(tags, element => element != "notHit");
                    subNode.groupID = string.Join(tagSeperator, tags);
                    if (FilterData)
                        subNode.groupID += (string)(tagSeperator + volumeInt.ToString());

#if UNITY_EDITOR
                    if (LogDebug)
                        s += "<i><b>Object in HitList</b> but has 'notHit' tag.</i>";
#endif
                }
                else if (FilterData)
                {
                    //if filtering check if tagged with volume of sender or type All
                    if (Array.Exists(tags, element => (element == ((int)CombatVolumeBase.VolumeType.All).ToString() || element == volumeInt.ToString())))
                    {
                        wasAdded = false;
#if UNITY_EDITOR
                        if (LogDebug)
                        {
                            s += "<i><b>Object in HitList.</b> Already hit by volume type " + volumeInt + "(" + volumeTypeOfSender.ToString() + ") or by type All.</i>";
                        }
                        return s;
#else
                        return "";
#endif
                    }
                    else
                    {
                        subNode.groupID = string.Join(tagSeperator, tags);
                        subNode.groupID += tagSeperator + ((int)volumeTypeOfSender).ToString();
#if UNITY_EDITOR
                        if (LogDebug)
                        {
                            s += "<i><b>Group found.</b> Added volume type " + volumeInt + "(" + volumeTypeOfSender.ToString() + ") to flags.</i>";
                        }
#endif
                    }

                }
                else
                {
                    wasAdded = false;
#if UNITY_EDITOR
                    if (LogDebug)
                    {
                        s += "<i><b>Object in HitList.</b></i>";
                    }
                    return s;
#else
                    return "";
#endif
                }

            }

            wasAdded = true;
#if UNITY_EDITOR
            return s;
#else
            return "";
#endif
        }
        
        //use to add objects that do NOT use ITakeDamage
        // returns if object was already in HitList
        public override bool AddObject(GameObject gameObject)
        {
            Node node = new Node(gameObject);
            int hitIndex = hitList.BinarySearch(node, compareNodes);
            if (hitIndex >= 0)
                return false;
            else
            {
                hitList.Add(node);
                hitList.Sort(compareNodes);
                return true;
            }
        }

        public override void Clear()
        {
            foreach (Node node in hitList)
            {
                if(node.subNode != null)
                {
                    SubNode node1 = node.subNode;
                    SubNode node2 = node.subNode.subNode;
                    while(node2 != null)
                    {
                        node1.subNode = null;
                        node1 = node2;
                        node2 = node2.subNode;
                    }
                    if (node1.subNode != null)
                        node1.subNode = null;
                }
            }
            hitList.Clear();
        }
        public override string ToString()
        {
            if (hitList.Count == 0 || hitList == null)
                return "HitList: Empty";

            string s = "";

            foreach (Node node in hitList)
            {
                s += "<b>[</b> <i>parent</i>: " + node.gameObject.name;
                if (node.subNode != null)
                {
                    s += ", <i>groupID</i>: ";
                    SubNode subNode = node.subNode;
                    while (subNode != null)
                    {
                        s += subNode.groupID;
                        subNode = subNode.subNode;
                        if (subNode != null)
                            s += ", ";
                    }
                }

                s += " <b>]</b>";
            }

            return s;
        }

        Vector2 FindNode(GameObject gameObject)
        {
            /*
             * search for object in hitlist
             * results tell if node exists and if it should be linked to a parent node
             * 
             * Vector.NegativeInfinity     == does not exist
             * Vector.PositiveInfinity     == does not exist but has a parent that needs to be added first
             * Vector(x, NegativeInfinity) == parent exists but object groupID does not
             * Vector(x, y)                == object exists at index x,y where y is depth of SubNodes and 0 is the parent node
             * 
             * If object does not implement ITakeDamage interface then groupIDs will not be checked and only the parent node will matter
             */
            Node node;
            ITakeDamage damageInterface = gameObject.GetComponent<ITakeDamage>();

            if (damageInterface != null && damageInterface.Parent != null)
            {
                node = new Node(damageInterface.Parent);
            }
            else
                node = new Node(gameObject);

            int hitIndex = hitList.BinarySearch(node, compareNodes);
            if (hitIndex >= 0)
            {
                if (damageInterface != null)
                {
                    //search subNodes
                    SubNode subNode = hitList[hitIndex].subNode;
                    int count = 0;//0 is parent
                    while (subNode != null)
                    {
                        string baseID = subNode.groupID.Split(tagSeperator)[0];
                        if (baseID == damageInterface.GroupID)
                            return new Vector2(hitIndex, count);//group found
                        else
                        {
                            subNode = subNode.subNode;
                            count++;
                        }
                    }

                    //parent exists but not group 
                    return new Vector2(hitIndex, float.NegativeInfinity);
                }
                else
                    return new Vector2(hitIndex, 0);//found
            }
            else
            {
                if (damageInterface != null && damageInterface.Parent != null)
                    return Vector2.positiveInfinity;//does not exist, parent needs to be added
                else
                    return Vector2.negativeInfinity;//does not exist
            }
        }
        int GetSubNodeDepth(Node node)
        {
            if (node.subNode == null)
                return 0;

            SubNode subNode = node.subNode;
            int count = 1;
            while (subNode != null)
            {
                subNode = subNode.subNode;
                count++;
            }
            return count;
        }
        SubNode GetLastSubNode(Node node)
        {
            if (node.subNode == null)
                return null;

            SubNode subNode = node.subNode;
            while (subNode.subNode != null)
            {
                subNode = subNode.subNode;
            }
            return subNode;
        }
        SubNode GetSubNode(int x, int y)
        {
            Node node = hitList[x];
            SubNode subNode = node.subNode;

            if (y == 0)
                return subNode;
            else
            {
                for (int i = 0; i < y; ++i)
                    subNode = subNode.subNode;

                return subNode;
            }
        }
    }

}