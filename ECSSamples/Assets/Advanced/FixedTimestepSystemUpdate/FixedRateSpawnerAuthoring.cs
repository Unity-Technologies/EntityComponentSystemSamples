using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Samples.FixedTimestepSystem.Authoring
{
    [AddComponentMenu("DOTS Samples/FixedTimestepWorkaround/Fixed Rate Spawner")]
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
