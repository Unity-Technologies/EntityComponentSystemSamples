using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Unity.DotsUISample
{
    public partial struct GameManagerSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameData>();
            state.RequireForUpdate<CollectableCount>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var game = SystemAPI.GetSingletonRW<GameData>();
            
            if (game.ValueRO.State == GameState.Init)
            {
                GameInput.Initialize();
                
                var doc = GameObject.FindFirstObjectByType<UIDocument>();
                VisualElement root = doc.rootVisualElement;
                
                game.ValueRW.State = GameState.SplashScreen;
                
                var screens = new UIScreens
                {
                    SplashScreen = SplashScreen.Instantiate(root.Q<VisualElement>("splash__container")),
                    HelpScreen = HelpScreen.Instantiate(root.Q<VisualElement>("help__container")),
                    QuestScreen = QuestScreen.Instantiate(root.Q<VisualElement>("quest__container")),
                    HUDScreen = HUDScreen.Instantiate(root.Q<VisualElement>("hud__container")),
                    InventoryScreen = InventoryScreen.Instantiate(root.Q<VisualElement>("inventory__container")),
                    DialogueScreen = DialogueScreen.Instantiate(root.Q<VisualElement>("dialogue__container")),
                    HintScreen = HintScreen.Instantiate(root.Q<VisualElement>("hud__action-helper")),
                    Camera = GameObject.FindFirstObjectByType<Camera>(),
                };
                
                screens.SplashScreen.Value.Show();

                var entity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(entity, screens);
            }
            else if (game.ValueRO.State == GameState.SplashScreen)
            {
                if (Keyboard.current.anyKey.isPressed || Mouse.current.leftButton.isPressed)
                {
                    var screens = SystemAPI.GetSingletonRW<UIScreens>();
                    screens.ValueRO.SplashScreen.Value.Hide();
                    screens.ValueRO.DialogueScreen.Value.SetDialogueData(game.ValueRW.StartDialogue.Value);
                    screens.ValueRO.DialogueScreen.Value.Show();
                    
                    game.ValueRW.State = GameState.OpeningDialogue;
                }
            }
            else if (game.ValueRO.State == GameState.OpeningDialogue)
            {
                var screens = SystemAPI.GetSingletonRW<UIScreens>();
                if (screens.ValueRO.DialogueScreen.Value.isDone)
                {
                    screens.ValueRW.DialogueScreen.Value.Hide();
                    
                    screens.ValueRO.HUDScreen.Value.Show();
                    screens.ValueRO.QuestScreen.Value.SetQuestData(
                        game.ValueRW.Quest.Value,  
                        SystemAPI.GetSingletonBuffer<CollectableCount>(), 
                        game.ValueRW.Collectables.Value
                    );
                    screens.ValueRO.QuestScreen.Value.Show();
                    
                    game.ValueRW.State = GameState.Questing;
                }
            }
            else if (game.ValueRO.State == GameState.Questing)
            {
                if (game.ValueRW.Quest.Value.Done)
                {
                    // kill player momentum
                    foreach (var velocity in
                             SystemAPI.Query<RefRW<PhysicsVelocity>>()
                                 .WithAll<Player>())
                    {
                        velocity.ValueRW.Linear = float3.zero;
                    }

                    var screens = SystemAPI.GetSingletonRW<UIScreens>();
                    screens.ValueRO.HUDScreen.Value.Hide();
                    screens.ValueRO.QuestScreen.Value.Hide();
                    screens.ValueRO.InventoryScreen.Value.Hide();
                    screens.ValueRO.HelpScreen.Value.Hide();
                    
                    screens.ValueRO.DialogueScreen.Value.SetDialogueData(game.ValueRW.EndDialogue.Value);
                    screens.ValueRO.DialogueScreen.Value.Show();
                    
                    game.ValueRW.State = GameState.ClosingDialogue;
                }
            }
            else if (game.ValueRO.State == GameState.ClosingDialogue)
            {
                var screens = SystemAPI.GetSingletonRW<UIScreens>();
                if (screens.ValueRO.DialogueScreen.Value.isDone)
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                }
            }
        }
    }
}