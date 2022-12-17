using System.Collections.Generic;
using Unity.Assertions;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

public struct PeriodicallySpawnExplosionComponent : IComponentData, ISpawnSettings, IPeriodicSpawnSettings
{
    public Entity Prefab { get; set; }
    public float3 Position { get; set; }
    public quaternion Rotation { get; set; }
    public float3 Range { get; set; }
    public int Count { get; set; }
    public int RandomSeedOffset { get; set; }

    public int SpawnRate { get; set; }
    public int DeathRate { get; set; }
    public int Id;
}

public class PeriodicallySpawnExplosionAuthoring : MonoBehaviour
{
    public float3 Range;
    public int SpawnRate;
    public GameObject Prefab;
    public int Count = 1;
}

class PeriodicallySpawnExplosionAuthoringBaking : Baker<PeriodicallySpawnExplosionAuthoring>
{
    public override void Bake(PeriodicallySpawnExplosionAuthoring authoring)
    {
        var transform = GetComponent<Transform>();
        AddComponent(new PeriodicallySpawnExplosionComponent
        {
            Count = authoring.Count,
            DeathRate = 10,
            Position = transform.position,
            Prefab = GetEntity(authoring.Prefab),
            Range = authoring.Range,
            Rotation = quaternion.identity,
            SpawnRate = authoring.SpawnRate,
            Id = 0,
        });
    }
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
partial class PeriodicallySpawnExplosionsSystem : PeriodicalySpawnRandomObjectsSystem<PeriodicallySpawnExplosionComponent>
{
    internal override void ConfigureInstance(Entity instance, ref PeriodicallySpawnExplosionComponent spawnSettings)
    {
        Assert.IsTrue(EntityManager.HasComponent<SpawnExplosionSettings>(instance));

        var explosionComponent = EntityManager.GetComponentData<SpawnExplosionSettings>(instance);
#if !ENABLE_TRANSFORM_V1
        var localTransform = EntityManager.GetComponentData<LocalTransform>(instance);
#else
        var pos = EntityManager.GetComponentData<Translation>(instance);
#endif

        spawnSettings.Id--;

        // Setting the ID of a new explosion group
        // so that the group gets unique collider
        explosionComponent.Id = spawnSettings.Id;
#if !ENABLE_TRANSFORM_V1
        explosionComponent.Position = localTransform.Position;
#else
        explosionComponent.Position = pos.Value;
#endif

        EntityManager.SetComponentData(instance, explosionComponent);
    }
}
