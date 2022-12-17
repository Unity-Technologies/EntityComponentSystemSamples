using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace HelloCube.Prefabs
{
    [BurstCompile]
    public partial struct RotationSpeedSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Execute>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (transform, speed) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRO<RotationSpeed>>())
            {
                transform.ValueRW =
                    transform.ValueRO.RotateY(speed.ValueRO.RadiansPerSecond * SystemAPI.Time.DeltaTime);
            }
        }
    }
}
