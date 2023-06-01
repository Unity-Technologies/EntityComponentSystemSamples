using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

namespace Events
{
    // This system applies an impulse to any dynamic that collides with a Repulsor.
    // A Repulsor is defined by a PhysicsShapeAuthoring with the `Raise Collision Events` flag ticked and a
    // CollisionEventImpulse behaviour added.
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    public partial struct CollisionEventImpulseSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CollisionEventImpulse>();
            state.RequireForUpdate<SimulationSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new CollisionEventImpulseJob
            {
                CollisionEventImpulseData = SystemAPI.GetComponentLookup<CollisionEventImpulse>(),
                PhysicsVelocityData = SystemAPI.GetComponentLookup<PhysicsVelocity>(),
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
}
