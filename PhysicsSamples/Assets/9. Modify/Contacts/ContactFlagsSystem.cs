using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

namespace Modify
{
    [UpdateInGroup(typeof(PhysicsCreateJacobiansGroup), OrderFirst = true)]
    public partial struct ContactFlagsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ModificationType>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var simulationSingleton = SystemAPI.GetSingletonRW<SimulationSingleton>().ValueRW;
            if (simulationSingleton.Type == SimulationType.NoPhysics)
            {
                return;
            }

            var world = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
            state.Dependency = new SetContactFlagsJob
            {
                modificationData = SystemAPI.GetComponentLookup<ModificationType>()
            }.Schedule(simulationSingleton, ref world, state.Dependency);
        }

        // This job reads the modify component and sets some data on the contact, to get propagated to the jacobian
        // for processing in our jacobian modifier job. This is necessary because some flags require extra data to
        // be allocated along with the jacobian (e.g., SurfaceVelocity data typically does not exist). We also set
        // user data bits in the jacobianFlags to save us from looking up the ComponentLookup later.
        [BurstCompile]
        struct SetContactFlagsJob : IContactsJob
        {
            [ReadOnly] public ComponentLookup<ModificationType> modificationData;

            public void Execute(ref ModifiableContactHeader manifold, ref ModifiableContactPoint contact)
            {
                Entity entityA = manifold.EntityA;
                Entity entityB = manifold.EntityB;

                ModificationType.Type typeA = ModificationType.Type.None;
                if (modificationData.HasComponent(entityA))
                {
                    typeA = modificationData[entityA].type;
                }

                ModificationType.Type typeB = ModificationType.Type.None;
                if (modificationData.HasComponent(entityB))
                {
                    typeB = modificationData[entityB].type;
                }

                if (ContactsSystem.IsModificationType(
                    ModificationType.Type.SurfaceVelocity, typeA, typeB))
                {
                    manifold.JacobianFlags |= JacobianFlags.EnableSurfaceVelocity;
                }

                if (ContactsSystem.IsModificationType(
                    ModificationType.Type.InfiniteInertia, typeA, typeB) ||
                    ContactsSystem.IsModificationType(
                        ModificationType.Type.BiggerInertia, typeA, typeB))
                {
                    manifold.JacobianFlags |= JacobianFlags.EnableMassFactors;
                }
            }
        }
    }
}
