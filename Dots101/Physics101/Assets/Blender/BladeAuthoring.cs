using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Blender
{
    public class BladeAuthoring : MonoBehaviour
    {
        public Vector3 RotatationAxis;
        
        public class Baker : Baker<BladeAuthoring>
        {
            public override void Bake(BladeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Blade
                {
                    RotationAxis = authoring.RotatationAxis,
                });
            }
        }
    }

    public struct Blade : IComponentData
    {
        public float3 RotationAxis;
    }
}

