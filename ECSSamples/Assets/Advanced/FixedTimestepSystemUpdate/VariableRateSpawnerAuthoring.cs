using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Samples.FixedTimestepSystem.Authoring
{
    [AddComponentMenu("DOTS Samples/FixedTimestepWorkaround/Variable Rate Spawner")]
    public class VariableRateSpawnerAuthoring : MonoBehaviour
    {
        public GameObject projectilePrefab;

        class Baker : Baker<VariableRateSpawnerAuthoring>
        {
            public override void Bake(VariableRateSpawnerAuthoring authoring)
            {
                var transform = GetComponent<Transform>();
                var spawnerData = new VariableRateSpawner
                {
                    Prefab = GetEntity(authoring.projectilePrefab),
                    SpawnPos = transform.position,
                };
                AddComponent(spawnerData);
            }
        }
    }
}
