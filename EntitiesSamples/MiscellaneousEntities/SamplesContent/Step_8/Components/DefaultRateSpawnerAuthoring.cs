using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Samples.FixedTimestep
{
    public struct DefaultRateSpawner : IComponentData
    {
        public Entity Prefab;
        public float3 SpawnPos;
    }
    
    public class DefaultRateSpawnerAuthoring : MonoBehaviour
    {
        public GameObject projectilePrefab;

        class Baker : Baker<DefaultRateSpawnerAuthoring>
        {
            public override void Bake(DefaultRateSpawnerAuthoring authoring)
            {
                var transform = GetComponent<Transform>();
                var spawnerData = new DefaultRateSpawner
                {
                    Prefab = GetEntity(authoring.projectilePrefab),
                    SpawnPos = transform.position,
                };
                AddComponent(spawnerData);
            }
        }
    }
}
