// The purpose of this code is to build a grid of tiles (from prefabs). Each tile has a TileTriggerCounter component.
// The Sphere GameObject can be moved along the grid. Walls prevent the Sphere from rolling off the grid. Trigger events
// are recorded in the TileTriggerCounter component when the Sphere collides with a tile. Reactions to the trigger events
// are handled in the SpawnColliderFromTriggerSystem.
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Unity.Physics
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    internal partial struct CreateTileGridSystem : ISystem
    {
        private EntityQuery wallQuery;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CreateTileGridSpawnerComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var entityManager = state.World.EntityManager;

            foreach (var(creator, creatorEntity) in
                     SystemAPI.Query<RefRW<CreateTileGridSpawnerComponent>>()
                         .WithEntityAccess())
            {
                var initialTransform = entityManager.GetComponentData<LocalTransform>(creator.ValueRO.GridEntity);

                int gridSize = 14; // Create 14x14 grid
                var positions = ComputeGridPositions(gridSize, creator.ValueRO.SpawningPosition);

                var spawnedEntities = new NativeArray<Entity>(gridSize * gridSize, Allocator.Temp);

                ecb.Instantiate(creator.ValueRO.GridEntity, spawnedEntities);

                var i = 0;
                foreach (var s in spawnedEntities)
                {
                    ecb.SetComponent(s, new LocalTransform
                    {
                        Position = positions[i],
                        Scale = initialTransform.Scale,
                        Rotation = initialTransform.Rotation
                    });
                    i++;
                }

                // Instantiate the Walls entity. Note that this prefab contains child entities, therefore we cannot
                // update the position this pass. Will need a separate pass to do this
                var wallPrefabInstance = ecb.Instantiate(creator.ValueRO.WallEntity);
                ecb.SetComponent(wallPrefabInstance, new LocalTransform
                {
                    Position = creator.ValueRO.SpawningPosition,
                    Scale = initialTransform.Scale,
                    Rotation = initialTransform.Rotation
                });
                ecb.AddComponent(wallPrefabInstance, new WallsTagComponent()); // Tag the wall so the entity is easy to find next pass

                spawnedEntities.Dispose();
                positions.Dispose();
            }

            ecb.Playback(entityManager);
            ecb.Dispose();

            // Perform a second pass to update the position of the Walls entity. Need to use the output from the first
            // ECB playback here.
            wallQuery = entityManager.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<WallsTagComponent>()
                },
            });

            var wallArray = wallQuery.ToEntityArray(Allocator.Temp);
            foreach (var wall in wallArray)
            {
                if (entityManager.HasBuffer<LinkedEntityGroup>(wall))
                {
                    var leg = entityManager.GetBuffer<LinkedEntityGroup>(wall);

                    if (leg.Length > 1)
                    {
                        for (var j = 1; j < leg.Length; j++)
                        {
                            var childEntity = leg[j].Value;
                            var childPosition = entityManager.GetComponentData<LocalTransform>(childEntity);
                            var wallPosition = entityManager.GetComponentData<LocalTransform>(wall);
                            entityManager.SetComponentData(childEntity, new LocalTransform
                            {
                                Position = wallPosition.Position + childPosition.Position + new float3(0, 1.5f, 0),
                                Scale = childPosition.Scale,
                                Rotation = childPosition.Rotation
                            });
                        }
                    }
                }
            }
            wallArray.Dispose();

            entityManager.DestroyEntity(SystemAPI.QueryBuilder().WithAll<CreateTileGridSpawnerComponent>().Build());
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        // Create a grid of tiles. The startingPoint marks the middle of the grid
        internal static NativeList<float3> ComputeGridPositions(int gridSize, float3 startingPosition)
        {
            var arrayPositions = new NativeList<float3>(gridSize * gridSize, Allocator.Temp);
            int gridRadius = 1;
            var startingOffset = startingPosition -
                new float3(0.5f * gridSize * gridRadius, 0, 0.5f * gridSize * gridRadius) + 0.5f;

            for (int i = 0; i < gridSize; ++i)
            {
                for (int j = 0; j < gridSize; ++j)
                {
                    arrayPositions.Add(startingOffset + new float3(
                        i * gridRadius, 0, j * gridRadius));

                    if (arrayPositions.Length >= gridSize * gridSize) break;
                }
            }

            return arrayPositions;
        }
    }
}
