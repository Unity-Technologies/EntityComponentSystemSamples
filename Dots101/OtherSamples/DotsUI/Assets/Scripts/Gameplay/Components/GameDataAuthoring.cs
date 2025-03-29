using Unity.Entities;
using UnityEngine;

namespace Unity.DotsUISample
{
    public class GameDataAuthoring : MonoBehaviour
    {
        public DialogueData startDialogue;
        public DialogueData endDialogue;
        public QuestData quest;
        public CollectablesData collectables;
        
        private class Baker : Baker<GameDataAuthoring>
        {
            public override void Bake(GameDataAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.None);
                
                // to avoid mutating the original scriptable objects, we make copies
                AddComponent(entity, new GameData
                {
                    StartDialogue = ScriptableObject.Instantiate(authoring.startDialogue),
                    EndDialogue = ScriptableObject.Instantiate(authoring.endDialogue),
                    Quest = ScriptableObject.Instantiate(authoring.quest),  
                    Collectables = ScriptableObject.Instantiate(authoring.collectables),
                    State = GameState.Init,
                    InterfaceState = InterfaceState.Questing,
                });
            }
        }
    }
    
    public struct GameData : IComponentData
    {
        public GameState State;
        public InterfaceState InterfaceState;
        public UnityObjectRef<QuestData> Quest;
        public UnityObjectRef<CollectablesData> Collectables;
        public UnityObjectRef<DialogueData> StartDialogue;
        public UnityObjectRef<DialogueData> EndDialogue;
    }
    
    public enum GameState
    {
        Init,
        SplashScreen,
        OpeningDialogue,
        Questing,
        ClosingDialogue,
    }
    
    public enum InterfaceState
    {
        Questing,
        Inventory,
        Help,
    }
}