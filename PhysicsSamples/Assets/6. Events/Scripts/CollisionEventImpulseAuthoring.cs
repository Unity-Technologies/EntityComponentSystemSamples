using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Events
{
    public class CollisionEventImpulseAuthoring : MonoBehaviour
    {
        public float Magnitude = 1.0f;
        public float3 Direction = math.up();

        class Baker : Baker<CollisionEventImpulseAuthoring>
        {
            public override void Bake(CollisionEventImpulseAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new CollisionEventImpulse()
                {
                    Impulse = authoring.Magnitude * authoring.Direction,
                });
            }
        }
    }

    public struct CollisionEventImpulse : IComponentData
    {
        public float3 Impulse;
    }
}
