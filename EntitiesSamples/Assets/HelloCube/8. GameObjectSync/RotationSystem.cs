using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace HelloCube.GameObjectSync
{
    public partial struct RotationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DirectoryManaged>();
            state.RequireForUpdate<Execute.GameObjectSync>();
        }

        // This OnUpdate accesses managed objects, so it cannot be burst compiled.
        public void OnUpdate(ref SystemState state)
        {
            var directory = SystemAPI.ManagedAPI.GetSingleton<DirectoryManaged>();
            if (!directory.RotationToggle.isOn)
            {
                return;
            }

            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (transform, speed, go) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRO<RotationSpeed>, RotatorGO>())
            {
                transform.ValueRW = transform.ValueRO.RotateY(
                    speed.ValueRO.RadiansPerSecond * deltaTime);

                // Update the associated GameObject's transform to match.
                go.Value.transform.rotation = transform.ValueRO.Rotation;
            }
        }
    }
}
