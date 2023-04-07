﻿using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// Tag used to quickly identify our Player Entity
struct PlayerTag : IComponentData { }

/// <summary>
/// "Authoring" component for the player. Part of the GameObject Conversion workflow.
/// Allows us to edit GameObjects in the Editor and convert those GameObjects to the optimized Entity representation
/// </summary>
[DisallowMultipleComponent]
public class PlayerAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public float MovementSpeedMetersPerSecond = 5.0f;
    public float RotationSpeedDegreesPerSecond = 180.0f;

    /// <summary>
    /// A function which converts our Player authoring GameObject to a more optimized Entity representation
    /// </summary>
    /// <param name="entity">A reference to the entity this GameObject will become</param>
    /// <param name="dstManager">The EntityManager is used to make changes to Entity data.</param>
    /// <param name="conversionSystem">Used for more advanced conversion features. Not used here</param>
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        // Here we add all of the components needed by the player
        dstManager.AddComponents(entity, new ComponentTypes(
            typeof(PlayerTag),
            typeof(UserInputData),
            typeof(MovementSpeed),
            typeof(RotationSpeed)));

        // Set the movement speed value from the authoring component
        dstManager.SetComponentData(entity, new MovementSpeed { MetersPerSecond = MovementSpeedMetersPerSecond });
        dstManager.SetComponentData(entity, new RotationSpeed { RadiansPerSecond = math.radians(RotationSpeedDegreesPerSecond) });
    }
}
