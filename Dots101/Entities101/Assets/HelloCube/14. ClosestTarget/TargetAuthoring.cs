using Unity.Entities;
using UnityEngine;

namespace HelloCube.ClosestTarget
{
    public class TargetAuthoring : MonoBehaviour
    {
        class Baker : Baker<TargetAuthoring>
        {
            public override void Bake(TargetAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<Target>(entity);
            }
        }
    }

    public struct Target : IComponentData
    {
        public Entity Value;
    }
}
