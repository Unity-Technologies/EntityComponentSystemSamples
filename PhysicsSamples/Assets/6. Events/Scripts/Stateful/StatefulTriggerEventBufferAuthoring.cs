using Unity.Assertions;
using Unity.Entities;
using UnityEngine;

namespace Unity.Physics.Stateful
{
    public class StatefulTriggerEventBufferAuthoring : MonoBehaviour
    {
        class Baker : Baker<StatefulTriggerEventBufferAuthoring>
        {
            public override void Bake(StatefulTriggerEventBufferAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddBuffer<StatefulTriggerEvent>(entity);
            }
        }
    }

    // Trigger Event that can be stored inside a DynamicBuffer
    public struct StatefulTriggerEvent : IBufferElementData, IStatefulSimulationEvent<StatefulTriggerEvent>
    {
        public Entity EntityA { get; set; }
        public Entity EntityB { get; set; }
        public int BodyIndexA { get; set; }
        public int BodyIndexB { get; set; }
        public ColliderKey ColliderKeyA { get; set; }
        public ColliderKey ColliderKeyB { get; set; }
        public StatefulEventState State { get; set; }

        public StatefulTriggerEvent(TriggerEvent triggerEvent)
        {
            EntityA = triggerEvent.EntityA;
            EntityB = triggerEvent.EntityB;
            BodyIndexA = triggerEvent.BodyIndexA;
            BodyIndexB = triggerEvent.BodyIndexB;
            ColliderKeyA = triggerEvent.ColliderKeyA;
            ColliderKeyB = triggerEvent.ColliderKeyB;
            State = default;
        }

        // Returns other entity in EntityPair, if provided with one
        public Entity GetOtherEntity(Entity entity)
        {
            Assert.IsTrue((entity == EntityA) || (entity == EntityB));
            return (entity == EntityA) ? EntityB : EntityA;
        }

        public int CompareTo(StatefulTriggerEvent other) => ISimulationEventUtilities.CompareEvents(this, other);
    }

    // If this component is added to an entity, trigger events won't be added to a dynamic buffer
    // of that entity by the StatefulTriggerEventBufferSystem. This component is by default added to
    // CharacterController entity, so that CharacterControllerSystem can add trigger events to
    // CharacterController on its own, without StatefulTriggerEventBufferSystem interference.
    public struct StatefulTriggerEventExclude : IComponentData {}
}
