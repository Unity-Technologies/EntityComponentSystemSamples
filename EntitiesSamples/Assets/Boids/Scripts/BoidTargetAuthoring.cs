using Unity.Entities;
using UnityEngine;

namespace Boids
{
    public class BoidTargetAuthoring : MonoBehaviour
    {
        class Baker : Baker<BoidTargetAuthoring>
        {
            public override void Bake(BoidTargetAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent(entity, new BoidTarget());
            }
        }
    }

    public struct BoidTarget : IComponentData
    {
    }
}
