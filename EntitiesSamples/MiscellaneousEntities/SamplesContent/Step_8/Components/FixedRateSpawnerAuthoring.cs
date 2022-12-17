using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Samples.FixedTimestep
{
    public struct FixedRateSpawner : IComponentData
    {
        public Entity Prefab;
        public float3 SpawnPos;
    }
    
    public class FixedRateSpawnerAuthoring : MonoBehaviour
    {
        public GameObject projectilePrefab;

        class Baker : Baker<FixedRateSpawnerAuthoring>
        {
            public override void Bake(FixedRateSpawnerAuthoring authoring)
            {
                var transform = authoring.GetComponent<Transform>();
                var spawnerData = new FixedRateSpawner
                {
                    Prefab = GetEntity(authoring.projectilePrefab),
                    SpawnPos = transform.position,
                };
                AddComponent(spawnerData);
            }
        }
    }
}
