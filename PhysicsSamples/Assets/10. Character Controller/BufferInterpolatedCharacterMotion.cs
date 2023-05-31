using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.GraphicsIntegration;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace CharacterController
{
    // override the behavior of BufferInterpolatedRigidBodiesMotion
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsInitializeGroup)), UpdateBefore(typeof(ExportPhysicsWorld))]
    [UpdateAfter(typeof(BufferInterpolatedRigidBodiesMotion))]
    [RequireMatchingQueriesForUpdate]
    public partial struct BufferInterpolatedCharacterMotion : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new InterpolationBuffersJob().ScheduleParallel();
        }

        public partial struct InterpolationBuffersJob : IJobEntity
        {
            public void Execute(ref PhysicsGraphicalInterpolationBuffer interpolationBuffer,
                in CharacterControllerInternal ccInternal, in LocalTransform localTransform)
            {
                interpolationBuffer = new PhysicsGraphicalInterpolationBuffer
                {
                    PreviousTransform = new RigidTransform(localTransform.Rotation, localTransform.Position),
                    PreviousVelocity = ccInternal.Velocity,
                };
            }
        }
    }
}
