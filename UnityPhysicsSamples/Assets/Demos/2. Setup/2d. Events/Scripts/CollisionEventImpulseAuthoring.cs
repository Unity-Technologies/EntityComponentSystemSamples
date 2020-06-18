using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

public struct CollisionEventImpulse : IComponentData
{
    public float3 Impulse;
}

public class CollisionEventImpulseAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public float Magnitude = 1.0f;
    public float3 Direction = math.up();

    void OnEnable() { }

    void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (enabled)
        {
            dstManager.AddComponentData(entity, new CollisionEventImpulse()
            {
                Impulse = Magnitude * Direction,
            });
        }
    }
}

// This system applies an impulse to any dynamic that collides with a Repulsor.
// A Repulsor is defined by a PhysicsShapeAuthoring with the `Raise Collision Events` flag ticked and a
// CollisionEventImpulse behaviour added.
[UpdateAfter(typeof(EndFramePhysicsSystem))]
public class CollisionEventImpulseSystem : SystemBase
{
    BuildPhysicsWorld m_BuildPhysicsWorldSystem;
    StepPhysicsWorld m_StepPhysicsWorldSystem;
    EntityQuery m_ImpulseGroup;

    protected override void OnCreate()
    {
        m_BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
        m_StepPhysicsWorldSystem = World.GetOrCreateSystem<StepPhysicsWorld>();
        m_ImpulseGroup = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                typeof(CollisionEventImpulse)
            }
        });
    }

    [BurstCompile]
    struct CollisionEventImpulseJob : ICollisionEventsJob
    {
        [ReadOnly] public ComponentDataFromEntity<CollisionEventImpulse> ColliderEventImpulseGroup;
        public ComponentDataFromEntity<PhysicsVelocity> PhysicsVelocityGroup;

        public void Execute(CollisionEvent collisionEvent)
        {
            Entity entityA = collisionEvent.EntityA;
            Entity entityB = collisionEvent.EntityB;

            bool isBodyADynamic = PhysicsVelocityGroup.HasComponent(entityA);
            bool isBodyBDynamic = PhysicsVelocityGroup.HasComponent(entityB);

            bool isBodyARepulser = ColliderEventImpulseGroup.HasComponent(entityA);
            bool isBodyBRepulser = ColliderEventImpulseGroup.HasComponent(entityB);

            if (isBodyARepulser && isBodyBDynamic)
            {
                var impulseComponent = ColliderEventImpulseGroup[entityA];
                var velocityComponent = PhysicsVelocityGroup[entityB];
                velocityComponent.Linear = impulseComponent.Impulse;
                PhysicsVelocityGroup[entityB] = velocityComponent;
            }
            if (isBodyBRepulser && isBodyADynamic)
            {
                var impulseComponent = ColliderEventImpulseGroup[entityB];
                var velocityComponent = PhysicsVelocityGroup[entityA];
                velocityComponent.Linear = impulseComponent.Impulse;
                PhysicsVelocityGroup[entityA] = velocityComponent;
            }
        }
    }

    protected override void OnUpdate()
    {
        if (m_ImpulseGroup.CalculateEntityCount() == 0)
        {
            return;
        }

        Dependency = new CollisionEventImpulseJob
        {
            ColliderEventImpulseGroup = GetComponentDataFromEntity<CollisionEventImpulse>(true),
            PhysicsVelocityGroup = GetComponentDataFromEntity<PhysicsVelocity>(),
        }.Schedule(m_StepPhysicsWorldSystem.Simulation,
            ref m_BuildPhysicsWorldSystem.PhysicsWorld, Dependency);
    }
}
