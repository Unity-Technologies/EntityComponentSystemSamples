using Unity.Entities;
using Unity.Transforms;
using Unity.NetCode;
using Unity.Collections;
using Unity.Burst;

namespace Asteroids.Mixed
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [BurstCompile]
    public partial struct BulletSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BulletTagComponent>();
        }
        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {}
        [BurstCompile]
        [WithAll(typeof(Simulate), typeof(BulletTagComponent))]
        partial struct BulletJob : IJobEntity
        {
            public float deltaTime;
#if !ENABLE_TRANSFORM_V1
            public void Execute(ref LocalTransform transform, in Velocity velocity)
            {
                transform.Position.xy += velocity.Value * deltaTime;
            }
#else
            public void Execute(ref Translation position, in Velocity velocity)
            {
                position.Value.xy += velocity.Value * deltaTime;
            }
#endif
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var bulletJob = new BulletJob
            {
                deltaTime = SystemAPI.Time.DeltaTime
            };
            state.Dependency = bulletJob.ScheduleParallel(state.Dependency);
        }
    }
}
