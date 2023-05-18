using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Miscellaneous.ClosestTarget
{
    public class MovementAuthoring : MonoBehaviour
    {
        class Baker : Baker<MovementAuthoring>
        {
            public override void Bake(MovementAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<Movement>(entity);
            }
        }
    }

    public struct Movement : IComponentData
    {
        public float2 Value;
    }
}
