using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace Samples.HelloNetcode
{
    public struct HealthBarSpawner : IComponentData
    {
        public UnityObjectRef<GameObject> HealthBarPrefab;
        public float OpponentHeightOffset;
        public float PlayerTowardCameraOffset;
        public float PlayerHeightOffset;
    }

    public class HealthBarSpawnerAuthoring : MonoBehaviour
    {
        public GameObject HealthBarPrefab;
        public float OpponentHeightOffset = 0.5f;
        public float PlayerTowardCameraOffset = 1.8f;
        public float PlayerHeightOffset = -1.5f;

        class Baker : Baker<HealthBarSpawnerAuthoring>
        {
            public override void Bake(HealthBarSpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new HealthBarSpawner
                {
                    HealthBarPrefab = authoring.HealthBarPrefab,
                    OpponentHeightOffset = authoring.OpponentHeightOffset,
                    PlayerTowardCameraOffset = authoring.PlayerTowardCameraOffset,
                    PlayerHeightOffset = authoring.PlayerHeightOffset,
                });
            }
        }
    }
}

