using Unity.Entities;
using UnityEngine;

namespace Unity.Physics.Stateful
{
    // If this component is added to an entity, trigger events won't be added to a dynamic buffer
    // of that entity by the StatefulTriggerEventBufferSystem. This component is by default added to
    // CharacterController entity, so that CharacterControllerSystem can add trigger events to
    // CharacterController on its own, without StatefulTriggerEventBufferSystem interference.
    public struct StatefulTriggerEventExclude : IComponentData {}

    public class StatefulTriggerEventAuthoring : MonoBehaviour
    {
        class Baker : Baker<StatefulTriggerEventAuthoring>
        {
            public override void Bake(StatefulTriggerEventAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddBuffer<StatefulTriggerEvent>(entity);
            }
        }
    }
}
