using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.CodeGeneratedJobForEach;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

// Systems can schedule work to run on worker threads.
// However, creating and removing Entities can only be done on the main thread to prevent race conditions.
// The system demonstrates an efficient way to batch-instantiate and apply initial transformations to a large
// number of entities.

// ReSharper disable once InconsistentNaming
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class SpawnerSystem_FromEntity : SystemBase
{
    [BurstCompile]
    struct SetSpawnedTranslation : IJobParallelFor
    {
        [NativeDisableParallelForRestriction]
        public ComponentDataFromEntity<Translation> TranslationFromEntity;

        public NativeArray<Entity> Entities;
        public float4x4 LocalToWorld;
        public int Stride;

        public void Execute(int i)
        {
            var entity = Entities[i];
            var y = i / Stride;
            var x = i - (y * Stride);

            TranslationFromEntity[entity] = new Translation()
            {
                Value = math.transform(LocalToWorld, new float3(x * 1.3F, noise.cnoise(new float2(x, y) * 0.21F) * 2, y * 1.3F))
            };
        }
    }

    protected override void OnUpdate()
    {
        Entities.WithStructuralChanges().ForEach((Entity entity, int entityInQueryIndex,
            in Spawner_FromEntity spawnerFromEntity, in LocalToWorld spawnerLocalToWorld) =>
        {
            Dependency.Complete();

            var spawnedCount = spawnerFromEntity.CountX * spawnerFromEntity.CountY;
            var spawnedEntities =
                new NativeArray<Entity>(spawnedCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            EntityManager.Instantiate(spawnerFromEntity.Prefab, spawnedEntities);
            EntityManager.DestroyEntity(entity);

            var translationFromEntity = GetComponentDataFromEntity<Translation>();
            var setSpawnedTranslationJob = new SetSpawnedTranslation
            {
                TranslationFromEntity = translationFromEntity,
                Entities = spawnedEntities,
                LocalToWorld = spawnerLocalToWorld.Value,
                Stride = spawnerFromEntity.CountX
            };
            Dependency = setSpawnedTranslationJob.Schedule(spawnedCount, 64, Dependency);
            Dependency = spawnedEntities.Dispose(Dependency);
        }).Run();
    }
}
