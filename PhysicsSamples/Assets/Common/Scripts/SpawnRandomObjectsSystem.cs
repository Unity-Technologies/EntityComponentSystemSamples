using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Common.Scripts
{
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

#if UNITY_ANDROID || UNITY_IOS
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
            var random = Random.CreateFromIndex((uint)seed);
            for (int i = 0; i < count; i++)
            {
                positions[i] = center + math.mul(orientation, random.NextFloat3(-range, range));
                rotations[i] = math.mul(random.NextQuaternionRotation(), orientation);
            }
        }
    }
}
