using System;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Mathematics;
using Unity.Scenes;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

public struct PrefabSpawner : IComponentData
{
    public float SpawnsRemaining;
    public float SpawnsPerSecond;
}

public struct PrefabSpawnerBufferElement : IBufferElementData
{
    public EntityPrefabReference Prefab;
}

[RequireMatchingQueriesForUpdate]
public partial class PrefabSpawnerSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem m_BeginSimECBSystem;

    protected override void OnCreate()
    {
        m_BeginSimECBSystem = World.GetExistingSystemManaged<BeginSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var ecb = m_BeginSimECBSystem.CreateCommandBuffer().AsParallelWriter();
        var rnd = new Random((uint)Environment.TickCount);
        Entities.WithNone<RequestEntityPrefabLoaded>().ForEach((Entity entity, int entityInQueryIndex, ref PrefabSpawner spawner, in DynamicBuffer<PrefabSpawnerBufferElement> prefabs) =>
        {
            // Select a random prefab to load
            ecb.AddComponent(entityInQueryIndex, entity, new RequestEntityPrefabLoaded {Prefab = prefabs[rnd.NextInt(prefabs.Length)].Prefab});
        }).ScheduleParallel();

        var dt = SystemAPI.Time.DeltaTime;
        Entities.ForEach((Entity entity, int entityInQueryIndex, ref PrefabSpawner spawner, in PrefabLoadResult prefab) =>
        {
            var remaining = spawner.SpawnsRemaining;
            if (remaining < 0.0f)
            {
                // No more instances left to spawn
                ecb.DestroyEntity(entityInQueryIndex, entity);
                return;
            }
            var newRemaining = remaining - dt * spawner.SpawnsPerSecond;
            var spawnCount = (int) remaining - (int) newRemaining;
            for (int i = 0; i < spawnCount; ++i)
            {
                var instance = ecb.Instantiate(entityInQueryIndex, prefab.PrefabRoot);
                int index = i + (int) remaining;
#if !ENABLE_TRANSFORM_V1
                ecb.SetComponent(entityInQueryIndex, instance, new LocalToWorldTransform {Value = UniformScaleTransform.FromPosition(new float3(index*((index&1)*2-1), 0, 0))});
#else
                ecb.SetComponent(entityInQueryIndex, instance, new Translation {Value = new float3(index*((index&1)*2-1), 0, 0)});
#endif
            }
            spawner.SpawnsRemaining = newRemaining;
        }).ScheduleParallel();
        m_BeginSimECBSystem.AddJobHandleForProducer(Dependency);
    }
}
