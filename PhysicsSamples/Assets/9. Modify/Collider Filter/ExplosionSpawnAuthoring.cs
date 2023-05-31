using Common.Scripts;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Modify
{
    public class ExplosionSpawnAuthoring : MonoBehaviour
    {
        public float3 Range;
        public int SpawnRate;
        public GameObject Prefab;
        public int Count = 1;

        class Baker : Baker<ExplosionSpawnAuthoring>
        {
            public override void Bake(ExplosionSpawnAuthoring authoring)
            {
                var transform = GetComponent<Transform>();
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new ExplosionSpawn
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
    }

    public struct ExplosionSpawn : IComponentData, ISpawnSettings, IPeriodicSpawnSettings
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
}
