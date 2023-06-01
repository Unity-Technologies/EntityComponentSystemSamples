using Common.Scripts;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Modify
{
    public class ExplosionSpawnSettingsAuthoring : MonoBehaviour
    {
        [Header("Debris")] public GameObject Prefab;
        public int Count;

        [Header("Explosion")] public int Countdown;
        public float Force;

        internal static int Id = -1;

        class Baker : Baker<ExplosionSpawnSettingsAuthoring>
        {
            public override void Bake(ExplosionSpawnSettingsAuthoring authoring)
            {
                var transform = GetComponent<Transform>();
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new ExplosionSpawnSettings
                {
                    Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
                    Position = transform.position,
                    Rotation = quaternion.identity,
                    Count = authoring.Count,

                    Id = ExplosionSpawnSettingsAuthoring.Id--,
                    Countdown = authoring.Countdown,
                    Force = authoring.Force,
                    Source = Entity.Null,
                });
            }
        }
    }

    struct ExplosionSpawnSettings : ISpawnSettings, IComponentData
    {
        public Entity Prefab { get; set; }
        public float3 Position { get; set; }
        public quaternion Rotation { get; set; }
        public float3 Range { get; set; }
        public int Count { get; set; }
        public int RandomSeedOffset { get; set; }

        public int Id;
        public int Countdown;
        public float Force;
        public Entity Source;
    }
}
