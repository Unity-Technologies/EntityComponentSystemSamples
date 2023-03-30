using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public struct PlayerInput : IInputComponentData
    {
        public int Horizontal;
        public int Vertical;
        public InputEvent Jump;
    }

    [DisallowMultipleComponent]
    public class PlayerInputAuthoring : MonoBehaviour
    {
        class Baker : Baker<PlayerInputAuthoring>
        {
            public override void Bake(PlayerInputAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<PlayerInput>(entity);
            }
        }
    }
}
