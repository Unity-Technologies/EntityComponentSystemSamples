using UnityEngine;

namespace Unity.DotsUISample
{
    [CreateAssetMenu(fileName = "New Dialogue", menuName = "Dialogue")]
    public class DialogueData : ScriptableObject
    {
        [Multiline] public string[] Lines;
    }
}