using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

// This system sets the PhysicsGravityFactor of any dynamic body that enters a Trigger Volume.
// A Trigger Volume is defined by a PhysicsShapeAuthoring with the `Is Trigger` flag ticked and a
// TriggerGravityFactor behaviour added.
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
public partial struct TriggerGravitySystem : ISystem
{
    ComponentDataHandles m_Handles;

    struct ComponentDataHandles
    {
        public ComponentLookup<TriggerGravityFactor> TriggerGravityFactorGroup;
        public ComponentLookup<PhysicsGravityFactor> PhysicsGravityFactorGroup;
        public ComponentLookup<PhysicsVelocity> PhysicsVelocityGroup;

        public ComponentDataHandles(ref SystemState state)
        {
            TriggerGravityFactorGroup = state.GetComponentLookup<TriggerGravityFactor>(true);
            PhysicsGravityFactorGroup = state.GetComponentLookup<PhysicsGravityFactor>(false);
            PhysicsVelocityGroup = state.GetComponentLookup<PhysicsVelocity>(false);
        }

        public void Update(ref SystemState state)
        {
            TriggerGravityFactorGroup.Update(ref state);
            PhysicsGravityFactorGroup.Update(ref state);
            PhysicsVelocityGroup.Update(ref state);
        }
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(state.GetEntityQuery(ComponentType.ReadOnly<TriggerGravityFactor>()));
        m_Handles = new ComponentDataHandles(ref state);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        m_Handles.Update(ref state);
        state.Dependency = new TriggerGravityFactorJob
        {
            TriggerGravityFactorGroup = m_Handles.TriggerGravityFactorGroup,
            PhysicsGravityFactorGroup = m_Handles.PhysicsGravityFactorGroup,
            PhysicsVelocityGroup = m_Handles.PhysicsVelocityGroup,
        }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
    }

    [BurstCompile]
    struct TriggerGravityFactorJob : ITriggerEventsJob
    {
        [ReadOnly] public ComponentLookup<TriggerGravityFactor> TriggerGravityFactorGroup;
        public ComponentLookup<PhysicsGravityFactor> PhysicsGravityFactorGroup;
        public ComponentLookup<PhysicsVelocity> PhysicsVelocityGroup;

        public void Execute(TriggerEvent triggerEvent)
        {
            Entity entityA = triggerEvent.EntityA;
            Entity entityB = triggerEvent.EntityB;

            bool isBodyATrigger = TriggerGravityFactorGroup.HasComponent(entityA);
            bool isBodyBTrigger = TriggerGravityFactorGroup.HasComponent(entityB);

            // Ignoring Triggers overlapping other Triggers
            if (isBodyATrigger && isBodyBTrigger)
                return;

            bool isBodyADynamic = PhysicsVelocityGroup.HasComponent(entityA);
            bool isBodyBDynamic = PhysicsVelocityGroup.HasComponent(entityB);

            // Ignoring overlapping static bodies
            if ((isBodyATrigger && !isBodyBDynamic) ||
                (isBodyBTrigger && !isBodyADynamic))
                return;

            var triggerEntity = isBodyATrigger ? entityA : entityB;
            var dynamicEntity = isBodyATrigger ? entityB : entityA;

            var triggerGravityComponent = TriggerGravityFactorGroup[triggerEntity];
            // tweak PhysicsGravityFactor
            {
                var component = PhysicsGravityFactorGroup[dynamicEntity];
                component.Value = triggerGravityComponent.GravityFactor;
                PhysicsGravityFactorGroup[dynamicEntity] = component;
            }
            // damp velocity
            {
                var component = PhysicsVelocityGroup[dynamicEntity];
                component.Linear *= triggerGravityComponent.DampingFactor;
                PhysicsVelocityGroup[dynamicEntity] = component;
            }
        }
    }
}
