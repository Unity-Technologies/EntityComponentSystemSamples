using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace HelloCube.JobEntity
{
    public partial struct RotationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Execute.IJobEntity>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new RotationJob { deltaTime = SystemAPI.Time.DeltaTime };
            job.ScheduleParallel();
        }
    }

    [BurstCompile]
    partial struct RotationJob : IJobEntity
    {
        public float deltaTime;

        // In source generation, a query is created from the parameters of Execute().
        // Here, the query will match all entities having a LocalTransform component and RotationSpeed component.
        void Execute(ref LocalTransform transform, in RotationSpeed speed)
        {
            transform = transform.RotateY(speed.RadiansPerSecond * deltaTime);
        }
    }
}
