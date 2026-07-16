using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace HelloCube.RandomSpawn
{
    public partial struct SpawnSystem : ISystem
    {
        uint m_SeedOffset;
        float m_SpawnTimer;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<ExecuteRandomSpawn>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            const int count = 200;
            const float spawnWait = 0.05f; // 0.05 seconds

            m_SpawnTimer -= SystemAPI.Time.DeltaTime;
            if (m_SpawnTimer > 0)
            {
                return;
            }

            m_SpawnTimer = spawnWait;

            // Remove the NewSpawn tag component from the entities spawned in the prior frame.
            var newSpawnQuery = SystemAPI.QueryBuilder().WithAll<NewSpawn>().Build();
            state.EntityManager.RemoveComponent<NewSpawn>(newSpawnQuery);

            // Spawn the boxes
            var prefab = SystemAPI.GetSingleton<Config>().Prefab;
            state.EntityManager.Instantiate(prefab, count, Allocator.Temp);

            // Every spawned box needs a unique seed, so the
            // seedOffset must be incremented by the number of boxes every frame.
            m_SeedOffset += count;

            new RandomPositionJob { SeedOffset = m_SeedOffset }.ScheduleParallel();
        }
    }

    [WithAll(typeof(NewSpawn))]
    [BurstCompile]
    partial struct RandomPositionJob : IJobEntity
    {
        public uint SeedOffset;

        void Execute([EntityIndexInQuery] int index, ref LocalTransform transform)
        {
            // Random instances with similar seeds produce similar results, so to get proper
            // randomness here, we use CreateFromIndex, which hashes the seed.
            var random = Random.CreateFromIndex(SeedOffset + (uint)index);
            var xz = random.NextFloat2Direction() * 50;
            transform.Position = new float3(xz[0], 50, xz[1]);
        }
    }
}
