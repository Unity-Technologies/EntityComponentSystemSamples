using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Streaming.SceneManagement.SectionMetadata
{
    public class CircleAuthoring : MonoBehaviour
    {
        public float radius;

        class Baker : Baker<CircleAuthoring>
        {
            public override void Bake(CircleAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new Circle
                {
                    Radius = authoring.radius,
                    Center = GetComponent<Transform>().position
                });
            }
        }
    }

    public struct Circle : IComponentData
    {
        public float Radius; // Proximity radius within which to consider loading a section
        public float3 Center;
    }
}
