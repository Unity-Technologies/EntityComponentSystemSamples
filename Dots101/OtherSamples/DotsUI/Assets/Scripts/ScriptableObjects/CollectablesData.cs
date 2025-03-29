using System;
using UnityEngine;

namespace Unity.DotsUISample
{
    [CreateAssetMenu(fileName = "New Collectables List", menuName = "Collectables")]
    public class CollectablesData : ScriptableObject
    {
        // the number and order of collectables in the list
        // must match the members of the CollectableType enum
        public CollectableItem[] Collectables;
    }
    
    [Serializable]
    public struct CollectableItem
    {
        public string Name;
        public Sprite Icon;
    }
}