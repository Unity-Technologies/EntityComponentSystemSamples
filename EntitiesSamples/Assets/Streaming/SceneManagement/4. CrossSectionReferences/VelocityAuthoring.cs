using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace Streaming.SceneManagement.CrossSectionReferences
{
    public class VelocityAuthoring : MonoBehaviour
    {
        public float3 velocity;

        public class VelocityAuthoringBaker : Baker<VelocityAuthoring>
        {
            public override void Bake(VelocityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Velocity
                {
                    Value = authoring.velocity
                });
            }
        }
    }

    public struct Velocity : IComponentData
    {
        public float3 Value;
    }
}
