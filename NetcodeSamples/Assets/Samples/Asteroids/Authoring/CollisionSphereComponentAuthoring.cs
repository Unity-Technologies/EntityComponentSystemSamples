using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[DisallowMultipleComponent]
public class CollisionSphereComponentAuthoring : MonoBehaviour
{
    [RegisterBinding(typeof(CollisionSphereComponent), "radius")]
    public float radius;

    class CollisionSphereComponentBaker : Baker<CollisionSphereComponentAuthoring>
    {
        public override void Bake(CollisionSphereComponentAuthoring authoring)
        {
            CollisionSphereComponent component = default(CollisionSphereComponent);
            component.radius = authoring.radius;
            AddComponent(component);
        }
    }
}
