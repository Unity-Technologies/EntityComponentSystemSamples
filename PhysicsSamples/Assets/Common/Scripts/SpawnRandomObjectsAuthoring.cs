using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

class SpawnRandomObjectsAuthoring : SpawnRandomObjectsAuthoringBase<SpawnSettings>
{
}

abstract class SpawnRandomObjectsAuthoringBase<T> : MonoBehaviour
    where T : unmanaged, IComponentData, ISpawnSettings
{
    #pragma warning disable 649
    public GameObject prefab;
    public float3 range = new float3(10f);
    [Tooltip("Limited to 500 on some platforms!")]
    public int count;
    // The random seed used for spawners is a function of the spawner's quantized position, range, count, etc.
    // See the GetRandomSeed() method.
    // Two spawners in the same scene can potentially end up with the same random seed. If so, this field gives scene
    // authors a way to tweak one of the spawners' seeds without changing its behavior-defining parameters.
    public int randomSeedOffset;
    #pragma warning restore 649

    internal virtual void Configure(ref T spawnSettings) {}
    internal virtual void Configure(List<GameObject> referencedPrefabs) { referencedPrefabs.Add(prefab); }
}

class SpawnRandomObjectsAuthoringBaker : SpawnRandomObjectsAuthoringBaseBaker<SpawnRandomObjectsAuthoring, SpawnSettings>
{
}

abstract class SpawnRandomObjectsAuthoringBaseBaker<T, U> : Baker<T> where T : SpawnRandomObjectsAuthoringBase<U>
    where U : unmanaged, IComponentData, ISpawnSettings
{
    public override void Bake(T authoring)
    {
        var transform = GetComponent<Transform>();
        var spawnSettings = new U
        {
            Prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic),
            Position = transform.position,
            Rotation = transform.rotation,
            Range = authoring.range,
            Count = authoring.count,
            RandomSeedOffset = authoring.randomSeedOffset,
        };
        Configure(authoring, ref spawnSettings, GetEntity(TransformUsageFlags.Dynamic), this);
        Configure(authoring, ref spawnSettings);
        Configure(authoring, this);
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, spawnSettings);
    }

    internal virtual void Configure(T authoring, ref U spawnSettings) {}
    internal virtual void Configure(T authoring, ref U spawnSettings, Entity entity, IBaker baker) {}
    internal virtual void Configure(T authoring, IBaker baker) { GetEntity(authoring.prefab, TransformUsageFlags.Dynamic); }
}

interface ISpawnSettings
{
    Entity Prefab { get; set; }
    float3 Position { get; set; }
    quaternion Rotation { get; set; }
    float3 Range { get; set; }
    int Count { get; set; }
    int RandomSeedOffset { get; set; }
}

struct SpawnSettings : IComponentData, ISpawnSettings
{
    public Entity Prefab { get; set; }
    public float3 Position { get; set; }
    public quaternion Rotation { get; set; }
    public float3 Range { get; set; }
    public int Count { get; set; }
    public int RandomSeedOffset { get; set; }
}

partial class SpawnRandomObjectsSystem : SpawnRandomObjectsSystemBase<SpawnSettings>
{
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
abstract partial class SpawnRandomObjectsSystemBase<T> : SystemBase where T : unmanaged, IComponentData, ISpawnSettings
{
    internal virtual int GetRandomSeed(T spawnSettings)
    {
        var seed = spawnSettings.RandomSeedOffset;
        seed = (seed * 397) ^ spawnSettings.Count;
        seed = (seed * 397) ^ (int)math.csum(spawnSettings.Position);
        seed = (seed * 397) ^ (int)math.csum(spawnSettings.Range);
        return seed;
    }

    internal virtual void OnBeforeInstantiatePrefab(ref T spawnSettings) {}

    internal virtual void ConfigureInstance(Entity instance, ref T spawnSettings) {}

    protected override void OnUpdate()
    {
        // Entities.ForEach in generic system types are not supported
        using (var entities = GetEntityQuery(new ComponentType[] { typeof(T) }).ToEntityArray(Allocator.TempJob))
        {
            for (int j = 0; j < entities.Length; j++)
            {
                var entity = entities[j];
                var spawnSettings = EntityManager.GetComponentData<T>(entity);

#if UNITY_ANDROID || UNITY_IOS || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                // Limit the number of bodies on platforms with potentially low-end devices
                var count = math.min(spawnSettings.Count, 500);
#else
                var count = spawnSettings.Count;
#endif
                OnBeforeInstantiatePrefab(ref spawnSettings);

                var instances = new NativeArray<Entity>(count, Allocator.Temp);
                EntityManager.Instantiate(spawnSettings.Prefab, instances);

                var positions = new NativeArray<float3>(count, Allocator.Temp);
                var rotations = new NativeArray<quaternion>(count, Allocator.Temp);
                RandomPointsInRange(spawnSettings.Position, spawnSettings.Rotation, spawnSettings.Range, ref positions, ref rotations, GetRandomSeed(spawnSettings));

                for (int i = 0; i < count; i++)
                {
                    var instance = instances[i];

                    var transform = EntityManager.GetComponentData<LocalTransform>(instance);
                    transform.Position = positions[i];
                    transform.Rotation = rotations[i];
                    EntityManager.SetComponentData(instance, transform);

                    ConfigureInstance(instance, ref spawnSettings);
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
        var random = Unity.Mathematics.Random.CreateFromIndex((uint)seed);
        for (int i = 0; i < count; i++)
        {
            positions[i] = center + math.mul(orientation, random.NextFloat3(-range, range));
            rotations[i] = math.mul(random.NextQuaternionRotation(), orientation);
        }
    }
}
