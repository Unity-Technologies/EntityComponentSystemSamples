using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace HelloCube.Prefabs
{
    [UpdateInGroup(typeof(PrefabsGroup))]
    [BurstCompile]
    public partial struct SpawnSystem : ISystem
    {
        EntityQuery m_SpinningCubes;
        uint m_UpdateCounter;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // This adds a condition to the system updating: the system will not
            // update unless at least one entity exists having the Spawner component.
            state.RequireForUpdate<Spawner>();

            // Create a query that matches all entities having a RotationSpeed component.
            var queryBuilder = new EntityQueryBuilder(Allocator.Temp);
            queryBuilder.WithAll<RotationSpeed>();
            m_SpinningCubes = state.GetEntityQuery(queryBuilder);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Only spawn cubes when no cubes currently exist.
            if (m_SpinningCubes.IsEmpty)
            {
                var prefab = SystemAPI.GetSingleton<Spawner>().Prefab;

                // Instantiating an entity creates copy entities with the same component types and values.
                var instances = state.EntityManager.Instantiate(prefab, 500, Allocator.Temp);

                // Unlike new Random(), CreateFromIndex() hashes the random seed
                // so that similar seeds don't produce similar results.
                var random = Random.CreateFromIndex(m_UpdateCounter++);

                foreach (var entity in instances)
                {
                    var position = (random.NextFloat3() - new float3(0.5f, 0, 0.5f)) * 20;

                    // Get a TransformAspect instance wrapping the entity.
                    var transform = SystemAPI.GetAspectRW<TransformAspect>(entity);
                    transform.LocalPosition = position;
                }
            }
        }
    }
}
