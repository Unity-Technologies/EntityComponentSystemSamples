using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Streaming.SceneManagement.CompleteSample
{
    // System that will move/orient all the entities in the tile to the right position/rotation.
    [WorldSystemFilter(WorldSystemFilterFlags.ProcessAfterLoad)]
    public partial struct TileOffsetSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TileOffset>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var offsetQuery = SystemAPI.QueryBuilder().WithAll<TileOffset>().Build();
            var offsets = offsetQuery.ToComponentDataArray<TileOffset>(Allocator.Temp);
            state.EntityManager.DestroyEntity(offsetQuery);

            foreach (var offset in offsets)
            {
                var rotation = quaternion.AxisAngle(new float3(0f, 1f, 0f), offset.Rotation);
                var offsetTransform = LocalTransform.FromPositionRotation(offset.Offset, rotation);

                // Apply the offset and rotation to all dynamic entities
                foreach (var transform in
                         SystemAPI.Query<RefRW<LocalTransform>>()
                             .WithNone<Parent>())
                {
                    transform.ValueRW = offsetTransform.TransformTransform(transform.ValueRW);
                }

                var offsetMatrix = float4x4.TRS(offset.Offset, rotation, new float3(1f, 1f, 1f));

                // Apply the offset and rotation to all non-dynamic entities
                foreach (var transform in
                         SystemAPI.Query<RefRW<LocalToWorld>>()
                             .WithNone<Parent, LocalTransform>())
                {
                    transform.ValueRW.Value = math.mul(offsetMatrix, transform.ValueRW.Value);
                }
            }
        }
    }
}
