using Streaming.SceneManagement.Common;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Streaming.SceneManagement.SubsceneInstancing
{
    // After a subscene instance is loaded, this system moves its entities by the instance's offset.
    // This system runs in a separate world before the loaded entities are moved to the main world.
    [WorldSystemFilter(WorldSystemFilterFlags.ProcessAfterLoad)]
    public partial struct OffsetSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Offset>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var offsetQuery = SystemAPI.QueryBuilder().WithAll<Offset>().Build();
            var offsets = offsetQuery.ToComponentDataArray<Offset>(Allocator.Temp);
            state.EntityManager.DestroyEntity(offsetQuery);

            foreach (var offset in offsets)
            {
                // Apply the offset to all the dynamic entities
                foreach (var transform in
                         SystemAPI.Query<RefRW<LocalTransform>>())
                {
                    transform.ValueRW.Position += offset.Value;
                }

                // Apply the offset to all non-dynamic entities
                var offsetMatrix = float4x4.Translate(offset.Value);
                foreach (var transform in
                         SystemAPI.Query<RefRW<LocalToWorld>>()
                             .WithNone<LocalTransform>())
                {
                    transform.ValueRW.Value = math.mul(offsetMatrix, transform.ValueRW.Value);
                }

                // Apply the offset to the center of oscillation
                foreach (var oscillating in
                         SystemAPI.Query<RefRW<Oscillating>>())
                {
                    oscillating.ValueRW.Center += offset.Value;
                }
            }
        }
    }
}
