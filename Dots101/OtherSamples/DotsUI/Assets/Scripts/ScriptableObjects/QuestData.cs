using System;
using UnityEngine;

namespace Unity.DotsUISample
{
    [CreateAssetMenu(fileName = "New Quest", menuName = "Quest")]
    public class QuestData : ScriptableObject
    {
        public string Title;
        public QuestGoals[] Items;
        public string CompletionText;
        public bool HasAllItems;
        public bool Done;
    }
    
    [Serializable]
    public class QuestGoals
    {
        public CollectableType Type;
        public int GoalCount;
    }
}