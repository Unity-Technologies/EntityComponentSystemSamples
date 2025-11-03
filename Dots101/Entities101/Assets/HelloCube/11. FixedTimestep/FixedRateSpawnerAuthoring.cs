using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace HelloCube.FixedTimestep
{
    public class FixedRateSpawnerAuthoring : MonoBehaviour
    {
        public GameObject ProjectilePrefab;

        class Baker : Baker<FixedRateSpawnerAuthoring>
        {
            public override void Bake(FixedRateSpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                var spawnerData = new FixedRateSpawner
                {
                    Prefab = GetEntity(authoring.ProjectilePrefab, TransformUsageFlags.Dynamic),
                    SpawnPos = GetComponent<Transform>().position,
                };
                AddComponent(entity, spawnerData);
            }
        }
    }

    public struct FixedRateSpawner : IComponentData
    {
        public Entity Prefab;
        public float3 SpawnPos;
    }
}
