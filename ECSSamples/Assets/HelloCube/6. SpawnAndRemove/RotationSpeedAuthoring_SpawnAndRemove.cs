﻿using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// ReSharper disable once InconsistentNaming
[RequiresEntityConversion]
[AddComponentMenu("DOTS Samples/SpawnAndRemove/Rotation Speed")]
[ConverterVersion("joe", 1)]
public class RotationSpeedAuthoring_SpawnAndRemove : MonoBehaviour, IConvertGameObjectToEntity
{
    public float DegreesPerSecond = 360;

    // The MonoBehaviour data is converted to ComponentData on the entity.
    // We are specifically transforming from a good editor representation of the data (Represented in degrees)
    // To a good runtime representation (Represented in radians)
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new RotationSpeed_SpawnAndRemove { RadiansPerSecond = math.radians(DegreesPerSecond) });
        dstManager.AddComponentData(entity, new LifeTime { Value = 0.0F });
    }
}
