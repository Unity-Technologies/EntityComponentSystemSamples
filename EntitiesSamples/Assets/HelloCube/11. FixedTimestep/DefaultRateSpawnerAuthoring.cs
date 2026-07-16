using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace HelloCube.FixedTimestep
{
    public class DefaultRateSpawnerAuthoring : MonoBehaviour
    {
        public GameObject ProjectilePrefab;

        class Baker : Baker<DefaultRateSpawnerAuthoring>
        {
            public override void Bake(DefaultRateSpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                var spawnerData = new DefaultRateSpawner
                {
                    Prefab = GetEntity(authoring.ProjectilePrefab, TransformUsageFlags.Dynamic),
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
