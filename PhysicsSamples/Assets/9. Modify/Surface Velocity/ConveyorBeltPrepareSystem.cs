using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine.Serialization;

namespace Modify
{
    [UpdateInGroup(typeof(PhysicsSimulationGroup))]
    [UpdateAfter(typeof(PhysicsCreateContactsGroup))]
    [UpdateBefore(typeof(PhysicsCreateJacobiansGroup))]
    public partial struct ConveyorBeltPrepareSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ConveyorBelt>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            SimulationSingleton simulation = SystemAPI.GetSingleton<SimulationSingleton>();
            if (simulation.Type == SimulationType.NoPhysics)
            {
                return;
            }

            ref var world = ref SystemAPI.GetSingletonRW<PhysicsWorldSingleton>().ValueRW.PhysicsWorld;

            state.Dependency = new SetConveyorBeltFlagJob
            {
                ConveyorBeltLookup = SystemAPI.GetComponentLookup<ConveyorBelt>(true)
            }.Schedule(simulation, ref world, state.Dependency);
        }

        // This job reads the modify component and sets some data on the contact, to get propagated to the jacobian
        // for processing in our jacobian modifier job. This is necessary because some flags require extra data to
        // be allocated along with the jacobian (e.g., SurfaceVelocity data typically does not exist).
        [BurstCompile]
        struct SetConveyorBeltFlagJob : IContactsJob
        {
            [ReadOnly] public ComponentLookup<ConveyorBelt> ConveyorBeltLookup;

            public void Execute(ref ModifiableContactHeader manifold, ref ModifiableContactPoint contact)
            {
                if (ConveyorBeltLookup.HasComponent(manifold.EntityA) || ConveyorBeltLookup.HasComponent(manifold.EntityB))
                {
                    manifold.JacobianFlags |= JacobianFlags.EnableSurfaceVelocity;
                }
            }
        }
    }
}
