using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace HelloCube.CrossQuery
{
    public class VelocityAuthoring : MonoBehaviour
    {
        public Vector3 Value;

        class Baker : Baker<VelocityAuthoring>
        {
            public override void Bake(VelocityAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                Velocity component = default(Velocity);
                component.Value = authoring.Value;

                AddComponent(entity, component);
            }
        }
    }

    public struct Velocity : IComponentData
    {
        public float3 Value;
    }

}

