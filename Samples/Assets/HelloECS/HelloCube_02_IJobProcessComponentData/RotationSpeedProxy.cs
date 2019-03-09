using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Samples.HelloCube_02
{
    public class RotationSpeedProxy : MonoBehaviour, IConvertGameObjectToEntity
    {
        public float DegreesPerSecond = 360;
        
        // The MonoBehaviour data is converted to ComponentData on the entity.
        // We are specifically transforming from a good editor representation of the data (Represented in degrees)
        // To a good runtime representation (Represented in radians)
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            var data = new RotationSpeed { RadiansPerSecond = math.radians(DegreesPerSecond) };
            dstManager.AddComponentData(entity, data);
        }
    }
}
