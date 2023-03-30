using System;
using Unity.Entities;
using Unity.Physics;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

class SpawnRandomAsteroidsAuthoring : SpawnRandomObjectsAuthoringBase<AsteroidSpawnSettings>
{
    public float massFactor = 1;

    internal override void Configure(ref AsteroidSpawnSettings spawnSettings) => spawnSettings.MassFactor = massFactor;
}

class SpawnRandomAsteroidsAuthoringBaker : SpawnRandomObjectsAuthoringBaseBaker<SpawnRandomAsteroidsAuthoring, AsteroidSpawnSettings>
{
    internal override void Configure(SpawnRandomAsteroidsAuthoring authoring, ref AsteroidSpawnSettings spawnSettings) => spawnSettings.MassFactor = authoring.massFactor;
}

struct AsteroidSpawnSettings : IComponentData, ISpawnSettings
{
    public Entity Prefab { get; set; }
    public float3 Position { get; set; }
    public quaternion Rotation { get; set; }
    public float3 Range { get; set; }
    public int Count { get; set; }
    public int RandomSeedOffset { get; set; }
    public float MassFactor;
}

partial class SpawnRandomAsteroidsSystem : SpawnRandomObjectsSystemBase<AsteroidSpawnSettings>
{
    Random m_RandomMass;

    internal override int GetRandomSeed(AsteroidSpawnSettings spawnSettings)
    {
        var seed = base.GetRandomSeed(spawnSettings);
        // Historical note: this used to be "^ spawnSettings.Prefab.GetHashCode(), but the prefab's hash wasn't stable across code changes.
        // Now it's hard-coded. If two spawners in the same scene differ only by the prefab they spawn, set their RandomSeedOffset field
        // to different values to differentiate them.
        seed = (seed * 397) ^ 220;
        seed = (seed * 397) ^ (int)(spawnSettings.MassFactor * 1000);
        return seed;
    }

    internal override void OnBeforeInstantiatePrefab(ref AsteroidSpawnSettings spawnSettings)
    {
        m_RandomMass = new Random();
        m_RandomMass.InitState(10);
    }

    internal override void ConfigureInstance(Entity instance, ref AsteroidSpawnSettings spawnSettings)
    {
        var mass = EntityManager.GetComponentData<PhysicsMass>(instance);
        var halfMassFactor = spawnSettings.MassFactor * 0.5f;
        mass.InverseMass = m_RandomMass.NextFloat(mass.InverseMass * math.rcp(halfMassFactor), mass.InverseMass * halfMassFactor);
        EntityManager.SetComponentData(instance, mass);
    }
}
