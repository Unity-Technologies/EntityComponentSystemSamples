using Unity.Entities;
using UnityEngine;

namespace Tutorials.Kickball.Step2
{
    // Same pattern as ObstacleAuthoring.cs in Step 1.
    public class PlayerAuthoring : MonoBehaviour
    {
        class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<Player>(entity);

                // Used in Step 5
                AddComponent<Carry>(entity);
                SetComponentEnabled<Carry>(entity, false);
            }
        }
    }

    public struct Player : IComponentData
    {
    }

    // Used in Step 5
    public struct Carry : IComponentData, IEnableableComponent
    {
        // on a ball, this denotes the player carrying the ball; on a player, this denotes the ball being carried
        public Entity Target;
    }
}
