using Unity.Entities;
using UnityEngine;

namespace ClosestTarget
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
                AddComponent(new Settings
                {
                    UnitCount = authoring.unitCount,
                    UnitPrefab = GetEntity(authoring.unitPrefab),
                    TargetCount = authoring.targetCount,
                    TargetPrefab = GetEntity(authoring.targetPrefab),
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