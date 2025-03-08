using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace BreakingBricks
{
    public partial struct BallSystem : ISystem
    {
        private float spawnTimer;
        private uint seed;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BreakingBricks.Config>();
            seed = 1;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<Config>();

            // spawn balls
            if (spawnTimer <= 0)
            {
                spawnTimer = config.BallSpawnInterval;

                var newBalls =
                    state.EntityManager.Instantiate(config.BallPrefab, config.NumBallsSpawn, Allocator.Temp);
                var rand = Random.CreateFromIndex(++seed);

                var min = config.SpawnBoundsMin;
                var max = config.SpawnBoundsMax;
                min.y += config.BallSpawnHeight;
                max.y += config.BallSpawnHeight;

                foreach (var ball in newBalls)
                {
                    var trans = SystemAPI.GetComponentRW<LocalTransform>(ball);
                    var pos = rand.NextFloat3(min, max);
                    trans.ValueRW.Position = pos;
                }
            }

            var dt = SystemAPI.Time.DeltaTime;
            spawnTimer -= dt;

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // decrement ball when they fall below a certain Y-axis position 
            foreach (var (ballTransform, entity) in
                     SystemAPI.Query<RefRO<LocalTransform>>()
                         .WithAll<Ball>()
                         .WithEntityAccess())
            {
                if (ballTransform.ValueRO.Position.y <= config.BallDespawnHeight)
                {
                    ecb.DestroyEntity(entity);
                }
            }

            ecb.Playback(state.EntityManager);
        }
    }
}