using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

namespace Modify
{
    // A system which configures the simulation step to disable certain broad phase pairs
    [UpdateInGroup(typeof(PhysicsSimulationGroup))]
    [UpdateAfter(typeof(PhysicsCreateBodyPairsGroup))]
    [UpdateBefore(typeof(PhysicsCreateContactsGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial struct BroadphasePairsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BroadphasePairs>();
            state.RequireForUpdate<SimulationSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var simulation = SystemAPI.GetSingleton<SimulationSingleton>();
            if (simulation.Type == SimulationType.NoPhysics)
            {
                return;
            }

            var physicsWorld = SystemAPI.GetSingletonRW<PhysicsWorldSingleton>().ValueRW.PhysicsWorld;

            state.Dependency = new DisablePairsJob
            {
                Bodies = physicsWorld.Bodies,
                Motions = physicsWorld.MotionVelocities
            }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), ref physicsWorld, state.Dependency);
        }

        [BurstCompile]
        struct DisablePairsJob : IBodyPairsJob
        {
            [ReadOnly] public NativeArray<RigidBody> Bodies;
            [ReadOnly] public NativeArray<MotionVelocity> Motions;

            public unsafe void Execute(ref ModifiableBodyPair pair)
            {
                // Disable the pair if a box collides with a static object
                int indexA = pair.BodyIndexA;
                int indexB = pair.BodyIndexB;
                if ((Bodies[indexA].Collider != null
                     && Bodies[indexA].Collider.Value.Type == ColliderType.Box
                     && indexB >= Motions.Length)
                    || (Bodies[indexB].Collider != null
                        && Bodies[indexB].Collider.Value.Type == ColliderType.Box
                        && indexA >= Motions.Length))
                {
                    pair.Disable();
                }
            }
        }
    }
}
