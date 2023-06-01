using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Modify
{
// In general, you should treat colliders as immutable data at run-time, as several bodies might share the same collider.
// If you plan to modify mesh or convex colliders at run-time, remember to tick the Force Unique box on the PhysicsShapeAuthoring component.
// This guarantees that the PhysicsCollider component will have a unique instance in all cases.
    public class BoxColliderSizeAuthoring : MonoBehaviour
    {
        public float3 Min = 0;
        public float3 Max = 10;

        class Baker : Baker<BoxColliderSizeAuthoring>
        {
            public override void Bake(BoxColliderSizeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.ManualOverride);
                AddComponent(entity, new ChangeBoxColliderSize
                {
                    Min = authoring.Min,
                    Max = authoring.Max,
                    Target = math.lerp(authoring.Min, authoring.Max, 0.5f),
                });

                AddComponent(entity, new PostTransformMatrix
                {
                    Value = float4x4.identity,
                });
            }
        }
    }

    public struct ChangeBoxColliderSize : IComponentData
    {
        public float3 Min;
        public float3 Max;
        public float3 Target;
    }
}
