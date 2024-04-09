using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace HelloCube.Aspects
{
    public partial struct RotationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ExecuteAspects>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            double elapsedTime = SystemAPI.Time.ElapsedTime;

            // Rotate the cube directly without using the aspect.
            // The query matches all entities having the LocalTransform and RotationSpeed components.
            foreach (var (transform, speed) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRO<RotationSpeed>>())
            {
                transform.ValueRW = transform.ValueRO.RotateY(speed.ValueRO.RadiansPerSecond * deltaTime);
            }

            // Rotate the cube using the aspect.
            // The query will include all components of VerticalMovementAspect.
            // Note that, unlike components, aspect type params of SystemAPI.Query are not wrapped in a RefRW or RefRO.
            foreach (var movement in
                     SystemAPI.Query<VerticalMovementAspect>())
            {
                movement.Move(elapsedTime);
            }
        }
    }

    // An instance of this aspect wraps the LocalTransform and RotationSpeed components of a single entity.
    // (This trivial example is arguably not a worthwhile use case for aspects, but larger examples better demonstrate their utility.)
    readonly partial struct VerticalMovementAspect : IAspect
    {
        readonly RefRW<LocalTransform> m_Transform;
        readonly RefRO<RotationSpeed> m_Speed;

        public void Move(double elapsedTime)
        {
            m_Transform.ValueRW.Position.y = (float)math.sin(elapsedTime * m_Speed.ValueRO.RadiansPerSecond);
        }
    }
}
