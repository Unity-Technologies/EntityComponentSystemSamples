// A system that processes trigger events and updates the TileTriggerCounter component on the tile entity
// Structural changes are not allowed in this system, so triggers are processed in the SpawnColliderFromTriggerSystem
// Triggers larger than the MaxTriggerCount are ignored.
using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(PhysicsSystemGroup))]
[UpdateAfter(typeof(PhysicsSimulationGroup))] //events are valid AFTER this has finished
public partial struct TileTriggerSystem : ISystem
{
    ComponentDataHandles m_Handles;
    private Entity m_TriggerEntity;

    struct ComponentDataHandles
    {
        public ComponentLookup<TileTriggerCounter> TileTriggerCounterGroup;

        public ComponentDataHandles(ref SystemState state)
        {
            TileTriggerCounterGroup = state.GetComponentLookup<TileTriggerCounter>(false);
        }

        public void Update(ref SystemState state)
        {
            TileTriggerCounterGroup.Update(ref state);
        }
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(state.GetEntityQuery(ComponentType.ReadWrite<SimulationSingleton>()));
        state.RequireForUpdate(state.GetEntityQuery(ComponentType.ReadWrite<TileTriggerCounter>()));
        m_Handles = new ComponentDataHandles(ref state);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        m_Handles.Update(ref state);

        state.Dependency = new TriggerColliderChangeJob()
        {
            TriggerColliderChangeGroup = m_Handles.TileTriggerCounterGroup,
        }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    // This trigger is later used to spawn colliders in SpawnColliderFromTriggerSystem (structural changes can't be done here)
    [BurstCompile]
    struct TriggerColliderChangeJob : ITriggerEventsJob
    {
        public ComponentLookup<TileTriggerCounter> TriggerColliderChangeGroup;

        public void Execute(TriggerEvent triggerEvent)
        {
            // get the two entities involved in the trigger event
            Entity entityA = triggerEvent.EntityA;
            Entity entityB = triggerEvent.EntityB;

            // The entity that has a TileTriggerCounter is the trigger body
            bool isBodyATrigger = TriggerColliderChangeGroup.HasComponent(entityA);
            bool isBodyBTrigger = TriggerColliderChangeGroup.HasComponent(entityB);

            // Ignoring Triggers overlapping other Triggers
            if (isBodyATrigger && isBodyBTrigger)
                return;

            var triggerEntity = isBodyATrigger ? entityA : entityB; //entity of tile

            var tileComponent = TriggerColliderChangeGroup[triggerEntity];
            if (tileComponent.TriggerCount < tileComponent.MaxTriggerCount) // limit how many times the trigger can be triggered
            {
                tileComponent.TriggerCount++;
                TriggerColliderChangeGroup[triggerEntity] = tileComponent;
            }
        }
    }
}
