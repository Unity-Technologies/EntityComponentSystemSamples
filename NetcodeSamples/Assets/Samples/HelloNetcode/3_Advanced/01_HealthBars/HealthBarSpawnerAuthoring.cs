using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Samples.HelloNetcode
{
#if !UNITY_DISABLE_MANAGED_COMPONENTS
    public class HealthBarSpawner : IComponentData
    {
        public GameObject HealthBarPrefab;
        public Vector3 Offset;
    }
#endif

    public class HealthBarSpawnerAuthoring : MonoBehaviour
    {
        public GameObject HealthBarPrefab;
        public float3 Offset;
#if !UNITY_DISABLE_MANAGED_COMPONENTS

        class Baker : Baker<HealthBarSpawnerAuthoring>
        {
            public override void Bake(HealthBarSpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponentObject(entity, new HealthBarSpawner
                {
                    HealthBarPrefab = authoring.HealthBarPrefab,
                    Offset = authoring.Offset,
                });
            }
        }
#endif
    }
}

