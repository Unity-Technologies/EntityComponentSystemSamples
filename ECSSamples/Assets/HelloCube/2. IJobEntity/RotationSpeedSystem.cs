#if !ENABLE_TRANSFORM_V1
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace HelloCube.JobEntity
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(JobEntityGroup))]
    [BurstCompile]
    public partial struct RotationSpeedSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new RotationSpeedJob { deltaTime = SystemAPI.Time.DeltaTime };
            job.ScheduleParallel();
        }
    }

    partial struct RotationSpeedJob : IJobEntity
    {
        public float deltaTime;

        // In source generation, a query is created from the parameters of Execute().
        // Here, the query will match all entities having a LocalToWorldTransform component and RotationSpeed component.
        void Execute(ref LocalToWorldTransform transform, in RotationSpeed speed)
        {
            transform.Value = transform.Value.RotateY(speed.RadiansPerSecond * deltaTime);
        }
    }
}
#endif
