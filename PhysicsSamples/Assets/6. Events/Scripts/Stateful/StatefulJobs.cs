using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Unity.Physics.Stateful
{
    public static class StatefulEventCollectionJobs
    {
        [BurstCompile]
        public struct CollectTriggerEvents : ITriggerEventsJob
        {
            public NativeList<StatefulTriggerEvent> TriggerEvents;
            public void Execute(TriggerEvent triggerEvent) => TriggerEvents.Add(new StatefulTriggerEvent(triggerEvent));
        }

        [BurstCompile]
        public struct CollectCollisionEvents : ICollisionEventsJob
        {
            public NativeList<StatefulCollisionEvent> CollisionEvents;
            public void Execute(CollisionEvent collisionEvent) => CollisionEvents.Add(new StatefulCollisionEvent(collisionEvent));
        }

        [BurstCompile]
        public struct CollectCollisionEventsWithDetails : ICollisionEventsJob
        {
            public NativeList<StatefulCollisionEvent> CollisionEvents;
            [ReadOnly] public PhysicsWorld PhysicsWorld;
            [ReadOnly] public ComponentLookup<StatefulCollisionEventDetails> EventDetails;
            public bool ForceCalculateDetails;

            public void Execute(CollisionEvent collisionEvent)
            {
                var statefulCollisionEvent = new StatefulCollisionEvent(collisionEvent);

                // Check if we should calculate the collision details
                bool calculateDetails = ForceCalculateDetails;
                if (!calculateDetails && EventDetails.HasComponent(collisionEvent.EntityA))
                {
                    calculateDetails = EventDetails[collisionEvent.EntityA].CalculateDetails;
                }
                if (!calculateDetails && EventDetails.HasComponent(collisionEvent.EntityB))
                {
                    calculateDetails = EventDetails[collisionEvent.EntityB].CalculateDetails;
                }
                if (calculateDetails)
                {
                    var details = collisionEvent.CalculateDetails(ref PhysicsWorld);
                    statefulCollisionEvent.CollisionDetails = new StatefulCollisionEvent.Details(
                        details.EstimatedContactPointPositions.Length,
                        details.EstimatedImpulse,
                        details.AverageContactPointPosition);
                }

                CollisionEvents.Add(statefulCollisionEvent);
            }
        }

        [BurstCompile]
        public struct ConvertEventStreamToDynamicBufferJob<T, C> : IJob
            where T : unmanaged, IBufferElementData, IStatefulSimulationEvent<T>
            where C : unmanaged, IComponentData
        {
            public NativeList<T> PreviousEvents;
            public NativeList<T> CurrentEvents;
            public BufferLookup<T> EventLookup;

            public bool UseExcludeComponent;
            [ReadOnly] public ComponentLookup<C> EventExcludeLookup;

            public void Execute()
            {
                var statefulEvents = new NativeList<T>(CurrentEvents.Length, Allocator.Temp);

                StatefulSimulationEventBuffers<T>.GetStatefulEvents(PreviousEvents, CurrentEvents, statefulEvents);

                for (int i = 0; i < statefulEvents.Length; i++)
                {
                    var statefulEvent = statefulEvents[i];

                    var addToEntityA = EventLookup.HasBuffer(statefulEvent.EntityA) &&
                        (!UseExcludeComponent || !EventExcludeLookup.HasComponent(statefulEvent.EntityA));
                    var addToEntityB = EventLookup.HasBuffer(statefulEvent.EntityB) &&
                        (!UseExcludeComponent || !EventExcludeLookup.HasComponent(statefulEvent.EntityA));

                    if (addToEntityA)
                    {
                        EventLookup[statefulEvent.EntityA].Add(statefulEvent);
                    }

                    if (addToEntityB)
                    {
                        EventLookup[statefulEvent.EntityB].Add(statefulEvent);
                    }
                }
            }
        }
    }
}