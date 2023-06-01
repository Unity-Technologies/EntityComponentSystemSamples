using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace HelloCube.Prefabs
{
    public partial struct SpawnSystem : ISystem
    {
        uint updateCounter;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // This call makes the system not update unless at least one entity in the world exists that has the Spawner component.
            state.RequireForUpdate<Spawner>();

            state.RequireForUpdate<Execute.Prefabs>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Create a query that matches all entities having a RotationSpeed component.
            // (The query is cached in source generation, so this does not incur a cost of recreating it every update.)
            var spinningCubesQuery = SystemAPI.QueryBuilder().WithAll<RotationSpeed>().Build();

            // Only spawn cubes when no cubes currently exist.
            if (spinningCubesQuery.IsEmpty)
            {
                var prefab = SystemAPI.GetSingleton<Spawner>().Prefab;

                // Instantiating an entity creates copy entities with the same component types and values.
                var instances = state.EntityManager.Instantiate(prefab, 500, Allocator.Temp);

                // Unlike new Random(), CreateFromIndex() hashes the random seed
                // so that similar seeds don't produce similar results.
                var random = Random.CreateFromIndex(updateCounter++);

                foreach (var entity in instances)
                {
                    // Update the entity's LocalTransform component with the new position.
                    var transform = SystemAPI.GetComponentRW<LocalTransform>(entity);
                    transform.ValueRW.Position = (random.NextFloat3() - new float3(0.5f, 0, 0.5f)) * 20;
                }
            }
        }
    }
}
