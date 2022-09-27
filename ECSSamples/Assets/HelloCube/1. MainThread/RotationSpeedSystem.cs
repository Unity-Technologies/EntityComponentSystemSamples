#if !ENABLE_TRANSFORM_V1
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace HelloCube.MainThread
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(MainThreadGroup))]
    [BurstCompile]
    public partial struct RotationSpeedSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            // Loop over every entity having a LocalToWorldTransform component and RotationSpeed component.
            // In each iteration, transform is assigned a read-write reference to the LocalToWorldTransform,
            // and speed is assigned a read-only reference to the RotationSpeed component.
            foreach (var (transform, speed) in
                     SystemAPI.Query<RefRW<LocalToWorldTransform>, RefRO<RotationSpeed>>())
            {
                // ValueRW and ValueRO both return a ref to the actual component value.
                // The difference is that ValueRW does a safety check for read-write access while
                // ValueRO does a safety check for read-only access.
                transform.ValueRW.Value = transform.ValueRO.Value.RotateY(
                    speed.ValueRO.RadiansPerSecond * deltaTime);
            }
        }
    }
}
#endif
