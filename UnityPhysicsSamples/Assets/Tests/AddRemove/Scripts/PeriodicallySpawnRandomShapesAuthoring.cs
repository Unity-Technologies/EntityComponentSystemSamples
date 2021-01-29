using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;

public interface IPeriodicSpawnSettings
{
    int SpawnRate { get; set; }
    int DeathRate { get; set; }
}


class PeriodicallySpawnRandomShapesAuthoring : SpawnRandomObjectsAuthoringBase<PeriodicSpawnSettings>
{
    public int SpawnRate = 50;
    public int DeathRate = 50;

    internal override void Configure(ref PeriodicSpawnSettings spawnSettings)
    {
        spawnSettings.SpawnRate = SpawnRate;
        spawnSettings.DeathRate = DeathRate;
    }
}

struct PeriodicSpawnSettings : IComponentData, ISpawnSettings, IPeriodicSpawnSettings
{
    public Entity Prefab { get; set; }
    public float3 Position { get; set; }
    public quaternion Rotation { get; set; }
    public float3 Range { get; set; }
    public int Count { get; set; }

    public int SpawnRate { get; set; }
    public int DeathRate { get; set; }
}

abstract class PeriodicalySpawnRandomObjectsSystem<T> : SpawnRandomObjectsSystemBase<T> where T : struct, ISpawnSettings, IPeriodicSpawnSettings, IComponentData
{
    public int FrameCount = 0;

    internal override int GetRandomSeed(T spawnSettings)
    {
        int seed = base.GetRandomSeed(spawnSettings);
        seed = (seed * 397) ^ spawnSettings.Prefab.GetHashCode();
        seed = (seed * 397) ^ spawnSettings.DeathRate;
        seed = (seed * 397) ^ spawnSettings.SpawnRate;
        seed = (seed * 397) ^ FrameCount;
        return seed;
    }

    internal override void OnBeforeInstantiatePrefab(ref T spawnSettings)
    {
        if (!EntityManager.HasComponent<LifeTime>(spawnSettings.Prefab))
        {
            EntityManager.AddComponent<LifeTime>(spawnSettings.Prefab);
        }
        EntityManager.SetComponentData(spawnSettings.Prefab, new LifeTime { Value = spawnSettings.DeathRate });
    }

    protected override void OnUpdate()
    {
        var lFrameCount = FrameCount;

        // Entities.ForEach in generic system types are not supported
        using (var entities = GetEntityQuery(new ComponentType[] { typeof(T) }).ToEntityArray(Allocator.TempJob))
        {
            for (int j = 0; j < entities.Length; j++)
            {
                var entity = entities[j];
                var spawnSettings = EntityManager.GetComponentData<T>(entity);

                if (lFrameCount % spawnSettings.SpawnRate == 0)
                {
                    var count = spawnSettings.Count;

                    OnBeforeInstantiatePrefab(ref spawnSettings);

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
                        ConfigureInstance(instance, ref spawnSettings);
                    }
                }

                EntityManager.SetComponentData<T>(entity, spawnSettings);
            }
        }
        FrameCount++;
    }
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(BuildPhysicsWorld))]
class PeriodicallySpawnRandomShapeSystem : PeriodicalySpawnRandomObjectsSystem<PeriodicSpawnSettings>
{}
