using Tutorials.Kickball.Execute;
using Tutorials.Kickball.Step1;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Tutorials.Kickball.Step2
{
    [UpdateAfter(typeof(ObstacleSpawnerSystem))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct PlayerSpawnerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerSpawner>();
            state.RequireForUpdate<Config>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // We only want to spawn players in one frame. Disabling the system stops it from updating again after this one time.
            state.Enabled = false;

            var config = SystemAPI.GetSingleton<Config>();

            #if true
                // higher-level API
                // This "foreach query" is transformed by source-gen into code resembling the #else below.
                // For every entity having a LocalTransform and Obstacle component, a read-only reference to
                // the LocalTransform is assigned to 'obstacleTransform'.
                foreach (var obstacleTransform in
                         SystemAPI.Query<RefRO<LocalTransform>>().
                             WithAll<Obstacle>())
                {
                    // Create a player entity from the prefab.
                    var player = state.EntityManager.Instantiate(config.PlayerPrefab);

                    // Set the new player's transform (a position offset from the obstacle).
                    state.EntityManager.SetComponentData(player, new LocalTransform
                    {
                        Position = new float3
                        {
                            x = obstacleTransform.ValueRO.Position.x + config.PlayerOffset,
                            y = 1,
                            z = obstacleTransform.ValueRO.Position.z + config.PlayerOffset
                        },
                        Scale = 1,  // If we didn't set Scale and Rotation, they would default to zero (which is bad!)
                        Rotation = quaternion.identity
                    });
                }
            #else
                // lower-level API
                // Get a query that matches all entities which have both a LocalTransform and Obstacle component.
                var query = SystemAPI.QueryBuilder().WithAll<LocalTransform, Obstacle>().Build();

                // Type handles are needed to access component data arrays from chunks.
                var localTransformTypeHandle = SystemAPI.GetComponentTypeHandle<LocalTransform>(true);

                // Perform the query: returns all the chunks with entities matching the query.
                var chunks = query.ToArchetypeChunkArray(Allocator.Temp);
                foreach (var chunk in chunks)
                {
                    // Use the LocalTransform type handle to get the LocalTransform component data array from the chunk.
                    // Be clear that this is not a copy! This is the actual component array stored in the chunk, so
                    // modifying its contents directly modifies the LocalTransform values of the entities.
                    // Because the array belongs to the chunk, you need not (and should not) dispose it.
                    var localTransforms = chunk.GetNativeArray(ref localTransformTypeHandle);

                    // Iterate through every entity in the chunk.
                    for (int i = 0; i < chunk.Count; i++)
                    {
                        // Directly read the component value from the component data array.
                        var obstacleTransform = localTransforms[i];

                        // Same player instantiation code as above.
                        var player = state.EntityManager.Instantiate(config.PlayerPrefab);
                        state.EntityManager.SetComponentData(player, new LocalTransform
                        {
                            Position = new float3
                            {
                                x = obstacleTransform.Position.x + config.PlayerOffset,
                                y = 1,
                                z = obstacleTransform.Position.z + config.PlayerOffset
                            },
                            Scale = 1,
                            Rotation = quaternion.identity
                        });
                    }
                }
            #endif
        }
    }
}
