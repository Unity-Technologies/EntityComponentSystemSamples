using Unity.Entities;
using UnityEngine;

namespace BreakingBricks
{
    public class BrickAuthoring : MonoBehaviour
    {
        public class Baker : Baker<BrickAuthoring>
        {
            public override void Bake(BrickAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                AddComponent(entity, new Brick
                {
                    Hitpoints = 1.0f
                });
            }
        }
    }

    public struct Brick : IComponentData
    {
        public float Hitpoints;
    }
}