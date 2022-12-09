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

public class CollisionEventImpulseAuthoring : MonoBehaviour
{
    public float Magnitude = 1.0f;
    public float3 Direction = math.up();

    void OnEnable() {}

    class CollisionEventImpulseBaker : Baker<CollisionEventImpulseAuthoring>
    {
        public override void Bake(CollisionEventImpulseAuthoring authoring)
        {
            AddComponent(new CollisionEventImpulse()
            {
                Impulse = authoring.Magnitude * authoring.Direction,
            });
        }
    }
}

// This system applies an impulse to any dynamic that collides with a Repulsor.
// A Repulsor is defined by a PhysicsShapeAuthoring with the `Raise Collision Events` flag ticked and a
// CollisionEventImpulse behaviour added.
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
[BurstCompile]
public partial struct CollisionEventImpulseSystem : ISystem
{
    internal ComponentDataHandles m_ComponentDataHandles;

    internal struct ComponentDataHandles
    {
        public ComponentLookup<CollisionEventImpulse> CollisionEventImpulseData;
        public ComponentLookup<PhysicsVelocity> PhysicsVelocityData;

        public ComponentDataHandles(ref SystemState systemState)
        {
            CollisionEventImpulseData = systemState.GetComponentLookup<CollisionEventImpulse>(true);
            PhysicsVelocityData = systemState.GetComponentLookup<PhysicsVelocity>(false);
        }

        public void Update(ref SystemState systemState)
        {
            CollisionEventImpulseData.Update(ref systemState);
            PhysicsVelocityData.Update(ref systemState);
        }
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(state.GetEntityQuery(ComponentType.ReadOnly<CollisionEventImpulse>()));
        m_ComponentDataHandles = new ComponentDataHandles(ref state);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        m_ComponentDataHandles.Update(ref state);
        state.Dependency = new CollisionEventImpulseJob
        {
            CollisionEventImpulseData = m_ComponentDataHandles.CollisionEventImpulseData,
            PhysicsVelocityData = m_ComponentDataHandles.PhysicsVelocityData,
        }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
    }

    [BurstCompile]
    struct CollisionEventImpulseJob : ICollisionEventsJob
    {
        [ReadOnly] public ComponentLookup<CollisionEventImpulse> CollisionEventImpulseData;
        public ComponentLookup<PhysicsVelocity> PhysicsVelocityData;

        public void Execute(CollisionEvent collisionEvent)
        {
            Entity entityA = collisionEvent.EntityA;
            Entity entityB = collisionEvent.EntityB;

            bool isBodyADynamic = PhysicsVelocityData.HasComponent(entityA);
            bool isBodyBDynamic = PhysicsVelocityData.HasComponent(entityB);

            bool isBodyARepulser = CollisionEventImpulseData.HasComponent(entityA);
            bool isBodyBRepulser = CollisionEventImpulseData.HasComponent(entityB);

            if (isBodyARepulser && isBodyBDynamic)
            {
                var impulseComponent = CollisionEventImpulseData[entityA];
                var velocityComponent = PhysicsVelocityData[entityB];
                velocityComponent.Linear = impulseComponent.Impulse;
                PhysicsVelocityData[entityB] = velocityComponent;
            }
            if (isBodyBRepulser && isBodyADynamic)
            {
                var impulseComponent = CollisionEventImpulseData[entityB];
                var velocityComponent = PhysicsVelocityData[entityA];
                velocityComponent.Linear = impulseComponent.Impulse;
                PhysicsVelocityData[entityA] = velocityComponent;
            }
        }
    }
}
