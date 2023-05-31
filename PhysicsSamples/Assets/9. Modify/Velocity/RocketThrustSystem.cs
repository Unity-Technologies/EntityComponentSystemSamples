using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Aspects;
using Unity.Physics.Systems;

namespace Modify
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    public partial struct RocketThrustSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new RocketThurstJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
            }.Schedule();
        }

        [BurstCompile]
        public partial struct RocketThurstJob : IJobEntity
        {
            public float DeltaTime;

            public void Execute(in RocketThrust rocket, RigidBodyAspect rigidBodyAspect)
            {
                // Newton's 3rd law states that for every action there is an equal and opposite reaction.
                // As this is a rocket thrust the impulse applied with therefore use negative Direction.
                float3 impulse = -rocket.Direction * rocket.Magnitude;
                impulse *= DeltaTime;

                rigidBodyAspect.ApplyImpulseAtPointLocalSpace(impulse, rocket.Offset);
            }
        }
    }
}
