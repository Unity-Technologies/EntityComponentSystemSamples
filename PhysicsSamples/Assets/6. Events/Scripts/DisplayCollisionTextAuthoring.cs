using Common.Scripts;
using Unity.Entities;
using UnityEngine;

namespace Events
{
    public class DisplayCollisionTextAuthoring : MonoBehaviour, IReceiveEntity
    {
        class Baker : Baker<DisplayCollisionTextAuthoring>
        {
            public override void Bake(DisplayCollisionTextAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new DisplayCollisionTextComponent()
                {
                    CollisionDurationCount = 0,
                    FramesSinceCollisionExit = 0
                });
            }
        }
    }

    public struct DisplayCollisionTextComponent : IComponentData
    {
        public int CollisionDurationCount;
        public int FramesSinceCollisionExit;
    }
}
