using Unity.Entities;
using UnityEngine;

namespace Unity.DotsUISample
{
    public class PlayerAuthoring : MonoBehaviour
    {
        public float MovementSpeed = 5.0f;
        public CollectablesData collectables;

        public class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
                AddComponent(entity, new Player
                {
                    MovementSpeed = authoring.MovementSpeed
                });
                AddBuffer<InventoryItem>(entity);
                var buf = AddBuffer<CollectableCount>(entity);
                buf.Length = authoring.collectables.Collectables.Length;
                for (int i = 0; i < buf.Length; i++)
                {
                    buf[i] = new CollectableCount { Count = 0 };
                }
            }
        }
    }

    public struct Player : IComponentData
    {
        public float MovementSpeed;
        public int EnergyCount;
    }

    // buffer length should equal count of item types
    // The count of items for each type
    // collectableCountBuf[CollectableType.FireFlower] is count for the fire flowers, etc. 
    public struct CollectableCount : IBufferElementData
    {
        public int Count;
    }

    // the player's items in the order they should appear in the inventory window
    public struct InventoryItem : IBufferElementData
    {
        public CollectableType Type;
    }
}