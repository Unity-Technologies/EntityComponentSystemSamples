using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace KickBall
{
    public class PlayerInputAuthoring : MonoBehaviour
    {
        class Baker : Baker<PlayerInputAuthoring>
        {
            public override void Bake(PlayerInputAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.None);
                AddComponent<PlayerInput>(entity);
            }
        }
    }
    
    // We only need set this input component once per frame.
    // Netcode will append the value to the client's input buffer.
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct PlayerInput : IInputComponentData
    {
        public float Horizontal;
        public float Vertical;
        public InputEvent KickBall;
        public InputEvent SpawnBall;   
    }
}
