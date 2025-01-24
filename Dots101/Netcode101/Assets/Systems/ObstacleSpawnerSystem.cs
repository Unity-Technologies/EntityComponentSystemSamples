using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace KickBall
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct ObstacleSpawnerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EntityPrefabs>();
            state.RequireForUpdate<ObstacleConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;

            var prefabs = SystemAPI.GetSingleton<EntityPrefabs>();
            var obstacleConfig = SystemAPI.GetSingleton<ObstacleConfig>();

            // For simplicity and consistency, we'll use a fixed random seed value.
            var rand = new Random(123);

            var prefabTransform = state.EntityManager.GetComponentData<LocalTransform>(prefabs.Obstacle);

            // Spawn the obstacles in a grid.
            for (int column = 0; column < obstacleConfig.NumColumns; column++)
            {
                for (int row = 0; row < obstacleConfig.NumRows; row++)
                {
                    var obstacle = state.EntityManager.Instantiate(prefabs.Obstacle);

                    prefabTransform.Position = new float3
                    {
                        x = (column * obstacleConfig.GridCellSize) + rand.NextFloat(obstacleConfig.Offset),
                        y = 0,
                        z = (row * obstacleConfig.GridCellSize) + rand.NextFloat(obstacleConfig.Offset)
                    }; 
                    
                    state.EntityManager.SetComponentData(obstacle, prefabTransform);
                }
            }
        }
    }
}
