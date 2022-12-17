using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace CrossQuery
{
    public struct Velocity : IComponentData
    {
        public float3 Value;
    }

    public class VelocityAuthoring : MonoBehaviour
    {
        public Vector3 Value;

        class Baker : Baker<VelocityAuthoring>
        {
            public override void Bake(VelocityAuthoring authoring)
            {
                Velocity component = default(Velocity);
                component.Value = authoring.Value;
                AddComponent(component);
            }
        }
    }
}

