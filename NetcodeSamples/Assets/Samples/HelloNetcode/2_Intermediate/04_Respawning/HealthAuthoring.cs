using Unity.Entities;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public struct Health : IComponentData
    {
        public float MaximumHitPoints;
        public float CurrentHitPoints;
    }

    public class HealthAuthoring : MonoBehaviour
    {
        public float MaximumHitPoints = 100;

        class Baker : Baker<HealthAuthoring>
        {
            public override void Bake(HealthAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Health
                {
                    MaximumHitPoints = authoring.MaximumHitPoints,
                    CurrentHitPoints = authoring.MaximumHitPoints,
                });
            }
        }
    }
}

