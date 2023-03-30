using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Miscellaneous.FixedTimestep
{
    public class DefaultRateSpawnerAuthoring : MonoBehaviour
    {
        public GameObject projectilePrefab;

        class Baker : Baker<DefaultRateSpawnerAuthoring>
        {
            public override void Bake(DefaultRateSpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                var spawnerData = new DefaultRateSpawner
                {
                    Prefab = GetEntity(authoring.projectilePrefab, TransformUsageFlags.Dynamic),
                    SpawnPos = GetComponent<Transform>().position,
                };
                AddComponent(entity, spawnerData);
            }
        }
    }

    public struct DefaultRateSpawner : IComponentData
    {
        public Entity Prefab;
        public float3 SpawnPos;
    }
}
