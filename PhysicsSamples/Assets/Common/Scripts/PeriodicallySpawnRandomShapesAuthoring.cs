using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Common.Scripts
{
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

    class PeriodicallySpawnRandomShapesAuthoringBaker : SpawnRandomObjectsAuthoringBaseBaker<
        PeriodicallySpawnRandomShapesAuthoring, PeriodicSpawnSettings>
    {
        internal override void Configure(PeriodicallySpawnRandomShapesAuthoring authoring,
            ref PeriodicSpawnSettings spawnSettings)
        {
            spawnSettings.SpawnRate = authoring.SpawnRate;
            spawnSettings.DeathRate = authoring.DeathRate;
        }
    }

    struct PeriodicSpawnSettings : IComponentData, ISpawnSettings, IPeriodicSpawnSettings
    {
        public Entity Prefab { get; set; }
        public float3 Position { get; set; }
        public quaternion Rotation { get; set; }
        public float3 Range { get; set; }
        public int Count { get; set; }
        public int RandomSeedOffset { get; set; }
        public int SpawnRate { get; set; }
        public int DeathRate { get; set; }
    }

    abstract partial class PeriodicalySpawnRandomObjectsSystem<T> :
        SpawnRandomObjectsSystemBase<T> where T : unmanaged, ISpawnSettings, IPeriodicSpawnSettings, IComponentData
    {
        public int FrameCount = 0;

        EntityQuery _Query;

        internal override int GetRandomSeed(T spawnSettings)
        {
            int seed = base.GetRandomSeed(spawnSettings);
            // Historical note: this used to be "^ spawnSettings.Prefab.GetHashCode(), but the prefab's hash wasn't stable across code changes.
            // Now it's hard-coded. If two spawners in the same scene differ only by the prefab they spawn, set their RandomSeedOffset field
            // to different values to differentiate them.
            seed = (seed * 397) ^ 220;
            seed = (seed * 397) ^ spawnSettings.DeathRate;
            seed = (seed * 397) ^ spawnSettings.SpawnRate;
            seed = (seed * 397) ^ FrameCount;
            return seed;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            _Query = GetEntityQuery(typeof(T));
            RequireForUpdate(_Query);
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
            using (var entities = _Query.ToEntityArray(Allocator.TempJob))
            {
                for (int j = 0; j < entities.Length; j++)
                {
                    var entity = entities[j];
                    var spawnSettings = EntityManager.GetComponentData<T>(entity);

                    if (lFrameCount % spawnSettings.SpawnRate == 0)
                    {
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
                        RandomPointsInRange(spawnSettings.Position, spawnSettings.Rotation, spawnSettings.Range,
                            ref positions, ref rotations, GetRandomSeed(spawnSettings));

                        for (int i = 0; i < count; i++)
                        {
                            var instance = instances[i];

                            var transform = EntityManager.GetComponentData<LocalTransform>(instance);
                            transform.Position = positions[i];
                            transform.Rotation = rotations[i];
                            EntityManager.SetComponentData(instance, transform);

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
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    partial class PeriodicallySpawnRandomShapeSystem : PeriodicalySpawnRandomObjectsSystem<PeriodicSpawnSettings>
    {
    }
}
