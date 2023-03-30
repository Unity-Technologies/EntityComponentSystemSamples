using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Samples.HelloNetcode
{
    [GhostComponent(PrefabType=GhostPrefabType.AllPredicted)]
    public struct PlayerMovement : IComponentData
    {
        // This value is connected to the jump logic calculated inside the prediction loop
        // and thus needs to be a ghost field so it's properly stored in ghost history and
        // you get the correct value each time the prediction runs regardless of what tick is
        // being predicted
        [GhostField]
        public int JumpVelocity;
    }

    [DisallowMultipleComponent]
    public class PlayerMovementAuthoring : MonoBehaviour
    {
        [RegisterBinding(typeof(PlayerMovement), "JumpVelocity")]
        public int JumpVelocity;

        class Baker : Baker<PlayerMovementAuthoring>
        {
            public override void Bake(PlayerMovementAuthoring authoring)
            {
                PlayerMovement component = default(PlayerMovement);
                component.JumpVelocity = authoring.JumpVelocity;
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
