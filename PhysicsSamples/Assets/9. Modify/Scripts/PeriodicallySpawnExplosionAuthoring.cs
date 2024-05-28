// This code is used in the 5g2. Unique Collider Blob Sharing demo and inherits from SpawnRandomObjectsSystemBase
// The OnUpdate method in SpawnRandomObjectsSystemBase will spawn an explosion group where this is defined in
// ConfigureInstance(). The prefab being spawned is ExplosionSpawner.
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
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new PeriodicallySpawnExplosionComponent
        {
            Count = authoring.Count,
            DeathRate = 10,
            Position = transform.position,
            Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
            Range = authoring.Range,
            Rotation = quaternion.identity,
            SpawnRate = authoring.SpawnRate,
            Id = 0,
        });
    }
}

// The data set in ConfigureInstance feeds into the OnUpdate method of PeriodicalySpawnRandomObjectsSystem. This system
// updates the ExplosionSpawner prefab (the fireworks rocket pre-explosion).
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
partial class PeriodicallySpawnExplosionsSystem : PeriodicalySpawnRandomObjectsSystem<PeriodicallySpawnExplosionComponent>
{
    internal override void ConfigureInstance(Entity instance, ref PeriodicallySpawnExplosionComponent spawnSettings)
    {
        Assert.IsTrue(EntityManager.HasComponent<SpawnExplosionSettings>(instance));

        var explosionComponent = EntityManager.GetComponentData<SpawnExplosionSettings>(instance);

        var localTransform = EntityManager.GetComponentData<LocalTransform>(instance);

        // Want a negative ID to use with the CollisionFilter GroupIndex
        spawnSettings.Id--;

        // Setting the ID of a new explosion group so that the group gets unique collider
        explosionComponent.Id = spawnSettings.Id;

        explosionComponent.Position = localTransform.Position;

        EntityManager.SetComponentData(instance, explosionComponent);
    }
}
