using Unity.Entities;
using UnityEngine;


public class DisplayCollisionTextAuthoring : MonoBehaviour, IReceiveEntity
{
    class Baker : Baker<DisplayCollisionTextAuthoring>
    {
        public override void Bake(DisplayCollisionTextAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new DisplayCollisionText()
            {
                CollisionDurationCount = 0,
                FramesSinceCollisionExit = 0
            });
        }
    }
}

public struct DisplayCollisionText : IComponentData
{
    public int CollisionDurationCount;
    public int FramesSinceCollisionExit;
}
