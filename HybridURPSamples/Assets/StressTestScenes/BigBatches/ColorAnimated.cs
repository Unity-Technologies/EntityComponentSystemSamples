using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

struct ColorAnimated : IComponentData
{
}

namespace Authoring
{
    [DisallowMultipleComponent]
    public class ColorAnimated : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponent(entity, ComponentType.ReadWrite<global::ColorAnimated>());
        }
    }
}