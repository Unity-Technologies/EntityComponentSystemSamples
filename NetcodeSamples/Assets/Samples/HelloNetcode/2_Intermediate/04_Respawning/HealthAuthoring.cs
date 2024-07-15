using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Samples.HelloNetcode
{
    /// <remarks>
    /// short is used so that we don't have to deal with floating point imprecision,
    /// and to ensure that we can go negative, which could come into play if there was healing or similar positive HP operations.
    /// </remarks>
    public struct Health : IComponentData
    {
        [GhostField(Smoothing = SmoothingAction.Clamp)] public short MaximumHitPoints;
        [GhostField(Smoothing = SmoothingAction.Clamp)] public short CurrentHitPoints;
    }

    public class HealthAuthoring : MonoBehaviour
    {
        public short MaximumHitPoints = 100;

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

