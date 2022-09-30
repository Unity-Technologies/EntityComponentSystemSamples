using Unity.Entities;
using UnityEngine;

// Tag used to quickly identify our Player Entity
struct PlayerTag : IComponentData {}

/// <summary>
/// "Authoring" component for the player. Part of the GameObject Conversion workflow.
/// Allows us to edit GameObjects in the Editor and convert those GameObjects to the optimized Entity representation
/// </summary>
[DisallowMultipleComponent]
public class PlayerAuthoring : MonoBehaviour
{
    public float MovementSpeedMetersPerSecond = 5.0f;

    class Baker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            var entity = GetEntity();
            // Here we add all of the components needed by the player
            AddComponent(entity, new ComponentTypeSet(
                typeof(PlayerTag),
                typeof(UserInputData),
                typeof(MovementSpeed)));

            // Set the movement speed value from the authoring component
            SetComponent(entity, new MovementSpeed {MetersPerSecond = authoring.MovementSpeedMetersPerSecond});
        }
    }
}
