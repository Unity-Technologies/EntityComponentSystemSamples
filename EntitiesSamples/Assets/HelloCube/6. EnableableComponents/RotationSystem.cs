using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace HelloCube.EnableableComponents
{
    public partial struct RotationSystem : ISystem
    {
        float m_Timer;
        const float k_Interval = 1.3f;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_Timer = k_Interval;
            state.RequireForUpdate<ExecuteEnableableComponents>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            m_Timer -= deltaTime;

            // Toggle the enabled state of every RotationSpeed
            if (m_Timer < 0)
            {
                foreach (var rotationSpeedEnabled in
                         SystemAPI.Query<EnabledRefRW<RotationSpeed>>()
                             .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
                {
                    rotationSpeedEnabled.ValueRW = !rotationSpeedEnabled.ValueRO;
                }

                m_Timer = k_Interval;
            }

            // The query only matches entities whose RotationSpeed is enabled.
            foreach (var (transform, speed) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRO<RotationSpeed>>())
            {
                transform.ValueRW = transform.ValueRO.RotateY(
                    speed.ValueRO.RadiansPerSecond * deltaTime);
            }
        }
    }
}
