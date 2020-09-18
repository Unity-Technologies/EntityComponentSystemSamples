using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

class SpawnRandomObjectsAuthoring : SpawnRandomObjectsAuthoringBase<SpawnSettings>
{
}

abstract class SpawnRandomObjectsAuthoringBase<T> : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
    where T : struct, IComponentData, ISpawnSettings
{
    #pragma warning disable 649
    public GameObject prefab;
    public float3 range = new float3(10f);
    public int count;
    #pragma warning restore 649

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var spawnSettings = new T
        {
            Prefab = conversionSystem.GetPrimaryEntity(prefab),
            Position = transform.position,
            Rotation = transform.rotation,
            Range = range,
            Count = count
        };
        Configure(ref spawnSettings);
        dstManager.AddComponentData(entity, spawnSettings);
    }

    internal virtual void Configure(ref T spawnSettings) {}

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs) => referencedPrefabs.Add(prefab);
}

interface ISpawnSettings
{
    Entity Prefab { get; set; }
    float3 Position { get; set; }
    quaternion Rotation { get; set; }
    float3 Range { get; set; }
    int Count { get; set; }
}

struct SpawnSettings : IComponentData, ISpawnSettings
{
    public Entity Prefab { get; set; }
    public float3 Position { get; set; }
    public quaternion Rotation { get; set; }
    public float3 Range { get; set; }
    public int Count { get; set; }
}

class SpawnRandomObjectsSystem : SpawnRandomObjectsSystemBase<SpawnSettings>
{
}

abstract class SpawnRandomObjectsSystemBase<T> : SystemBase where T : struct, IComponentData, ISpawnSettings
{
    internal virtual int GetRandomSeed(T spawnSettings)
    {
        var seed = 0;
        seed = (seed * 397) ^ spawnSettings.Count;
        seed = (seed * 397) ^ (int)math.csum(spawnSettings.Position);
        seed = (seed * 397) ^ (int)math.csum(spawnSettings.Range);
        return seed;
    }

    internal virtual void OnBeforeInstantiatePrefab(T spawnSettings) {}

    internal virtual void ConfigureInstance(Entity instance, T spawnSettings) {}

    protected override void OnUpdate()
    {
        // Entities.ForEach in generic system types are not supported
        using (var entities = GetEntityQuery(new ComponentType[] { typeof(T) }).ToEntityArray(Allocator.TempJob))
        {
            for (int j = 0; j < entities.Length; j++)
            {
                var entity = entities[j];
                var spawnSettings = EntityManager.GetComponentData<T>(entity);

                var count = spawnSettings.Count;

                OnBeforeInstantiatePrefab(spawnSettings);

                var instances = new NativeArray<Entity>(count, Allocator.Temp);
                EntityManager.Instantiate(spawnSettings.Prefab, instances);

                var positions = new NativeArray<float3>(count, Allocator.Temp);
                var rotations = new NativeArray<quaternion>(count, Allocator.Temp);
                RandomPointsInRange(spawnSettings.Position, spawnSettings.Rotation, spawnSettings.Range, ref positions, ref rotations, GetRandomSeed(spawnSettings));

                for (int i = 0; i < count; i++)
                {
                    var instance = instances[i];
                    EntityManager.SetComponentData(instance, new Translation { Value = positions[i] });
                    EntityManager.SetComponentData(instance, new Rotation { Value = rotations[i] });
                    ConfigureInstance(instance, spawnSettings);
                }

                EntityManager.RemoveComponent<T>(entity);
            }
        }
    }

    protected static void RandomPointsInRange(
        float3 center, quaternion orientation, float3 range,
        ref NativeArray<float3> positions, ref NativeArray<quaternion> rotations, int seed = 0)
    {
        var count = positions.Length;
        // initialize the seed of the random number generator
        var random = new Unity.Mathematics.Random((uint)seed);
        for (int i = 0; i < count; i++)
        {
            positions[i] = center + math.mul(orientation, random.NextFloat3(-range, range));
            rotations[i] = math.mul(random.NextQuaternionRotation(), orientation);
        }
    }
}
