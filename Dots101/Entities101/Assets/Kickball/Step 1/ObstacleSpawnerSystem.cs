using Tutorials.Kickball.Execute;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Tutorials.Kickball.Step1
{
    // The systems in TransformSystemGroup compute the rendering matrix from an entity's LocalTransform component.
    // The UpdateBefore attribute makes this system update before the TransformSystemGroup, and consequently the
    // obstacles we spawn will have their rendering matrix computed in the same frame rather than the next frame.
    // (In this case, most players wouldn't notice the difference without this attribute: obstacles would just
    // appear one frame later.)
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct ObstacleSpawnerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // RequireForUpdate<T> causes the system to skip updates
            // as long as no instances of component T exist in the world.

            // Normally a system will start updating before the main scene is loaded. By using RequireForUpdate,
            // we can make a system skip updating until certain components are loaded from the scene.

            // This system needs to access the singleton component Config, which
            // won't exist until the scene has loaded.
            state.RequireForUpdate<Config>();

            // The Execute* components in this sample are used to control which systems run in which scenes.
            // By adding the ExecuteAuthoring component to a GameObject in the sub scene and 
            // checking the ObstacleSpawner checkbox, an instance of this type will be created in 
            // the scene, and so this system will start updating when the scene loads.
            state.RequireForUpdate<ObstacleSpawner>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // We only want to spawn obstacles one time. Disabling the system stops subsequent updates.
            state.Enabled = false;

            // GetSingleton and SetSingleton are conveniences for accessing
            // a "singleton" component (a component type that only one entity has).
            // If 0 entities or 2 or more entities have the Config component, this GetSingleton() call will throw.
            var config = SystemAPI.GetSingleton<Config>();

            // For simplicity and consistency, we'll use a fixed random seed value.
            var rand = new Random(123);
            var scale = config.ObstacleRadius * 2;

            // Spawn the obstacles in a grid.
            for (int column = 0; column < config.NumColumns; column++)
            {
                for (int row = 0; row < config.NumRows; row++)
                {
                    // Instantiate copies an entity: a new entity is created with all the same component types
                    // and component values as the ObstaclePrefab entity.
                    var obstacle = state.EntityManager.Instantiate(config.ObstaclePrefab);

                    // Position the new obstacle by setting its LocalTransform component.
                    state.EntityManager.SetComponentData(obstacle, new LocalTransform
                    {
                        Position = new float3
                        {
                            x = (column * config.ObstacleGridCellSize) + rand.NextFloat(config.ObstacleOffset),
                            y = 0,
                            z = (row * config.ObstacleGridCellSize) + rand.NextFloat(config.ObstacleOffset)
                        },
                        Scale = scale,
                        Rotation = quaternion.identity
                    });
                }
            }
        }
    }
}
