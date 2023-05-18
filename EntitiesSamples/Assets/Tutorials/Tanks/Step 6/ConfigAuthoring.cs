using Unity.Entities;
using UnityEngine;

namespace Tutorials.Tanks.Step6
{
    // The SafeZoneRadius is used in Step 8.
    public class ConfigAuthoring : MonoBehaviour
    {
        public GameObject TankPrefab;
        public int TankCount;
        public float SafeZoneRadius;

        class Baker : Baker<ConfigAuthoring>
        {
            public override void Bake(ConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Config
                {
                    TankPrefab = GetEntity(authoring.TankPrefab, TransformUsageFlags.Dynamic),
                    TankCount = authoring.TankCount,
                    SafeZoneRadius = authoring.SafeZoneRadius
                });
            }
        }
    }

    public struct Config : IComponentData
    {
        public Entity TankPrefab;
        public int TankCount;
        public float SafeZoneRadius;
    }
}
