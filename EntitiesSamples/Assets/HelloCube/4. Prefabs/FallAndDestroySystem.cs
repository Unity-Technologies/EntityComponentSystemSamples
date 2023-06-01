using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace HelloCube.Prefabs
{
    public partial struct FallAndDestroySystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Execute.Prefabs>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // rotation
            float deltaTime = SystemAPI.Time.DeltaTime;
            foreach (var (transform, speed) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRO<RotationSpeed>>())
            {
                // ValueRW and ValueRO both return a ref to the actual component value.
                // The difference is that ValueRW does a safety check for read-write access while
                // ValueRO does a safety check for read-only access.
                transform.ValueRW = transform.ValueRO.RotateY(
                    speed.ValueRO.RadiansPerSecond * deltaTime);
            }

            // An EntityCommandBuffer created from EntityCommandBufferSystem.Singleton will be
            // played back and disposed by the EntityCommandBufferSystem when it next updates.
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // Downward vector
            var movement = new float3(0, -SystemAPI.Time.DeltaTime * 5f, 0);

            // WithAll() includes RotationSpeed in the query, but
            // the RotationSpeed component values will not be accessed.
            // WithEntityAccess() includes the Entity ID as the last element of the tuple.
            foreach (var (transform, entity) in
                     SystemAPI.Query<RefRW<LocalTransform>>()
                         .WithAll<RotationSpeed>()
                         .WithEntityAccess())
            {
                transform.ValueRW.Position += movement;
                if (transform.ValueRO.Position.y < 0)
                {
                    // Making a structural change would invalidate the query we are iterating through,
                    // so instead we record a command to destroy the entity later.
                    ecb.DestroyEntity(entity);
                }
            }
        }
    }
}
