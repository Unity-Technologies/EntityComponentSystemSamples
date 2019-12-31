#if UNITY_EDITOR

using System;
using Samples.Boids;
using Unity.Entities;
using UnityEngine;

[RequiresEntityConversion]
[AddComponentMenu("DOTS Samples/Boids/BoidTarget")]
[ConverterVersion("joe", 1)]
public class BoidTargetAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new BoidTarget());
    }
}

#endif
