using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ContactEventImpulseAuthoring : MonoBehaviour
{
    public float Magnitude = 1.0f;
    public float3 Direction = math.up();

    class Baker : Baker<ContactEventImpulseAuthoring>
    {
        public override void Bake(ContactEventImpulseAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ContactEventImpulse()
            {
                Impulse = authoring.Magnitude * authoring.Direction,
            });
        }
    }
}

public struct ContactEventImpulse : IComponentData
{
    public float3 Impulse;
}
