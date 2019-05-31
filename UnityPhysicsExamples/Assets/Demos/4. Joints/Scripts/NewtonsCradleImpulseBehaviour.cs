using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Physics.Authoring;

public struct NewtonsCradleImpulse : IComponentData
{
    public Entity OtherEntity;
}

public class NewtonsCradleImpulseBehaviour : MonoBehaviour, IConvertGameObjectToEntity
{
    public PhysicsBody OtherBody = null;

    void OnEnable() { }

    void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (enabled)
        {
            var otherEntity = conversionSystem.GetPrimaryEntity(OtherBody);
            if (otherEntity != Entity.Null)
            {
                dstManager.AddComponentData(entity, new NewtonsCradleImpulse()
                {
                    OtherEntity = conversionSystem.GetPrimaryEntity(OtherBody),
                });
            }
        }
    }
}


[UpdateAfter(typeof(EndFramePhysicsSystem))]
unsafe public class NewtonsCradleImpulseSystem : JobComponentSystem
{
    BuildPhysicsWorld m_BuildPhysicsWorldSystem;
    StepPhysicsWorld m_StepPhysicsWorldSystem;

    EntityQuery ImpulseGroup;

    protected override void OnCreate()
    {
        m_BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
        m_StepPhysicsWorldSystem = World.GetOrCreateSystem<StepPhysicsWorld>();
        ImpulseGroup = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(NewtonsCradleImpulse), }
        });
    }

    [BurstCompile]
    struct NewtonsCradleImpulseJob : ICollisionEventsJob
    {
        [ReadOnly] public ComponentDataFromEntity<NewtonsCradleImpulse> ImpulseGroup;
        public ComponentDataFromEntity<PhysicsVelocity> PhysicsVelocityGroup;

        public void Execute(CollisionEvent collisionEvent)
        {
            Entity entityA = collisionEvent.Entities.EntityA;
            Entity entityB = collisionEvent.Entities.EntityB;

            bool isEntityAInImpulseGroup = ImpulseGroup.Exists(entityA);
            bool isEntityBInImpulseGroup = ImpulseGroup.Exists(entityB);

            if (!(isEntityAInImpulseGroup || isEntityBInImpulseGroup))
                return;

            var hitSphere = isEntityAInImpulseGroup ? entityB : entityA;
            var endSphere = isEntityAInImpulseGroup ? entityA : entityB;

            var impulseInfo = ImpulseGroup[endSphere];
            var otherEndSphere = impulseInfo.OtherEntity;

            var impulse = math.csum(collisionEvent.AccumulatedImpulses);

            // kill motion on endSphere
            {
                var velocityComponent = PhysicsVelocityGroup[endSphere];
                velocityComponent.Linear -= impulse * collisionEvent.Normal;
                PhysicsVelocityGroup[endSphere] = velocityComponent;
            }
            // add motion to otherEndSphere
            {
                var velocityComponent = PhysicsVelocityGroup[otherEndSphere];
                velocityComponent.Linear += impulse * -collisionEvent.Normal;
                PhysicsVelocityGroup[otherEndSphere] = velocityComponent;
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        JobHandle jobHandle = new NewtonsCradleImpulseJob
        {
            ImpulseGroup = GetComponentDataFromEntity<NewtonsCradleImpulse>(true),
            PhysicsVelocityGroup = GetComponentDataFromEntity<PhysicsVelocity>(),
        }.Schedule(m_StepPhysicsWorldSystem.Simulation, 
                    ref m_BuildPhysicsWorldSystem.PhysicsWorld, inputDeps);

        return jobHandle;
    }
}
