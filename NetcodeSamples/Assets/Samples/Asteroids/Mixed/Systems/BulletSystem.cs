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
        [WithAll(typeof(Simulate), typeof(BulletTagComponent))]
        partial struct BulletJob : IJobEntity
        {
            public float deltaTime;

            public void Execute(ref LocalTransform transform, in Velocity velocity)
            {
                transform.Position.xy += velocity.Value * deltaTime;
            }

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
