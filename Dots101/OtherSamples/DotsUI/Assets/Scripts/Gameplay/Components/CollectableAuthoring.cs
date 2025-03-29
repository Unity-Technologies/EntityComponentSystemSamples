using Unity.Entities;
using UnityEngine;

namespace Unity.DotsUISample
{
    public class CollectableAuthoring : MonoBehaviour
    {
        public CollectableType collectableType;

        public class Baker : Baker<CollectableAuthoring>
        {
            public override void Bake(CollectableAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                AddComponent(entity, new Collectable
                {
                    Type = authoring.collectableType
                });
            }
        }
    }
    
    public struct Collectable : IComponentData
    {
        public CollectableType Type;
    }
    
    public enum CollectableType
    {
        StarMushroom = 0,
        FireFlower = 1,
        MoonlitRoot = 2,
    }
}