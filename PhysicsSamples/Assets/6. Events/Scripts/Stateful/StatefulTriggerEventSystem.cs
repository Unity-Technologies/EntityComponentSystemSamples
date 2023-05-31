using Unity.Entities;
using Unity.Jobs;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Burst;

namespace Unity.Physics.Stateful
{
    /// <summary>
    /// This system converts stream of TriggerEvents to StatefulTriggerEvents that can be stored in a Dynamic Buffer.
    /// In order for this conversion, it is required to:
    ///    1) Use the 'Raise Trigger Events' option of the 'Collision Response' property on a <see cref="PhysicsShapeAuthoring"/> component, and
    ///    2) Add a <see cref="StatefulTriggerEventAuthoring"/> component to that entity
    /// or, if this is desired on a Character Controller:
    ///    1) Tick the 'Raise Trigger Events' flag on the <see cref="CharacterControllerAuthoring"/> component.
    ///       Note: the Character Controller will not become a trigger, it will raise events when overlapping with one
    /// </summary>
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    public partial struct StatefulTriggerEventSystem : ISystem
    {
        StatefulSimulationEventBuffers<StatefulTriggerEvent> events;
        EntityQuery triggerEventQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            events = new StatefulSimulationEventBuffers<StatefulTriggerEvent>();
            events.AllocateBuffers();

            triggerEventQuery = SystemAPI.QueryBuilder()
                .WithAllRW<StatefulTriggerEvent>()
                .WithNone<StatefulTriggerEventExclude>().Build();
            state.RequireForUpdate(triggerEventQuery);

            state.RequireForUpdate<SimulationSingleton>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            events.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new ClearTriggerEventDynamicBufferJob()
                .ScheduleParallel(triggerEventQuery, state.Dependency);

            events.SwapBuffers();

            state.Dependency = new CollectTriggerEventsJob
            {
                TriggerEvents = events.Current
            }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);

            state.Dependency = new ConvertEventStreamToDynamicBufferJob<StatefulTriggerEvent, StatefulTriggerEventExclude>
            {
                CurrentEvents = events.Current,
                PreviousEvents = events.Previous,
                EventLookup = SystemAPI.GetBufferLookup<StatefulTriggerEvent>(),

                UseExcludeComponent = true,
                EventExcludeLookup = SystemAPI.GetComponentLookup<StatefulTriggerEventExclude>(true)
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        public partial struct ClearTriggerEventDynamicBufferJob : IJobEntity
        {
            public void Execute(ref DynamicBuffer<StatefulTriggerEvent> eventBuffer) => eventBuffer.Clear();
        }
    }
}
