using Unity.Entities;
using UnityEngine.InputSystem;

namespace Unity.DotsUISample
{
    [UpdateAfter(typeof(EventSystem))]
    public partial struct UISystem : ISystem
    {
        private InterfaceState lastInterfaceState;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameData>();
            state.RequireForUpdate<UIScreens>();
            state.RequireForUpdate<Player>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var game = SystemAPI.GetSingletonRW<GameData>();
            if (game.ValueRO.State != GameState.Questing)
            {
                return;
            }

            var screens = SystemAPI.GetSingleton<UIScreens>();
            
            screens.HintScreen.Value.Hide();
            
            foreach (var evt in
                     SystemAPI.Query<RefRO<CollectableProximityEvent>>())
            {
                screens.HintScreen.Value.SetMessage("COLLECT");
                screens.HintScreen.Value.Show();
                screens.HintScreen.Value.SetPosition(evt.ValueRO.Position, screens.Camera.Value);
                
            }
            
            foreach (var evt in
                     SystemAPI.Query<RefRO<CauldronProximityEvent>>())
            {
                screens.HintScreen.Value.SetMessage("INTERACT");
                screens.HintScreen.Value.Show();
                screens.HintScreen.Value.SetPosition(evt.ValueRO.Position, screens.Camera.Value);
            }

            foreach (var evt in
                     SystemAPI.Query<RefRO<HelpScreen.CloseClickEvent>>())
            {
                game.ValueRW.InterfaceState = InterfaceState.Questing;
            }

            foreach (var evt in
                     SystemAPI.Query<RefRO<HUDScreen.HelpClickEvent>>())
            {
                game.ValueRW.InterfaceState = InterfaceState.Help;
            }

            foreach (var evt in
                     SystemAPI.Query<RefRO<HUDScreen.InventoryClickEvent>>())
            {
                game.ValueRW.InterfaceState = InterfaceState.Inventory;
            }

            foreach (var evt in
                     SystemAPI.Query<RefRO<InventoryScreen.BackClickedEvent>>())
            {
                game.ValueRW.InterfaceState = InterfaceState.Questing;
            }

            foreach (var evt in
                     SystemAPI.Query<RefRO<PickupEvent>>())
            {
                var player = SystemAPI.GetSingleton<Player>();
                screens.InventoryScreen.Value.UpdateInventory(SystemAPI.GetSingletonBuffer<InventoryItem>(), 
                    player.EnergyCount, game.ValueRO.Collectables.Value);
                screens.QuestScreen.Value.UpdateMessage(SystemAPI.GetSingletonBuffer<CollectableCount>(),
                    game.ValueRO.Collectables.Value, game.ValueRO.Quest.Value.HasAllItems);
            }

            if (GameInput.ShowInventory.WasPerformedThisFrame()) 
            {
                if (game.ValueRW.InterfaceState == InterfaceState.Inventory)
                {
                    game.ValueRW.InterfaceState = InterfaceState.Questing;
                }
                else
                {
                    game.ValueRW.InterfaceState = InterfaceState.Inventory;
                }
            }

            if (GameInput.Cancel.WasPerformedThisFrame())
            {
                game.ValueRW.InterfaceState = InterfaceState.Questing;
            }
            
            if (lastInterfaceState != game.ValueRW.InterfaceState)
            {
                // set render state
                switch (game.ValueRW.InterfaceState)
                {
                    case InterfaceState.Questing:
                        screens.HelpScreen.Value.Hide();
                        screens.InventoryScreen.Value.Hide();
                        screens.QuestScreen.Value.Show();
                        screens.HUDScreen.Value.Show();
                        break;
                    case InterfaceState.Inventory:
                        screens.HelpScreen.Value.Hide();
                        screens.InventoryScreen.Value.Show();
                        screens.QuestScreen.Value.Show();
                        screens.HUDScreen.Value.Show();
                        break;
                    case InterfaceState.Help:
                        screens.HelpScreen.Value.Show();
                        screens.InventoryScreen.Value.Hide();
                        screens.QuestScreen.Value.Hide();
                        screens.HUDScreen.Value.Hide();
                        break;
                }
            }
            lastInterfaceState = game.ValueRW.InterfaceState;
        }
    }
}