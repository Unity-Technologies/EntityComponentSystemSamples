using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Asteroids.Server
{
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct BulletAgeSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ageJob = new BulletAgeJob()
            {
                ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                deltaTime = SystemAPI.Time.DeltaTime,
            };
            state.Dependency = ageJob.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        internal partial struct BulletAgeJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ecb;
            public float deltaTime;

            [BurstCompile]
            public void Execute(Entity entity, [EntityIndexInQuery] int entityIndexInQuery, ref BulletAgeComponent age)
            {
                age.age += deltaTime;
                if (age.age > age.maxAge)
                    ecb.DestroyEntity(entityIndexInQuery, entity);
            }
        }
    }
}
