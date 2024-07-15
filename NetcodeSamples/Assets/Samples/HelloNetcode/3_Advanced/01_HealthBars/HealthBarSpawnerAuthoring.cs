using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace Samples.HelloNetcode
{
#if !UNITY_DISABLE_MANAGED_COMPONENTS
    public class HealthBarSpawner : IComponentData
    {
        public GameObject HealthBarPrefab;
        public float OpponentHeightOffset;
        public float PlayerTowardCameraOffset;
        public float PlayerHeightOffset;
    }
#endif

    public class HealthBarSpawnerAuthoring : MonoBehaviour
    {
        public GameObject HealthBarPrefab;
        public float OpponentHeightOffset = 0.5f;
        public float PlayerTowardCameraOffset = 1.8f;
        public float PlayerHeightOffset = -1.5f;
#if !UNITY_DISABLE_MANAGED_COMPONENTS

        class Baker : Baker<HealthBarSpawnerAuthoring>
        {
            public override void Bake(HealthBarSpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponentObject(entity, new HealthBarSpawner
                {
                    HealthBarPrefab = authoring.HealthBarPrefab,
                    OpponentHeightOffset = authoring.OpponentHeightOffset,
                    PlayerTowardCameraOffset = authoring.PlayerTowardCameraOffset,
                    PlayerHeightOffset = authoring.PlayerHeightOffset,
                });
            }
        }
#endif
    }
}

