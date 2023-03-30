using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class CollisionSphereComponentAuthoring : MonoBehaviour
{
    [RegisterBinding(typeof(CollisionSphereComponent), "radius")]
    public float radius;

    class Baker : Baker<CollisionSphereComponentAuthoring>
    {
        public override void Bake(CollisionSphereComponentAuthoring authoring)
        {
            CollisionSphereComponent component = default(CollisionSphereComponent);
            component.radius = authoring.radius;
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, component);
        }
    }
}
