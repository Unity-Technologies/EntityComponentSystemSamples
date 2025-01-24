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
        uint seedOffset;
        float spawnTimer;

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

            spawnTimer -= SystemAPI.Time.DeltaTime;
            if (spawnTimer > 0)
            {
                return;
            }

            spawnTimer = spawnWait;

            // Remove the NewSpawn tag component from the entities spawned in the prior frame.
            var newSpawnQuery = SystemAPI.QueryBuilder().WithAll<NewSpawn>().Build();
            state.EntityManager.RemoveComponent<NewSpawn>(newSpawnQuery);

            // Spawn the boxes
            var prefab = SystemAPI.GetSingleton<Config>().Prefab;
            state.EntityManager.Instantiate(prefab, count, Allocator.Temp);

            // Every spawned box needs a unique seed, so the
            // seedOffset must be incremented by the number of boxes every frame.
            seedOffset += count;

            new RandomPositionJob
            {
                SeedOffset = seedOffset
            }.ScheduleParallel();
        }
    }

    [WithAll(typeof(NewSpawn))]
    [BurstCompile]
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
}
