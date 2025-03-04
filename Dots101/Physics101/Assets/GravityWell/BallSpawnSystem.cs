using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace GravityWell
{
    public partial struct BallSpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;  // we want this system to update only once
            var config = SystemAPI.GetSingleton<Config>();
            
            // spawn the balls
            state.EntityManager.Instantiate(config.BallPrefab, config.BallCount, Allocator.Temp);

            // spread out the balls in a grid so they don't spawn on top of each other
            const float spacing = 3;
            const float maxRowSize = 100;
            float minX = -maxRowSize / 2.0f;
            float x = minX;
            float y = 0;
            foreach (var ballTransform in 
                     SystemAPI.Query<RefRW<LocalTransform>>()
                         .WithAll<Ball>())
            {
                ballTransform.ValueRW.Position = new float3(x, y, 0);
                x += spacing;
                if (x > maxRowSize) // cap number of balls in each row of the grid 
                {
                    x = minX;
                    y += spacing;
                }
            }
        }
    }
}