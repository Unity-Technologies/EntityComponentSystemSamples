using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace HelloCube.GameObjectSync
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

        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            // For every entity with a LocalTransform, RotationSpeed, and RotatingGameObject component...
            foreach (var (transform, speed, go) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRO<RotationSpeed>, RotatingGameObject>())
            {
                // Update the entity transform.
                transform.ValueRW = transform.ValueRO.RotateY(
                    speed.ValueRO.RadiansPerSecond * deltaTime);

                // Update the associated GameObject's transform to match.
                go.Value.transform.rotation = transform.ValueRO.Rotation;
            }
        }
    }
}
