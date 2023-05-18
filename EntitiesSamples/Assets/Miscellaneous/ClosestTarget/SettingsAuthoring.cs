using Unity.Entities;
using UnityEngine;

namespace Miscellaneous.ClosestTarget
{
    public class SettingsAuthoring : MonoBehaviour
    {
        public int unitCount;
        public GameObject unitPrefab;

        public int targetCount;
        public GameObject targetPrefab;

        public TargetingSystem.SpatialPartitioningType spatialPartitioning;

        class Baker : Baker<SettingsAuthoring>
        {
            public override void Bake(SettingsAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new Settings
                {
                    UnitCount = authoring.unitCount,
                    UnitPrefab = GetEntity(authoring.unitPrefab, TransformUsageFlags.Dynamic),
                    TargetCount = authoring.targetCount,
                    TargetPrefab = GetEntity(authoring.targetPrefab, TransformUsageFlags.Dynamic),
                    SpatialPartitioning = authoring.spatialPartitioning
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
