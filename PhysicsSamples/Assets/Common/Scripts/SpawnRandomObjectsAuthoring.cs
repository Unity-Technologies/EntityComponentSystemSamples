using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Common.Scripts
{
    class SpawnRandomObjectsAuthoring : SpawnRandomObjectsAuthoringBase<SpawnSettings>
    {
        class Baker : SpawnRandomObjectsAuthoringBaseBaker<SpawnRandomObjectsAuthoring, SpawnSettings>
        {
        }
    }

    abstract class SpawnRandomObjectsAuthoringBase<T> : MonoBehaviour
        where T : unmanaged, IComponentData, ISpawnSettings
    {
        public GameObject prefab;
        public float3 range = new float3(10f);
        [Tooltip("Limited to 500 on some platforms!")]
        public int count;
        // The random seed used for spawners is a function of the spawner's quantized position, range, count, etc.
        // See the GetRandomSeed() method.
        // Two spawners in the same scene can potentially end up with the same random seed. If so, this field gives scene
        // authors a way to tweak one of the spawners' seeds without changing its behavior-defining parameters.
        public int randomSeedOffset;

        internal virtual void Configure(ref T spawnSettings) {}
        internal virtual void Configure(List<GameObject> referencedPrefabs) { referencedPrefabs.Add(prefab); }
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
}
