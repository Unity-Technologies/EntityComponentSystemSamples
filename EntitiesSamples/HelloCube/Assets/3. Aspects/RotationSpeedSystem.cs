using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace HelloCube.Aspects
{
    [RequireMatchingQueriesForUpdate]
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
            float deltaTime = SystemAPI.Time.DeltaTime;
            double elapsedTime = SystemAPI.Time.ElapsedTime;

            // The query will include all components of TransformAspect plus the RotationSpeed component.
            // Note that, unlike components, aspect type params of SystemAPI.Query are not wrapped in a RefRW or RefRO.
            foreach (var (transform, speed) in SystemAPI.Query<TransformAspect, RefRO<RotationSpeed>>())
            {
                transform.RotateLocal(quaternion.RotateY(speed.ValueRO.RadiansPerSecond * deltaTime));
            }

            // The query will include all components of VerticalMovementAspect.
            foreach (var movement in SystemAPI.Query<VerticalMovementAspect>())
            {
                movement.Move(elapsedTime);
            }
        }
    }

    // An instance of this aspect wraps the LocalTransform and RotationSpeed components of a single entity.
    // This trivial example is arguably not worth the effort, but larger examples,
    // like Unity.Transforms.TransformAspect, better demonstrate the utility of aspects.
    public readonly partial struct VerticalMovementAspect : IAspect
    {
        readonly RefRW<LocalTransform> m_Transform;
        readonly RefRO<RotationSpeed> m_Speed;

        public void Move(double elapsedTime)
        {
            m_Transform.ValueRW.Position.y = (float)math.sin(elapsedTime * m_Speed.ValueRO.RadiansPerSecond);
        }
    }
}
