using Common.Scripts;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace PlanetGravity
{
    partial class AsteroidSpawnSystem : SpawnRandomObjectsSystemBase<AsteroidSpawn>
    {
        Random m_RandomMass;

        internal override int GetRandomSeed(AsteroidSpawn spawnSettings)
        {
            var seed = base.GetRandomSeed(spawnSettings);
            // Historical note: this used to be "^ spawnSettings.Prefab.GetHashCode(), but the prefab's hash wasn't stable across code changes.
            // Now it's hard-coded. If two spawners in the same scene differ only by the prefab they spawn, set their RandomSeedOffset field
            // to different values to differentiate them.
            seed = (seed * 397) ^ 220;
            seed = (seed * 397) ^ (int)(spawnSettings.MassFactor * 1000);
            return seed;
        }

        internal override void OnBeforeInstantiatePrefab(ref AsteroidSpawn spawnSettings)
        {
            m_RandomMass = new Random();
            m_RandomMass.InitState(10);
        }

        internal override void ConfigureInstance(Entity instance, ref AsteroidSpawn spawnSettings)
        {
            var mass = EntityManager.GetComponentData<PhysicsMass>(instance);
            var halfMassFactor = spawnSettings.MassFactor * 0.5f;
            mass.InverseMass = m_RandomMass.NextFloat(mass.InverseMass * math.rcp(halfMassFactor),
                mass.InverseMass * halfMassFactor);
            EntityManager.SetComponentData(instance, mass);
        }
    }
}
