using Unity.Entities;
using UnityEngine;

namespace HelloCube.ClosestTarget
{
    public class SettingsAuthoring : MonoBehaviour
    {
        public int UnitCount;
        public GameObject UnitPrefab;

        public int TargetCount;
        public GameObject TargetPrefab;

        public TargetingSystem.SpatialPartitioningType SpatialPartitioning;

        class Baker : Baker<SettingsAuthoring>
        {
            public override void Bake(SettingsAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new Settings
                {
                    UnitCount = authoring.UnitCount,
                    UnitPrefab = GetEntity(authoring.UnitPrefab, TransformUsageFlags.Dynamic),
                    TargetCount = authoring.TargetCount,
                    TargetPrefab = GetEntity(authoring.TargetPrefab, TransformUsageFlags.Dynamic),
                    SpatialPartitioning = authoring.SpatialPartitioning
                });
            }
        }
    }

    public struct Settings : IComponentData
    {
        public int UnitCount;
        public Entity UnitPrefab;

        public int TargetCount;
        public Entity TargetPrefab;

        public TargetingSystem.SpatialPartitioningType SpatialPartitioning;
    }
}
