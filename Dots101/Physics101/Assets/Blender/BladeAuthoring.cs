using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Blender
{
    public class BladeAuthoring : MonoBehaviour
    {
        public Vector3 AngularVelocity;
        
        public class Baker : Baker<BladeAuthoring>
        {
            public override void Bake(BladeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Blade
                {
                    AngularVelocity = authoring.AngularVelocity,
                });
            }
        }
    }

    public struct Blade : IComponentData
    {
        public float3 AngularVelocity;
    }
}

