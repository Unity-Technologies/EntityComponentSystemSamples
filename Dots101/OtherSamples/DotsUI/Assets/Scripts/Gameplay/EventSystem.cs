using Unity.Collections;
using Unity.Entities;

namespace Unity.DotsUISample
{
    // Cleans up events of prior frame and creates events for the UI interactions
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(BeginSimulationEntityCommandBufferSystem))]
    public partial struct EventSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<UIScreens>();
        }

        public void OnDestroy(ref SystemState state)
        {
            var screens = SystemAPI.GetSingleton<UIScreens>();
            var ecb = screens.HelpScreen.Value.entityCommandBuffer;
            if (ecb.IsCreated)
            {
                ecb.Dispose();    
            }
        }

        public void OnUpdate(ref SystemState state)
        {
            var screens = SystemAPI.GetSingleton<UIScreens>();
            
            // destroy all events that were created last frame
            var query = SystemAPI.QueryBuilder().WithAll<Event>().Build();
            state.EntityManager.DestroyEntity(query);
            
            var ecb = screens.HelpScreen.Value.entityCommandBuffer;
            if (ecb.IsCreated)
            {
                ecb.Playback(state.EntityManager);
                ecb.Dispose();    
            }

            ecb = new EntityCommandBuffer(Allocator.TempJob);
            screens.HelpScreen.Value.entityCommandBuffer = ecb;
            screens.InventoryScreen.Value.entityCommandBuffer = ecb;
            screens.HUDScreen.Value.entityCommandBuffer = ecb;
        }
    }
}