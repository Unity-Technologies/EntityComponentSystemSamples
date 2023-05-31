using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Modify
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    public partial struct ScaleSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new ScaleJob().Schedule();
        }

        [BurstCompile]
        public partial struct ScaleJob : IJobEntity
        {
            public void Execute(ref Scaling scaling, ref LocalTransform localTransform)
            {
                localTransform.Scale = math.lerp(localTransform.Scale, scaling.Target, 0.05f);

                // If we reach the target, get a new target
                if (math.abs(localTransform.Scale - scaling.Target) < 0.01f)
                {
                    scaling.Target = scaling.Target == scaling.Min ? scaling.Max : scaling.Min;
                }
            }
        }
    }
}
