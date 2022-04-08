using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;
using Unity.Collections;
using Unity.Burst;
using System;
using Unity.Assertions;
using Unity.Mathematics;

namespace Unity.Physics.Stateful
{
    // If this component is added to an entity, trigger events won't be added to a dynamic buffer
    // of that entity by the StatefulTriggerEventBufferSystem. This component is by default added to
    // CharacterController entity, so that CharacterControllerSystem can add trigger events to
    // CharacterController on its own, without StatefulTriggerEventBufferSystem interference.
    public struct StatefulTriggerEventExclude : IComponentData {}

    public class StatefulTriggerEventBufferAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddBuffer<StatefulTriggerEvent>(entity);
        }
    }
}
