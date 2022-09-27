#if !ENABLE_TRANSFORM_V1
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace HelloCube.Prefabs
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(PrefabsGroup))]
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
            foreach (var (transform, speed) in
                     SystemAPI.Query<RefRW<LocalToWorldTransform>, RefRO<RotationSpeed>>())
            {
                transform.ValueRW.Value =
                    transform.ValueRO.Value.RotateY(speed.ValueRO.RadiansPerSecond * SystemAPI.Time.DeltaTime);
            }
        }
    }
}
#endif
