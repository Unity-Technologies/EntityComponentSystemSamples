using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics.Systems;

namespace Unity.Physics.Stateful
{
    // This system converts stream of CollisionEvents to StatefulCollisionEvents that can be stored in a Dynamic Buffer.
    // In order for this conversion, it is required to:
    //    1) Use the 'Collide Raise Collision Events' option of the 'Collision Response' property on a PhysicsShapeAuthoring component, and
    //    2) Add a StatefulCollisionEventBufferAuthoring component to that entity (and select if details should be calculated or not)
    // or, if this is desired on a Character Controller:
    //    1) Tick the 'Raise Collision Events' flag on the CharacterControllerAuthoring component.
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    public partial struct StatefulCollisionEventBufferSystem : ISystem
    {
        private StatefulSimulationEventBuffers<StatefulCollisionEvent> m_StateFulEventBuffers;
        private ComponentHandles m_Handles;

        // Component that does nothing. Made in order to use a generic job. See OnUpdate() method for details.
        internal struct DummyExcludeComponent : IComponentData {};

        struct ComponentHandles
        {
            public ComponentLookup<DummyExcludeComponent> EventExcludes;
            public ComponentLookup<StatefulCollisionEventDetails> EventDetails;
            public BufferLookup<StatefulCollisionEvent> EventBuffers;

            public ComponentHandles(ref SystemState systemState)
            {
                EventExcludes = systemState.GetComponentLookup<DummyExcludeComponent>(true);
                EventDetails = systemState.GetComponentLookup<StatefulCollisionEventDetails>(true);
                EventBuffers = systemState.GetBufferLookup<StatefulCollisionEvent>(false);
            }

            public void Update(ref SystemState systemState)
            {
                EventExcludes.Update(ref systemState);
                EventBuffers.Update(ref systemState);
                EventDetails.Update(ref systemState);
            }
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_StateFulEventBuffers = new StatefulSimulationEventBuffers<StatefulCollisionEvent>();
            m_StateFulEventBuffers.AllocateBuffers();
            state.RequireForUpdate<StatefulCollisionEvent>();

            m_Handles = new ComponentHandles(ref state);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            m_StateFulEventBuffers.Dispose();
        }

        [BurstCompile]
        public partial struct ClearCollisionEventDynamicBufferJob : IJobEntity
        {
            public void Execute(ref DynamicBuffer<StatefulCollisionEvent> eventBuffer) => eventBuffer.Clear();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            m_Handles.Update(ref state);

            state.Dependency = new ClearCollisionEventDynamicBufferJob()
                .ScheduleParallel(state.Dependency);

            m_StateFulEventBuffers.SwapBuffers();

            var currentEvents = m_StateFulEventBuffers.Current;
            var previousEvents = m_StateFulEventBuffers.Previous;

            state.Dependency = new StatefulEventCollectionJobs.
                CollectCollisionEventsWithDetails
            {
                CollisionEvents = currentEvents,
                PhysicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld,
                EventDetails = m_Handles.EventDetails
            }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);


            state.Dependency = new StatefulEventCollectionJobs.
                ConvertEventStreamToDynamicBufferJob<StatefulCollisionEvent, DummyExcludeComponent>
            {
                CurrentEvents = currentEvents,
                PreviousEvents = previousEvents,
                EventLookup = m_Handles.EventBuffers,

                UseExcludeComponent = false,
                EventExcludeLookup = m_Handles.EventExcludes
            }.Schedule(state.Dependency);
        }
    }
}
