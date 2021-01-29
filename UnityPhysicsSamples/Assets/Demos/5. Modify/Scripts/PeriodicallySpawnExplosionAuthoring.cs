using System.Collections.Generic;
using Unity.Assertions;
using Unity.Entities;
using Unity.Mathematics;
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
    public int SpawnRate { get; set; }
    public int DeathRate { get; set; }

    public int Id;
}

public class PeriodicallySpawnExplosionAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    public float3 Range;
    public int SpawnRate;
    public GameObject Prefab;
    public int Count = 1;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new PeriodicallySpawnExplosionComponent
        {
            Count = Count,
            DeathRate = 10,
            Position = transform.position,
            Prefab = conversionSystem.GetPrimaryEntity(Prefab),
            Range = Range,
            Rotation = quaternion.identity,
            SpawnRate = SpawnRate,
            Id = 0,
        });
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs) => referencedPrefabs.Add(Prefab);
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(BuildPhysicsWorld))]
class PeriodicallySpawnExplosionsSystem : PeriodicalySpawnRandomObjectsSystem<PeriodicallySpawnExplosionComponent>
{
    internal override void ConfigureInstance(Entity instance, ref PeriodicallySpawnExplosionComponent spawnSettings)
    {
        Assert.IsTrue(EntityManager.HasComponent<SpawnExplosionSettings>(instance));

        var explosionComponent = EntityManager.GetComponentData<SpawnExplosionSettings>(instance);
        var pos = EntityManager.GetComponentData<Translation>(instance);

        spawnSettings.Id--;

        // Setting the ID of a new explosion group
        // so that the group gets unique collider
        explosionComponent.Id = spawnSettings.Id;
        explosionComponent.Position = pos.Value;

        EntityManager.SetComponentData(instance, explosionComponent);
    }
}
