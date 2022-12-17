using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace RandomSpawn
{
    [WithAll(typeof(NewSpawn))]
    partial struct RandomPositionJob : IJobEntity
    {
        public uint SeedOffset;

        public void Execute([EntityIndexInQuery] int index, ref LocalTransform transform)
        {
            // Random instances with similar seeds produce similar results, so to get proper
            // randomness here, we use CreateFromIndex, which hashes the seed. 
            var random = Random.CreateFromIndex(SeedOffset + (uint)index);
            var xz = random.NextFloat2Direction() * 50;
            transform.Position = new float3(xz[0], 50, xz[1]);
        }
    }

    [BurstCompile]
    public partial struct SpawnSystem : ISystem
    {
        EntityQuery m_NewSpawnQuery;
        uint seedOffset;
        float spawnTimer;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp);
            builder.WithAll<NewSpawn>();
            m_NewSpawnQuery = state.GetEntityQuery(builder);
            state.RequireForUpdate<Config>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            const int count = 200;
            const float spawnWait = 0.05f;  // 0.05 seconds
            
            spawnTimer -= SystemAPI.Time.DeltaTime;
            if (spawnTimer > 0)
            {
                return;
            }
            
            spawnTimer = spawnWait;

            var prefab = SystemAPI.GetSingleton<Config>().Prefab;
            
            // Remove the NewSpawn tag component from the entities spawned in the prior frame.
            state.EntityManager.RemoveComponent<NewSpawn>(m_NewSpawnQuery);
            
            // Spawn the boxes
            state.EntityManager.Instantiate(prefab, count, Allocator.Temp);
             
            // Every spawned box needs a unique seed, so the
            // seedOffset must be incremented by the number of boxes every frame.
            seedOffset += count;
            var job = new RandomPositionJob { SeedOffset = seedOffset };
            job.ScheduleParallel();
        }
    }
}