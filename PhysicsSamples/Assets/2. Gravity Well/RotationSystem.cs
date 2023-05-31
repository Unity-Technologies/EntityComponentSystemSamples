using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;

namespace Conversion
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(GravityWellSystem))]
    public partial struct RotationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new RotationJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
            }.Schedule();
        }
    }

    [BurstCompile]
    public partial struct RotationJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(ref LocalTransform localTransform, in Rotation rotator)
        {
            var av = rotator.LocalAngularVelocity * DeltaTime;

            localTransform.Rotation = math.mul(localTransform.Rotation, quaternion.Euler(av));
        }
    }
}
