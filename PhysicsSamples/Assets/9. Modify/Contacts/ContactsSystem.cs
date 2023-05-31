using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;

namespace Modify
{
    // A system which configures the simulation step to modify contacts in various ways
    [UpdateInGroup(typeof(PhysicsSolveAndIntegrateGroup), OrderFirst = true)]
    public partial struct ContactsSystem : ISystem
    {
        internal static bool IsModificationType(ModificationType.Type typeToCheck,
            ModificationType.Type typeOfA, ModificationType.Type typeOfB) =>
            typeOfA == typeToCheck || typeOfB == typeToCheck;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate(state.GetEntityQuery(ComponentType.ReadOnly<ModificationType>()));
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

            state.Dependency = new ModifyContactsJob
            {
                modificationData = SystemAPI.GetComponentLookup<ModificationType>(true)
            }.Schedule(simulationSingleton, ref world, state.Dependency);
        }

        [BurstCompile]
        struct ModifyContactsJob : IJacobiansJob
        {
            [ReadOnly] public ComponentLookup<ModificationType> modificationData;

            // Don't do anything for triggers
            public void Execute(ref ModifiableJacobianHeader h, ref ModifiableTriggerJacobian j)
            {
            }

            public void Execute(ref ModifiableJacobianHeader header, ref ModifiableContactJacobian contact)
            {
                Entity entityA = header.EntityA;
                Entity entityB = header.EntityB;

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

                {
                    // Check for jacobians we want to ignore:
                    if (IsModificationType(ModificationType.Type.DisabledContact, typeA, typeB))
                    {
                        header.Flags = header.Flags | JacobianFlags.Disabled;
                    }

                    // Check if NoTorque modifier, or friction should be disabled through jacobian
                    if (IsModificationType(ModificationType.Type.NoAngularEffects, typeA, typeB) ||
                        IsModificationType(ModificationType.Type.DisabledAngularFriction, typeA,
                            typeB))
                    {
                        // Disable all friction angular effects
                        var friction0 = contact.Friction0;
                        friction0.AngularA = 0.0f;
                        friction0.AngularB = 0.0f;
                        contact.Friction0 = friction0;

                        var friction1 = contact.Friction1;
                        friction1.AngularA = 0.0f;
                        friction1.AngularB = 0.0f;
                        contact.Friction1 = friction1;

                        var angularFriction = contact.AngularFriction;
                        angularFriction.AngularA = 0.0f;
                        angularFriction.AngularB = 0.0f;
                        contact.AngularFriction = angularFriction;
                    }

                    // Check if SurfaceVelocity present
                    if (header.HasSurfaceVelocity &&
                        IsModificationType(ModificationType.Type.SurfaceVelocity, typeA, typeB))
                    {
                        // Since surface normal can change, make sure angular velocity is always relative to it, not independent
                        header.SurfaceVelocity = new SurfaceVelocity
                        {
                            LinearVelocity = float3.zero,
                            AngularVelocity = contact.Normal * (new float3(0.0f, 1.0f, 0.0f))
                        };
                    }

                    // Check if MassFactors present and we should make inertia infinite
                    if (header.HasMassFactors &&
                        IsModificationType(ModificationType.Type.InfiniteInertia, typeA, typeB))
                    {
                        // Give both bodies infinite inertia
                        header.MassFactors = new MassFactors
                        {
                            InverseInertiaFactorA = float3.zero,
                            InverseMassFactorA = 1.0f,
                            InverseInertiaFactorB = float3.zero,
                            InverseMassFactorB = 1.0f
                        };
                    }

                    // Check if MassFactors present and we should make inertia 10x bigger
                    if (header.HasMassFactors &&
                        IsModificationType(ModificationType.Type.BiggerInertia, typeA, typeB))
                    {
                        // Give both bodies 10x bigger inertia
                        header.MassFactors = new MassFactors
                        {
                            InverseInertiaFactorA = new float3(0.1f),
                            InverseMassFactorA = 1.0f,
                            InverseInertiaFactorB = new float3(0.1f),
                            InverseMassFactorB = 1.0f
                        };
                    }
                }

                // Angular jacobian modifications
                for (int i = 0; i < contact.NumContacts; i++)
                {
                    ContactJacAngAndVelToReachCp angular = header.GetAngularJacobian(i);

                    // Check if NoTorque modifier
                    if (IsModificationType(ModificationType.Type.NoAngularEffects, typeA, typeB))
                    {
                        // Disable all angular effects
                        angular.Jac.AngularA = 0.0f;
                        angular.Jac.AngularB = 0.0f;
                    }

                    // Check if SoftContact modifier
                    if (IsModificationType(ModificationType.Type.SoftContact, typeA, typeB))
                    {
                        angular.Jac.EffectiveMass *= 0.1f;
                        if (angular.VelToReachCp > 0.0f)
                        {
                            angular.VelToReachCp *= 0.5f;
                        }
                    }

                    header.SetAngularJacobian(i, angular);
                }
            }
        }
    }
}
