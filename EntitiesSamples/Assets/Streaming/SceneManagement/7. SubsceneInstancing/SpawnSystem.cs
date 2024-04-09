#if !UNITY_DISABLE_MANAGED_COMPONENTS

using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Scenes;

namespace Streaming.SceneManagement.SubsceneInstancing
{
    // Spawns the scene in a square grid pattern.
    public partial struct SpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Grid>();
        }

        // Cannot be Burst compiled because it uses PostLoadCommandBuffer.
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;

            // Set the parameters to indicate we are going to create instances
            var loadParameters = new SceneSystem.LoadParameters
            {
                Flags = SceneLoadFlags.NewInstance
            };

            var gridQuery = SystemAPI.QueryBuilder().WithAll<Grid>().Build();
            var grids = gridQuery.ToComponentDataArray<Grid>(Allocator.Temp);

            for (int index = 0; index < grids.Length; index += 1)
            {
                // Relative to the origin
                float2 centerOffset = -((grids[index].Size - 1) * grids[index].Spacing);
                centerOffset /= 2f;

                // Create the subscene instances.
                for (int i = 0; i < grids[index].Size; ++i)
                {
                    for (int j = 0; j < grids[index].Size; ++j)
                    {
                        var sceneEntity = SceneSystem.LoadSceneAsync(state.WorldUnmanaged,
                            grids[index].Scene, loadParameters);

                        // A PostLoadCommandBuffer wraps an EntityCommandBuffer that will execute commands
                        // after the subscene instance is loaded.
                        var buf = new PostLoadCommandBuffer();
                        buf.CommandBuffer = new EntityCommandBuffer(Allocator.Persistent, PlaybackPolicy.MultiPlayback);

                        // After the cell is loaded, create an entity to hold the offset for the cell.
                        var postLoadEntity = buf.CommandBuffer.CreateEntity();
                        buf.CommandBuffer.AddComponent(postLoadEntity, new Offset
                        {
                            Value = new float3(
                                grids[index].Spacing.x * i + centerOffset.x,
                                0f,
                                grids[index].Spacing.y * j + centerOffset.y)
                        });

                        state.EntityManager.AddComponentData(sceneEntity, buf);
                    }
                }
            }
        }
    }
}

#endif
