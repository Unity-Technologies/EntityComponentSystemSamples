using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Unity.Physics.Stateful
{
    public struct StatefulSimulationEventBuffers<T> where T : unmanaged, IStatefulSimulationEvent<T>
    {
        public NativeList<T> Previous;
        public NativeList<T> Current;

        public void AllocateBuffers()
        {
            Previous = new NativeList<T>(Allocator.Persistent);
            Current = new NativeList<T>(Allocator.Persistent);
        }

        public void Dispose()
        {
            if (Previous.IsCreated) Previous.Dispose();
            if (Current.IsCreated) Current.Dispose();
        }

        public void SwapBuffers()
        {
            var tmp = Previous;
            Previous = Current;
            Current = tmp;
            Current.Clear();
        }

        /// <summary>
        /// Returns a sorted combined list of stateful events based on the Previous and Current event frames.
        /// Note: ensure that the frame buffers are sorted by calling SortBuffers first.
        /// </summary>
        /// <param name="statefulEvents"></param>
        /// <param name="sortCurrent">Specifies whether the Current events list needs to be sorted first.</param>
        public void GetStatefulEvents(NativeList<T> statefulEvents, bool sortCurrent = true) => GetStatefulEvents(Previous, Current, statefulEvents, sortCurrent);

        /// <summary>
        /// Given two sorted event buffers, this function returns a single combined list with
        /// all the appropriate <see cref="StatefulEventState"/> set on each event.
        /// </summary>
        /// <param name="previousEvents">The events buffer from the previous frame. This list should have already be sorted from the previous frame.</param>
        /// <param name="currentEvents">The events buffer from the current frame. This list should be sorted before calling this function.</param>
        /// <param name="statefulEvents">A single combined list of stateful events based on the previous and current frames.</param>
        /// <param name="sortCurrent">Specifies whether the currentEvents list needs to be sorted first.</param>
        public static void GetStatefulEvents(NativeList<T> previousEvents, NativeList<T> currentEvents, NativeList<T> statefulEvents, bool sortCurrent = true)
        {
            if (sortCurrent) currentEvents.Sort();

            statefulEvents.Clear();

            int c = 0;
            int p = 0;
            while (c < currentEvents.Length && p < previousEvents.Length)
            {
                int r = previousEvents[p].CompareTo(currentEvents[c]);
                if (r == 0)
                {
                    var currentEvent = currentEvents[c];
                    currentEvent.State = StatefulEventState.Stay;
                    statefulEvents.Add(currentEvent);
                    c++;
                    p++;
                }
                else if (r < 0)
                {
                    var previousEvent = previousEvents[p];
                    previousEvent.State = StatefulEventState.Exit;
                    statefulEvents.Add(previousEvent);
                    p++;
                }
                else //(r > 0)
                {
                    var currentEvent = currentEvents[c];
                    currentEvent.State = StatefulEventState.Enter;
                    statefulEvents.Add(currentEvent);
                    c++;
                }
            }
            if (c == currentEvents.Length)
            {
                while (p < previousEvents.Length)
                {
                    var previousEvent = previousEvents[p];
                    previousEvent.State = StatefulEventState.Exit;
                    statefulEvents.Add(previousEvent);
                    p++;
                }
            }
            else if (p == previousEvents.Length)
            {
                while (c < currentEvents.Length)
                {
                    var currentEvent = currentEvents[c];
                    currentEvent.State = StatefulEventState.Enter;
                    statefulEvents.Add(currentEvent);
                    c++;
                }
            }
        }
    }

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
            public BufferLookup<T> EventBuffers;

            public bool UseExcludeComponent;
            [ReadOnly] public ComponentLookup<C> EventExcludeLookup;

            public void Execute()
            {
                var statefulEvents = new NativeList<T>(CurrentEvents.Length, Allocator.Temp);

                StatefulSimulationEventBuffers<T>.GetStatefulEvents(PreviousEvents, CurrentEvents, statefulEvents);

                for (int i = 0; i < statefulEvents.Length; i++)
                {
                    var statefulEvent = statefulEvents[i];

                    var addToEntityA = EventBuffers.HasBuffer(statefulEvent.EntityA) && (!UseExcludeComponent || !EventExcludeLookup.HasComponent(statefulEvent.EntityA));
                    var addToEntityB = EventBuffers.HasBuffer(statefulEvent.EntityB) && (!UseExcludeComponent || !EventExcludeLookup.HasComponent(statefulEvent.EntityA));

                    if (addToEntityA)
                    {
                        EventBuffers[statefulEvent.EntityA].Add(statefulEvent);
                    }
                    if (addToEntityB)
                    {
                        EventBuffers[statefulEvent.EntityB].Add(statefulEvent);
                    }
                }
            }
        }
    }
}
