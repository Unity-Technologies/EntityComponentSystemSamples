using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Collider = Unity.Physics.Collider;
using Unity.Transforms;
using Unity.Profiling;
using Unity.Burst;
using System;
using UnityEditor;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

public struct CollisionEventImpulse : IComponentData
{
    public float3 Impulse;
}

public class CollisionEventImpulseBehaviour : MonoBehaviour, IConvertGameObjectToEntity
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
// A Repulsor is defined by a PhysicsShape with the `Raise Collision Events` flag ticked and a
// CollisionEventImpulse behaviour added.
[UpdateAfter(typeof(EndFramePhysicsSystem))]
unsafe public class CollisionEventImpulseSystem : JobComponentSystem
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
            All = new ComponentType[] { typeof(CollisionEventImpulse), }
        });
    }

    [BurstCompile]
    struct CollisionEventImpulseJob : ICollisionEventsJob
    {
        [ReadOnly] public ComponentDataFromEntity<CollisionEventImpulse> ColliderEventImpulseGroup;
        public ComponentDataFromEntity<PhysicsVelocity> PhysicsVelocityGroup;

        public void Execute(CollisionEvent collisionEvent)
        {
            Entity entityA = collisionEvent.Entities.EntityA;
            Entity entityB = collisionEvent.Entities.EntityB;

            bool isBodyADynamic = PhysicsVelocityGroup.Exists(entityA);
            bool isBodyBDynamic = PhysicsVelocityGroup.Exists(entityB);

            bool isBodyARepulser = ColliderEventImpulseGroup.Exists(entityA);
            bool isBodyBRepulser = ColliderEventImpulseGroup.Exists(entityB);

            if(isBodyARepulser && isBodyBDynamic)
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

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        JobHandle jobHandle = new CollisionEventImpulseJob
        {
            ColliderEventImpulseGroup = GetComponentDataFromEntity<CollisionEventImpulse>(true),
            PhysicsVelocityGroup = GetComponentDataFromEntity<PhysicsVelocity>(),
        }.Schedule(m_StepPhysicsWorldSystem.Simulation, 
                    ref m_BuildPhysicsWorldSystem.PhysicsWorld, inputDeps);

        return jobHandle;
    }
}