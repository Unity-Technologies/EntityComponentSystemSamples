using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Conversion
{
    public class RotationAuthoring : MonoBehaviour
    {
        public Vector3 LocalAngularVelocity = Vector3.zero; // in degrees/sec

        class Baker : Baker<RotationAuthoring>
        {
            public override void Bake(RotationAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Rotation
                {
                    // We can convert to radians/sec once here.
                    LocalAngularVelocity = math.radians(authoring.LocalAngularVelocity),
                });
            }
        }
    }

    public struct Rotation : IComponentData
    {
        public float3 LocalAngularVelocity; // in radian/sec
    }
}
