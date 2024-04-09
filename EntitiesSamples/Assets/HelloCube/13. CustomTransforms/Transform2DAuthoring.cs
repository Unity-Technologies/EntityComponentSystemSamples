using System.Globalization;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace HelloCube.CustomTransforms
{
    public class Transform2DAuthoring : MonoBehaviour
    {
        class Baker : Baker<Transform2DAuthoring>
        {
            public override void Bake(Transform2DAuthoring authoring)
            {
                // Ensure that no standard transform components are added.
                var entity = GetEntity(TransformUsageFlags.ManualOverride);
                AddComponent(entity, new LocalTransform2D
                {
                    Scale = 1
                });
                AddComponent(entity, new LocalToWorld
                {
                    Value = float4x4.Scale(1)
                });

                var parentGO = authoring.transform.parent;
                if (parentGO != null)
                {
                    AddComponent(entity, new Parent
                    {
                        Value = GetEntity(parentGO, TransformUsageFlags.None)
                    });
                }
            }
        }
    }

    // By including LocalTransform2D in the LocalToWorld write group, entities with LocalTransform2D
    // are not processed by the standard transform system.
    [WriteGroup(typeof(LocalToWorld))]
    public struct LocalTransform2D : IComponentData
    {
        public float2 Position;
        public float Scale;
        public float Rotation;

        public override string ToString()
        {
            return
                $"Position={Position.ToString()} Rotation={Rotation.ToString()} Scale={Scale.ToString(CultureInfo.InvariantCulture)}";
        }

        public float4x4 ToMatrix()
        {
            quaternion rotation = quaternion.RotateZ(math.radians(Rotation));
            return float4x4.TRS(new float3(Position.xy, 0f), rotation, Scale);
        }
    }
}
