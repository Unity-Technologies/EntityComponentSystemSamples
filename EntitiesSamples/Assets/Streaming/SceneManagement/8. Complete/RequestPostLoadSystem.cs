#if !UNITY_DISABLE_MANAGED_COMPONENTS

using Unity.Collections;
using Unity.Entities;

namespace Streaming.SceneManagement.CompleteSample
{
    // Adds PostLoadCommandBuffer to the section meta entities.
    [UpdateAfter(typeof(TileLoadingSystem))]
    partial struct RequestPostLoadSystem : ISystem
    {
        // Cannot be Burst-compiled because it uses PostLoadCommandBuffer.
        public void OnUpdate(ref SystemState state)
        {
            var requiresQuery = SystemAPI.QueryBuilder().WithAll<RequiresPostLoadCommandBuffer>().Build();
            var entities = requiresQuery.ToEntityArray(Allocator.Temp);
            var requires = requiresQuery.ToComponentDataArray<RequiresPostLoadCommandBuffer>(Allocator.Temp);

            state.EntityManager.AddComponent<PostLoadCommandBuffer>(requiresQuery);
            for (int index = 0; index < entities.Length; ++index)
            {
                var buf = new PostLoadCommandBuffer();
                buf.CommandBuffer = new EntityCommandBuffer(Allocator.Persistent, PlaybackPolicy.MultiPlayback);

                // Create an entity with the subscene offset and rotation
                var postLoadEntity = buf.CommandBuffer.CreateEntity();
                buf.CommandBuffer.AddComponent(postLoadEntity, new TileOffset
                {
                    Offset = requires[index].Position,
                    Rotation = requires[index].Rotation
                });

                state.EntityManager.SetComponentData(entities[index], buf);
            }

            state.EntityManager.RemoveComponent<RequiresPostLoadCommandBuffer>(requiresQuery);
        }
    }
}

#endif
