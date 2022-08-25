using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

public struct TriggerGravityFactor : IComponentData
{
    public float GravityFactor;
    public float DampingFactor;
}

public class TriggerGravityFactorAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public float GravityFactor = 0f;
    public float DampingFactor = 0.9f;

    void OnEnable() {}

    void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (enabled)
        {
            dstManager.AddComponentData(entity, new TriggerGravityFactor()
            {
                GravityFactor = GravityFactor,
                DampingFactor = DampingFactor,
            });
        }
    }
}


// This system sets the PhysicsGravityFactor of any dynamic body that enters a Trigger Volume.
// A Trigger Volume is defined by a PhysicsShapeAuthoring with the `Is Trigger` flag ticked and a
// TriggerGravityFactor behaviour added.
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(ExportPhysicsWorld))]
[UpdateBefore(typeof(EndFramePhysicsSystem))]
public partial class TriggerGravityFactorSystem : SystemBase
{
    StepPhysicsWorld m_StepPhysicsWorldSystem;
    EntityQuery m_TriggerGravityGroup;

    protected override void OnCreate()
    {
        m_StepPhysicsWorldSystem = World.GetOrCreateSystem<StepPhysicsWorld>();
        m_TriggerGravityGroup = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                typeof(TriggerGravityFactor)
            }
        });
    }

    [BurstCompile]
    struct TriggerGravityFactorJob : ITriggerEventsJob
    {
        [ReadOnly] public ComponentDataFromEntity<TriggerGravityFactor> TriggerGravityFactorGroup;
        public ComponentDataFromEntity<PhysicsGravityFactor> PhysicsGravityFactorGroup;
        public ComponentDataFromEntity<PhysicsVelocity> PhysicsVelocityGroup;

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

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        this.RegisterPhysicsRuntimeSystemReadOnly();
    }

    protected override void OnUpdate()
    {
        if (m_TriggerGravityGroup.CalculateEntityCount() == 0)
        {
            return;
        }

        Dependency = new TriggerGravityFactorJob
        {
            TriggerGravityFactorGroup = GetComponentDataFromEntity<TriggerGravityFactor>(true),
            PhysicsGravityFactorGroup = GetComponentDataFromEntity<PhysicsGravityFactor>(),
            PhysicsVelocityGroup = GetComponentDataFromEntity<PhysicsVelocity>(),
        }.Schedule(m_StepPhysicsWorldSystem.Simulation, Dependency);
    }
}
